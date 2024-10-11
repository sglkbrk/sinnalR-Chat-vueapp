using ProductManagement.Domain.Models;

public class UserDto
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }

    public Message LastMessage { get; set; }
    // Password alanı burada yer almaz.
}