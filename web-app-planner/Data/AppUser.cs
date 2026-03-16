using Microsoft.AspNetCore.Identity;

namespace Planner.Data;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
}
