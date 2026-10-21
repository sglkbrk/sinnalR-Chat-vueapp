using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Services;
using ProductManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Serilog;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);


if (FirebaseApp.DefaultInstance == null)
{
    try
    {
        var firebasePath = builder.Configuration["Firebase:CredentialPath"]
                           ?? Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL_PATH")
                           ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")
                           ?? "serviceAccountKey.json";

        var firebaseJson = Environment.GetEnvironmentVariable("FIREBASE_CONFIG_JSON");

        GoogleCredential? credential = null;
        if (!string.IsNullOrEmpty(firebaseJson))
        {
            credential = GoogleCredential.FromJson(firebaseJson);
        }
        else if (File.Exists(firebasePath))
        {
            credential = GoogleCredential.FromFile(firebasePath);
        }

        if (credential != null)
        {
            FirebaseApp.Create(new AppOptions { Credential = credential });
            Console.WriteLine("[Firebase] FirebaseApp başarıyla başlatıldı.");
        }
        else
        {
            Console.WriteLine("[Firebase Uyarı] Firebase servis hesabı anahtarı bulunamadı (serviceAccountKey.json). Push bildirimler devre dışı olabilir.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Firebase Hata] Başlatılırken hata oluştu: {ex.Message}");
    }
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddSignalR();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
    new MySqlServerVersion(new Version(8, 0, 40))));
// Server=localhost;Database=ProductManagementDb;User=root;Password=123456;
// Servisleri ekleyin
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<UserAuthService>();
builder.Services.AddScoped<FbTokenService>();
builder.Services.AddScoped<FriendService>();
builder.Services.AddScoped<FriendRequestService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<MinioFileService>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Lütfen Bearer <token> biçiminde token giriniz",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });

});
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5044); // Tüm IP adreslerinden dinle
    // Veya belirli bir IP adresini dinle
    // options.Listen(IPAddress.Parse("192.168.1.100"), 5000);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(origin => true); // Geliştirme ortamında herhangi bir origin'e izin verebilirsiniz.
    });
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? "";
var key = Encoding.UTF8.GetBytes(jwtKey);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Issuer"],
            ValidateLifetime = true
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseCors();
app.UseMiddleware<ExceptionMiddleware>();
app.UseRouting();
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    //c.RoutePrefix = string.Empty; // Swagger'ı root URL üzerinden açmak için
});
app.UseStaticFiles();
// Hata ayıklama sayfasını sadece geliştirme modunda gösterin
app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/", async context =>
    {
        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(System.IO.File.ReadAllText("wwwroot/index.html"));
    });
    endpoints.MapHub<ChatHub>("/chatHub");
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        dbContext.Database.Migrate();
        Log.Information("Veritabanı migration'ları başarıyla uygulandı.");
    }
    catch (Exception ex)
    {
        Log.Warning("Veritabanı migration uygulanamadı (MySQL hazır olmayabilir): {Message}", ex.Message);
    }
}

try
{
    Log.Information("Uygulama başlatılıyor...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Uygulama başlatma hatası");
}
finally
{
    Log.CloseAndFlush();
}


