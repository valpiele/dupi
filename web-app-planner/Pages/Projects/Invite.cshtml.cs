using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planner.Data;
using Planner.Models;

namespace Planner.Pages.Projects;

public class InviteModel : PageModel
{
    private readonly PlannerDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public InviteModel(PlannerDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public string SuccessMessage { get; set; } = "";
    public string ErrorMessage { get; set; } = "";

    [BindProperty]
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    private async Task<IActionResult?> LoadProjectAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var membership = await _db.ProjectMembers
            .Include(pm => pm.Project)
            .FirstOrDefaultAsync(pm => pm.ProjectId == id && pm.UserId == userId && pm.Role == MemberRole.Owner);

        if (membership == null) return Forbid();

        ProjectId = id;
        ProjectName = membership.Project.Name;
        return null;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        return await LoadProjectAsync(id) ?? Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var guard = await LoadProjectAsync(id);
        if (guard != null) return guard;

        if (!ModelState.IsValid) return Page();

        var targetUser = await _userManager.FindByEmailAsync(Email);
        if (targetUser == null)
        {
            ErrorMessage = "No user found with that email.";
            return Page();
        }

        var alreadyMember = await _db.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == id && pm.UserId == targetUser.Id);

        if (alreadyMember)
        {
            ErrorMessage = "That user is already a member of this project.";
            return Page();
        }

        _db.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = id,
            UserId = targetUser.Id,
            Role = MemberRole.Member
        });
        await _db.SaveChangesAsync();

        SuccessMessage = $"{targetUser.DisplayName} ({targetUser.Email}) has been added.";
        Email = "";
        return Page();
    }
}
