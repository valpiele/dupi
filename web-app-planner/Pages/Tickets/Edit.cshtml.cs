using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planner.Data;
using Planner.Models;

namespace Planner.Pages.Tickets;

public class EditModel : PageModel
{
    private readonly PlannerDbContext _db;

    public EditModel(PlannerDbContext db) => _db = db;

    public Ticket Ticket { get; set; } = null!;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        [MaxLength(4000)]
        public string? Description { get; set; }

        [Required]
        public TicketType Type { get; set; }

        [Required]
        public TicketState State { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var guard = await LoadTicketAsync(id);
        if (guard != null) return guard;

        Input = new InputModel
        {
            Title       = Ticket.Title,
            Description = Ticket.Description,
            Type        = Ticket.Type,
            State       = Ticket.State
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var guard = await LoadTicketAsync(id);
        if (guard != null) return guard;

        if (!ModelState.IsValid) return Page();

        Ticket.Title       = Input.Title;
        Ticket.Description = Input.Description;
        Ticket.Type        = Input.Type;
        Ticket.State       = Input.State;
        Ticket.UpdatedAt   = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        return RedirectToPage("/Tickets/Detail", new { id });
    }

    private async Task<IActionResult?> LoadTicketAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        Ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new Exception("Ticket not found");

        var isMember = await _db.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == Ticket.ProjectId && pm.UserId == userId);

        return isMember ? null : Forbid();
    }
}
