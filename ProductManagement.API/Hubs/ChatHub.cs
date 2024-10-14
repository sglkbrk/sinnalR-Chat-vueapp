using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using ProductManagement.Domain.Models;
using ProductManagement.Application.Services;
using System.Collections.Concurrent;

[Authorize]
public class ChatHub : Hub
{
    // Kullanıcıların online durumlarını tutmak için bir ConcurrentDictionary kullanıyoruz
    private static ConcurrentDictionary<string, string> _onlineUsers = new ConcurrentDictionary<string, string>();
    private readonly MessageService _messageService;
    private readonly FbTokenService _fbTokenService;
    public ChatHub(MessageService messageService, FbTokenService fbTokenService)
    {
        _messageService = messageService;
        _fbTokenService = fbTokenService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _onlineUsers.TryAdd(Context.ConnectionId, userId);
        await Clients.All.SendAsync("UserStatusUpdated", _onlineUsers.Values.ToList());
        // Kullanıcıyı gruplara ekleyebilirsiniz veya online durumunu yönetebilirsiniz
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        // Kullanıcı bağlantıyı kestiğinde
        _onlineUsers.TryRemove(Context.ConnectionId, out _); // Kullanıcıyı kaldır

        // Tüm kullanıcılara online kullanıcıların listesini güncelle
        await Clients.All.SendAsync("UserStatusUpdated", _onlineUsers.Values.ToList());
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(Message message)
    {
        var senderId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = Context.User.FindFirstValue(ClaimTypes.Name);
        message.SenderId = int.Parse(senderId);
        await _messageService.AddMessageAsync(message);
        await Clients.User(message.ReceiverId.ToString()).SendAsync("ReceiveMessage", new
        {
            Name = name,
            SenderId = senderId,
            Content = message,
            Timestamp = message.Timestamp
        });
        await sendNotification(message);
    }

    public async Task SendWrite(string ReceiverId)
    {
        var senderId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        await Clients.User(ReceiverId).SendAsync("ReceiveWrites", senderId);
    }

    public async Task SendSeen(string ReceiverId, messageStatus messageStatus)
    {
        var senderId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        await Clients.User(ReceiverId).SendAsync("ReceiveSeen", senderId, messageStatus);
        await _messageService.setSeen(int.Parse(ReceiverId), int.Parse(senderId), messageStatus);
    }

    private async Task sendNotification(Message message)
    {
        var name = Context.User.FindFirstValue(ClaimTypes.Name);
        bool isOnline = _onlineUsers.Values.Contains(message.ReceiverId.ToString());
        if (isOnline) return;
        var token = _fbTokenService.GetTokenByUserId(int.Parse(message.ReceiverId.ToString()));
        if (token == null) return;
        await _fbTokenService.SendNotificationAsync(message.Content, name, token.Token, message.SenderId.ToString());
    }
}
