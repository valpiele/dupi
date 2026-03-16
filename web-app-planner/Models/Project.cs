using System.ComponentModel.DataAnnotations;
using Planner.Data;

namespace Planner.Models;

public class Project
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string OwnerId { get; set; } = "";
    public AppUser Owner { get; set; } = null!;

    public List<ProjectMember> Members { get; set; } = [];
    public List<Ticket> Tickets { get; set; } = [];
}
