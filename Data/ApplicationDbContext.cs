#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DotnetConfigServer.Data;

/// <summary>
/// Entity Framework DbContext for the configuration server
/// </summary>
public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Application> Applications { get; set; } = null!;
    public DbSet<Configuration> Configurations { get; set; } = null!;
    public DbSet<ConfigurationKey> ConfigurationKeys { get; set; } = null!;
    public DbSet<ConfigurationVersion> ConfigurationVersions { get; set; } = null!;
    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; } = null!;
    public DbSet<WebhookDelivery> WebhookDeliveries { get; set; } = null!;
    public DbSet<ConfigurationDiff> ConfigurationDiffs { get; set; } = null!;
    public DbSet<DiffEntry> DiffEntries { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<EncryptionKey> EncryptionKeys { get; set; } = null!;
    public DbSet<ChangeRequest> ChangeRequests { get; set; } = null!;
    public DbSet<ValidationRule> ValidationRules { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Application
        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.Slug).IsUnique();
            entity.HasIndex(a => a.ApiKey).IsUnique();
            entity.Property(a => a.Name).IsRequired().HasMaxLength(256);
            entity.Property(a => a.Slug).IsRequired().HasMaxLength(256);
            entity.Property(a => a.ApiKey).IsRequired();
        });

        // Configure Configuration
        modelBuilder.Entity<Configuration>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.ApplicationId);
            entity.HasIndex(c => new { c.ApplicationId, c.Name });
            entity.Property(c => c.Name).IsRequired().HasMaxLength(256);
            entity.HasIndex(c => c.Name);
        });

        // Configure ConfigurationKey
        modelBuilder.Entity<ConfigurationKey>(entity =>
        {
            entity.HasKey(k => k.Id);
            entity.HasIndex(k => k.ConfigurationId);
            entity.HasIndex(k => k.VersionId);
            entity.HasIndex(k => new { k.ConfigurationId, k.Key });
            entity.Property(k => k.Key).IsRequired().HasMaxLength(256);
            entity.Property(k => k.Value).IsRequired();
        });

        // Configure ConfigurationVersion
        modelBuilder.Entity<ConfigurationVersion>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.HasIndex(v => v.ConfigurationId);
            entity.HasIndex(v => new { v.ConfigurationId, v.Status });
    entity.HasIndex(v => new { v.ConfigurationId, v.VersionNumber })
        .IsUnique()
        .HasDatabaseName("IX_ConfigurationVersion_ConfigurationId_VersionNumber_Unique");
            entity.Property(v => v.VersionNumber).IsRequired().HasMaxLength(50);
            entity.HasMany(v => v.Keys).WithOne().HasForeignKey(k => k.VersionId).OnDelete(DeleteBehavior.Cascade);
        });

        // Configure WebhookSubscription
        modelBuilder.Entity<WebhookSubscription>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.HasIndex(w => w.ConfigurationId);
            entity.HasIndex(w => new { w.ConfigurationId, w.IsActive });
            entity.Property(w => w.Name).IsRequired().HasMaxLength(256);
            entity.Property(w => w.Url).IsRequired().HasMaxLength(2048);
        });

        // Configure WebhookDelivery
        modelBuilder.Entity<WebhookDelivery>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.WebhookSubscriptionId);
            entity.HasIndex(d => new { d.WebhookSubscriptionId, d.Status });
            entity.Property(d => d.Payload).IsRequired();
        });

        // Configure ConfigurationDiff
        modelBuilder.Entity<ConfigurationDiff>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.ConfigurationId);
            entity.HasIndex(d => new { d.ConfigurationId, d.CreatedAt });
            entity.HasMany(d => d.Changes).WithOne().HasForeignKey(e => e.DiffId).OnDelete(DeleteBehavior.Cascade);
        });

        // Configure DiffEntry
        modelBuilder.Entity<DiffEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DiffId);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(256);
        });

        // Configure AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.ConfigurationId);
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => new { a.EntityType, a.EntityId });
            entity.HasIndex(a => a.Timestamp);
            entity.Property(a => a.EntityType).IsRequired().HasMaxLength(256);
            entity.Property(a => a.EntityId).IsRequired().HasMaxLength(256);
        });

        // Configure EncryptionKey
        modelBuilder.Entity<EncryptionKey>(entity =>
        {
            entity.HasKey(k => k.Id);
            entity.HasIndex(k => k.KeyId).IsUnique();
            entity.HasIndex(k => new { k.IsActive, k.ExpiresAt });
            entity.Property(k => k.Name).IsRequired().HasMaxLength(256);
            entity.Property(k => k.KeyId).IsRequired().HasMaxLength(256);
            entity.Property(k => k.EncryptedKey).IsRequired();
            entity.Property(k => k.Salt).IsRequired();
        });

        // Configure ChangeRequest
        modelBuilder.Entity<ChangeRequest>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.ConfigurationId);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => new { r.ConfigurationId, r.Status });
            entity.Property(r => r.RequestedBy).IsRequired().HasMaxLength(256);
            entity.Property(r => r.Payload).IsRequired();
        });

        // Configure ValidationRule
        modelBuilder.Entity<ValidationRule>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.ConfigurationId);
            entity.HasIndex(r => new { r.ConfigurationId, r.IsActive });
            entity.Property(r => r.Name).IsRequired().HasMaxLength(256);
            entity.Property(r => r.Description).HasMaxLength(1024);
            entity.Property(r => r.CreatedBy).IsRequired();
            entity.Property(r => r.TargetKeyPattern).HasMaxLength(512);
        });
    }
}
