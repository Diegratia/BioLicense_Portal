using Microsoft.EntityFrameworkCore;
using BioLicense_Portal.Domain.Entities;
using AppEntity = BioLicense_Portal.Domain.Entities.Application;

namespace BioLicense_Portal.Infrastructure.Data
{
    public class BioLicenseDbContext : DbContext
    {
        public BioLicenseDbContext(DbContextOptions<BioLicenseDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<AppEntity> Applications { get; set; }
        public DbSet<ApplicationFeature> ApplicationFeatures { get; set; }
        public DbSet<ApplicationTier> ApplicationTiers { get; set; }
        public DbSet<ApplicationTierFeature> ApplicationTierFeatures { get; set; }
        public DbSet<LicenseRecord> Licenses { get; set; }
        public DbSet<LicenseRequest> LicenseRequests { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure naming conventions or specific mappings if needed
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<RefreshToken>().ToTable("refresh_tokens");
            modelBuilder.Entity<AppEntity>().ToTable("applications");
            modelBuilder.Entity<ApplicationFeature>().ToTable("application_features");
            modelBuilder.Entity<ApplicationTier>().ToTable("application_tiers");
            modelBuilder.Entity<LicenseRecord>().ToTable("licenses");
            modelBuilder.Entity<LicenseRequest>().ToTable("license_requests");
            modelBuilder.Entity<AuditLog>().ToTable("audit_logs");
            modelBuilder.Entity<User>(entity => {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<AppEntity>(entity => {
                entity.HasIndex(a => a.Name).IsUnique();
                entity.HasIndex(a => a.Slug).IsUnique();
            });

            modelBuilder.Entity<ApplicationFeature>(entity => {
                entity.HasIndex(f => new { f.ApplicationId, f.FeatureKey }).IsUnique();
            });

            modelBuilder.Entity<LicenseRecord>(entity => {
                entity.HasIndex(l => l.Licenseid).IsUnique();
            });

            modelBuilder.Entity<LicenseRequest>(entity => {
                entity.HasOne(r => r.Requester)
                    .WithMany()
                    .HasForeignKey(r => r.RequesterUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Approver)
                    .WithMany()
                    .HasForeignKey(r => r.ApproverUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Tier Features Many-to-Many
            modelBuilder.Entity<ApplicationTierFeature>(entity => {
                entity.ToTable("application_tier_features");
                entity.HasKey(tf => new { tf.TierId, tf.FeatureId });
                
                entity.HasOne(tf => tf.Tier)
                    .WithMany(t => t.TierFeatures)
                    .HasForeignKey(tf => tf.TierId);
                
                entity.HasOne(tf => tf.Feature)
                    .WithMany(f => f.TierFeatures)
                    .HasForeignKey(tf => tf.FeatureId);
            });

            modelBuilder.Entity<ApplicationTier>(entity => {
                entity.HasIndex(t => new { t.ApplicationId, t.Tier }).IsUnique();
            });
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
