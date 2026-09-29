using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProcurePilot.Application.Tenancy;
using ProcurePilot.Domain.Tenancy;
using ProcurePilot.Infrastructure.Identity;

namespace ProcurePilot.Infrastructure.Persistence;

public sealed class ProcurePilotDbContext(
    DbContextOptions<ProcurePilotDbContext> options,
    ITenantContext tenantContext)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    private Guid? CurrentTenantId => tenantContext.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(tenant =>
        {
            tenant.ToTable("Tenants");
            tenant.HasKey(value => value.Id);
            tenant.Property(value => value.Name).HasMaxLength(200).IsRequired();
            tenant.Property(value => value.Status).HasConversion<string>().HasMaxLength(32);
            tenant.HasIndex(value => value.Name);
            tenant.HasQueryFilter(value =>
                CurrentTenantId.HasValue && value.Id == CurrentTenantId.GetValueOrDefault());
        });

        modelBuilder.Entity<ApplicationUser>(user =>
        {
            user.Property(value => value.DisplayName).HasMaxLength(200).IsRequired();
            user.HasIndex(value => value.TenantId);
            user.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(value => value.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        ApplyTenantQueryFilters(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateTenantWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ValidateTenantWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidateTenantWrites()
    {
        var entries = ChangeTracker.Entries<ITenantScopedEntity>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        foreach (var entry in entries)
        {
            var currentTenantId = tenantContext.RequireTenantId();
            if (entry.Entity.TenantId != currentTenantId)
            {
                throw new InvalidOperationException("Cross-tenant writes are not allowed.");
            }
        }
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(type => typeof(ITenantScopedEntity).IsAssignableFrom(type.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var entityTenantId = Expression.Convert(
                Expression.Property(parameter, nameof(ITenantScopedEntity.TenantId)),
                typeof(Guid?));
            var currentTenantId = Expression.Property(
                Expression.Constant(this),
                nameof(CurrentTenantId));
            var hasTenant = Expression.Property(currentTenantId, nameof(Nullable<Guid>.HasValue));
            var belongsToTenant = Expression.Equal(entityTenantId, currentTenantId);
            var filter = Expression.Lambda(
                Expression.AndAlso(hasTenant, belongsToTenant),
                parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}
