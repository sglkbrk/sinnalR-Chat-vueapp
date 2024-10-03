# Base image olarak ASP.NET runtime kullanılıyor
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5044
EXPOSE 7266

# SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Proje dosyasını kopyala ve restore et
COPY ["ProductManagement.API/ProductManagement.API.csproj", "ProductManagement.API/"]
COPY ["ProductManagement.Application/ProductManagement.Application.csproj", "ProductManagement.Application/"]
COPY ["ProductManagement.Domain/ProductManagement.Domain.csproj", "ProductManagement.Domain/"]
COPY ["ProductManagement.Infrastructure/ProductManagement.Infrastructure.csproj", "ProductManagement.Infrastructure/"]
RUN dotnet restore "ProductManagement.API/ProductManagement.API.csproj"

# Diğer dosyaları kopyalayın ve uygulamayı build edin
COPY . .
WORKDIR "/src/ProductManagement.API"
RUN dotnet build "ProductManagement.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProductManagement.API.csproj" -c Release -o /app/publish

# Son olarak uygulamayı çalıştırmak için base image'e geçiş yapıyoruz
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductManagement.API.dll"]
