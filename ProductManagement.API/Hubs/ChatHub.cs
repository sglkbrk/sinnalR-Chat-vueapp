using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Security.Claims;
using ProductManagement.Domain.Models;
using ProductManagement.Application.Services;

[Authorize]
public class ChatHub : Hub
{
    private readonly MessageService _messageService;
    public ChatHub(MessageService messageService)
    {
        _messageService = messageService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        // Kullanıcıyı gruplara ekleyebilirsiniz veya online durumunu yönetebilirsiniz
        await base.OnConnectedAsync();
    }

    public async Task SendMessage(Message message)
    {
        var senderId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = Context.User.FindFirstValue(ClaimTypes.Name);
        // Mesajı veritabanına kaydet
        message.SenderId = int.Parse(senderId);
        _messageService.AddMessage(message);
        // Alıcıya mesajı gönder
        await Clients.User(message.ReceiverId.ToString()).SendAsync("ReceiveMessage", new
        {
            Name = name,
            SenderId = senderId,
            Content = message,
            Timestamp = message.Timestamp
        });
    }

    public async Task SendWrite(string ReceiverId)
    {
        var senderId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        // Mesajı veritabanına kaydet
        // Alıcıya mesajı gönder
        await Clients.User(ReceiverId).SendAsync("ReceiveWrites", senderId);
    }
}
