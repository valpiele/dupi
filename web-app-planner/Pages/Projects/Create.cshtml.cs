using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Planner.Data;
using Planner.Models;

namespace Planner.Pages.Projects;

public class CreateModel : PageModel
{
    private readonly PlannerDbContext _db;

    public CreateModel(PlannerDbContext db) => _db = db;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, MaxLength(120)]
        public string Name { get; set; } = "";

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var project = new Project
        {
            Name = Input.Name,
            Description = Input.Description,
            OwnerId = userId
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        _db.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = project.Id,
            UserId = userId,
            Role = MemberRole.Owner
        });
        await _db.SaveChangesAsync();

        return RedirectToPage("/Projects/Detail", new { id = project.Id });
    }
}
