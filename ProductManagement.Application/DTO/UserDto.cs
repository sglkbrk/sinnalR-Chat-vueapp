namespace ProductManagement.Application.DTO
{
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? StatusMessage { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public MessageDto? LastMessage { get; set; }
    }

    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? StatusMessage { get; set; }
        public string? UserCode { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserSearchResultDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? StatusMessage { get; set; }
        public bool IsFriend { get; set; }
        public bool HasPendingIncoming { get; set; }
        public bool HasPendingOutgoing { get; set; }
        public int? PendingRequestId { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string? StatusMessage { get; set; }
    }
}
