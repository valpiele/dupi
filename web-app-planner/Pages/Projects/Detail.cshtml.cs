using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planner.Data;
using Planner.Models;

namespace Planner.Pages.Projects;

public class DetailModel : PageModel
{
    private readonly PlannerDbContext _db;

    public DetailModel(PlannerDbContext db) => _db = db;

    public Project Project { get; set; } = null!;
    public List<Ticket> Features { get; set; } = [];
    public List<Ticket> Bugs { get; set; } = [];
    public bool IsOwner { get; set; }
    public TicketState? StateFilter { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, string? state)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var membership = await _db.ProjectMembers
            .Include(pm => pm.Project)
            .FirstOrDefaultAsync(pm => pm.ProjectId == id && pm.UserId == userId);

        if (membership == null) return Forbid();

        Project = membership.Project;
        IsOwner = membership.Role == MemberRole.Owner;

        StateFilter = state switch
        {
            "Open"       => TicketState.Open,
            "InProgress" => TicketState.InProgress,
            "Closed"     => TicketState.Closed,
            _            => null
        };

        var query = _db.Tickets
            .Include(t => t.Author)
            .Where(t => t.ProjectId == id);

        if (StateFilter.HasValue)
            query = query.Where(t => t.State == StateFilter.Value);

        var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

        Features = tickets.Where(t => t.Type == TicketType.Feature).ToList();
        Bugs     = tickets.Where(t => t.Type == TicketType.Bug).ToList();

        return Page();
    }
}
