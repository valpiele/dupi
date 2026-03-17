namespace dupi.Models;

public class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = false;
    public string Goals { get; set; } = string.Empty;
    public string DietType { get; set; } = string.Empty;
}
