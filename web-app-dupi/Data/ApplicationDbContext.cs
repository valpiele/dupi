using System.Text.Json;
using dupi.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace dupi.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<NutritionPlan> NutritionPlans => Set<NutritionPlan>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Challenge> Challenges => Set<Challenge>();
    public DbSet<ChallengeParticipant> ChallengeParticipants => Set<ChallengeParticipant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Friendship>(e =>
        {
            e.HasIndex(f => new { f.SenderId, f.ReceiverId }).IsUnique();
            e.HasIndex(f => f.ReceiverId);

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(f => f.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(f => f.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Message>(e =>
        {
            e.HasIndex(m => new { m.SenderId, m.ReceiverId });
            e.HasIndex(m => new { m.ReceiverId, m.IsRead });

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Challenge>(e =>
        {
            e.HasIndex(c => c.CreatorId);
            e.HasIndex(c => c.Status);

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(c => c.CreatorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChallengeParticipant>(e =>
        {
            e.HasIndex(cp => new { cp.ChallengeId, cp.UserId }).IsUnique();
            e.HasIndex(cp => cp.UserId);

            e.HasOne<Challenge>()
                .WithMany()
                .HasForeignKey(cp => cp.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        var listComparer = new ValueComparer<List<string>>(
            (a, b) => a != null && b != null && a.SequenceEqual(b),
            v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
            v => v.ToList());

        builder.Entity<NutritionPlan>(e =>
        {
            e.HasIndex(p => p.UserId);

            e.Property(p => p.WhatsGood)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("jsonb")
                .Metadata.SetValueComparer(listComparer);

            e.Property(p => p.WhatToImprove)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("jsonb")
                .Metadata.SetValueComparer(listComparer);

            e.Property(p => p.SharedWithUsers)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("jsonb")
                .Metadata.SetValueComparer(listComparer);
        });
    }
}
