using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planner.Data;
using Planner.Models;

namespace Planner.Pages.Tickets;

public class DetailModel : PageModel
{
    private readonly PlannerDbContext _db;

    public DetailModel(PlannerDbContext db) => _db = db;

    public Ticket Ticket { get; set; } = null!;

    [BindProperty]
    [Required, MaxLength(4000)]
    public string CommentBody { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        return await LoadTicketAsync(id) ?? Page();
    }

    public async Task<IActionResult> OnPostAddCommentAsync(int id)
    {
        var guard = await LoadTicketAsync(id);
        if (guard != null) return guard;

        if (!ModelState.IsValid) return Page();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        _db.Comments.Add(new Comment
        {
            TicketId = id,
            AuthorId = userId,
            Body = CommentBody
        });
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    private async Task<IActionResult?> LoadTicketAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var ticket = await _db.Tickets
            .Include(t => t.Author)
            .Include(t => t.Comments.OrderBy(c => c.CreatedAt))
                .ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();

        var isMember = await _db.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == ticket.ProjectId && pm.UserId == userId);

        if (!isMember) return Forbid();

        Ticket = ticket;
        return null;
    }
}
