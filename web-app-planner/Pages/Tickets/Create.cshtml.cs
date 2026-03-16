using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planner.Data;
using Planner.Models;

namespace Planner.Pages.Tickets;

public class CreateModel : PageModel
{
    private readonly PlannerDbContext _db;

    public CreateModel(PlannerDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public int ProjectId { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        [MaxLength(4000)]
        public string? Description { get; set; }

        [Required]
        public TicketType Type { get; set; } = TicketType.Feature;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        return await CheckMembershipAsync() ?? Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var guard = await CheckMembershipAsync();
        if (guard != null) return guard;

        if (!ModelState.IsValid) return Page();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        _db.Tickets.Add(new Ticket
        {
            ProjectId = ProjectId,
            Title = Input.Title,
            Description = Input.Description,
            Type = Input.Type,
            State = TicketState.Open,
            AuthorId = userId
        });
        await _db.SaveChangesAsync();

        return RedirectToPage("/Projects/Detail", new { id = ProjectId });
    }

    private async Task<IActionResult?> CheckMembershipAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isMember = await _db.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == ProjectId && pm.UserId == userId);
        return isMember ? null : Forbid();
    }
}
