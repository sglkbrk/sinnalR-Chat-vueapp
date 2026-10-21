namespace ProductManagement.Application.DTO
{
    public class Friends
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? StatusMessage { get; set; }
        public bool IsPinned { get; set; }
        public bool IsMuted { get; set; }
        public bool IsArchived { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public MessageDto? LastMessage { get; set; }
        public int NotSeenMessagesCount { get; set; }
    }
}
