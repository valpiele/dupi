using System.ComponentModel.DataAnnotations;
using Planner.Data;

namespace Planner.Models;

public class Ticket
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(4000)]
    public string? Description { get; set; }

    public TicketType Type { get; set; }
    public TicketState State { get; set; } = TicketState.Open;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string AuthorId { get; set; } = "";
    public AppUser Author { get; set; } = null!;

    public List<Comment> Comments { get; set; } = [];
}
