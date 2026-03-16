using System.ComponentModel.DataAnnotations;
using Planner.Data;

namespace Planner.Models;

public class Comment
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public string AuthorId { get; set; } = "";
    public AppUser Author { get; set; } = null!;

    [Required, MaxLength(4000)]
    public string Body { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
