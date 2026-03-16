using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planner.Data;
using Planner.Models;
using System.Security.Claims;

namespace Planner.Pages.Projects;

public class IndexModel : PageModel
{
    private readonly PlannerDbContext _db;

    public IndexModel(PlannerDbContext db) => _db = db;

    public List<Project> Projects { get; set; } = [];

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Projects = await _db.ProjectMembers
            .Where(pm => pm.UserId == userId)
            .Include(pm => pm.Project)
                .ThenInclude(p => p.Members)
            .OrderByDescending(pm => pm.Project.CreatedAt)
            .Select(pm => pm.Project)
            .ToListAsync();
    }
}
