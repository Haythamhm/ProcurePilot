using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProcurePilot.Application.Tenancy;
using ProcurePilot.Domain.Exceptions;
using ProcurePilot.Domain.Invoices;
using ProcurePilot.Domain.Matching;
using ProcurePilot.Domain.Purchasing;
using ProcurePilot.Domain.Tenancy;
using ProcurePilot.Domain.Vendors;
using ProcurePilot.Infrastructure.Identity;
using ProcurePilot.Infrastructure.Invoices;

namespace ProcurePilot.Infrastructure.Persistence;

public sealed class ProcurePilotDbContext(
    DbContextOptions<ProcurePilotDbContext> options,
    ITenantContext tenantContext)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    private Guid? CurrentTenantId => tenantContext.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Vendor> Vendors => Set<Vendor>();

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    public DbSet<InvoiceDocument> InvoiceDocuments => Set<InvoiceDocument>();

    public DbSet<InvoiceExtractionSnapshot> InvoiceExtractionSnapshots => Set<InvoiceExtractionSnapshot>();

    public DbSet<MatchingRule> MatchingRules => Set<MatchingRule>();

    public DbSet<MatchResult> MatchResults => Set<MatchResult>();

    public DbSet<ExceptionCase> ExceptionCases => Set<ExceptionCase>();

    public DbSet<ExceptionCaseEvidence> ExceptionCaseEvidences => Set<ExceptionCaseEvidence>();

    public DbSet<AiAnalysis> AiAnalyses => Set<AiAnalysis>();

    public DbSet<Resolution> Resolutions => Set<Resolution>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

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
            user.HasAlternateKey(value => new { value.TenantId, value.Id });
            user.HasIndex(value => value.TenantId);
            user.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(value => value.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Vendor>(vendor =>
        {
            vendor.ToTable("Vendors");
            vendor.HasKey(value => value.Id);
            vendor.Property(value => value.TenantId).IsRequired();
            vendor.Property(value => value.ExternalReference)
                .HasMaxLength(Vendor.ExternalReferenceMaxLength)
                .IsRequired();
            vendor.Property(value => value.Name)
                .HasMaxLength(Vendor.NameMaxLength)
                .IsRequired();
            vendor.Property(value => value.TaxIdentifier)
                .HasMaxLength(Vendor.TaxIdentifierMaxLength);
            vendor.HasAlternateKey(value => new { value.TenantId, value.Id });
            vendor.HasIndex(value => new { value.TenantId, value.ExternalReference })
                .IsUnique();
            vendor.HasIndex(value => new { value.TenantId, value.Name });
            vendor.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(value => value.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrder>(purchaseOrder =>
        {
            purchaseOrder.ToTable("PurchaseOrders");
            purchaseOrder.HasKey(value => value.Id);
            purchaseOrder.HasAlternateKey(value => new { value.TenantId, value.Id });
            purchaseOrder.Property(value => value.TenantId).IsRequired();
            purchaseOrder.Property(value => value.VendorId).IsRequired();
            purchaseOrder.Property(value => value.Number)
                .HasMaxLength(PurchaseOrder.NumberMaxLength)
                .IsRequired();
            purchaseOrder.Property(value => value.OrderDate).IsRequired();
            purchaseOrder.Property(value => value.Currency)
                .HasMaxLength(PurchaseOrder.CurrencyMaxLength)
                .IsFixedLength()
                .IsRequired();
            purchaseOrder.Property(value => value.Status)
                .HasMaxLength(PurchaseOrder.StatusMaxLength)
                .IsRequired();
            purchaseOrder.Property(value => value.TotalAmount)
                .HasPrecision(18, 2)
                .IsRequired();
            purchaseOrder.HasIndex(value => new { value.TenantId, value.Number })
                .IsUnique();
            purchaseOrder.HasIndex(value => new { value.TenantId, value.VendorId });
            purchaseOrder.HasOne<Vendor>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, value.VendorId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Restrict);
            purchaseOrder.HasMany(value => value.Lines)
                .WithOne()
                .HasForeignKey(value => new { value.TenantId, value.PurchaseOrderId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Cascade);
            purchaseOrder.Navigation(value => value.Lines)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<PurchaseOrderLine>(line =>
        {
            line.ToTable("PurchaseOrderLines");
            line.HasKey(value => value.Id);
            line.HasAlternateKey(value => new { value.TenantId, value.Id });
            line.Property(value => value.TenantId).IsRequired();
            line.Property(value => value.PurchaseOrderId).IsRequired();
            line.Property(value => value.LineNumber).IsRequired();
            line.Property(value => value.Description)
                .HasMaxLength(PurchaseOrderLine.DescriptionMaxLength)
                .IsRequired();
            line.Property(value => value.Quantity)
                .HasPrecision(18, 4)
                .IsRequired();
            line.Property(value => value.UnitPrice)
                .HasPrecision(18, 2)
                .IsRequired();
            line.HasIndex(value => new
            {
                value.TenantId,
                value.PurchaseOrderId,
                value.LineNumber
            })
                .IsUnique();
        });

        modelBuilder.Entity<Invoice>(invoice =>
        {
            invoice.ToTable("Invoices");
            invoice.HasKey(value => value.Id);
            invoice.HasAlternateKey(value => new { value.TenantId, value.Id });
            invoice.Property(value => value.TenantId).IsRequired();
            invoice.Property(value => value.VendorId);
            invoice.Property(value => value.InvoiceNumber)
                .HasMaxLength(Invoice.NumberMaxLength);
            invoice.Property(value => value.InvoiceDate);
            invoice.Property(value => value.Currency)
                .HasMaxLength(Invoice.CurrencyMaxLength)
                .IsFixedLength();
            invoice.Property(value => value.TotalAmount)
                .HasPrecision(18, 2);
            invoice.Property(value => value.ProcessingStatus)
                .HasConversion<string>()
                .HasMaxLength(Invoice.ProcessingStatusMaxLength)
                .IsRequired();
            invoice.HasIndex(value => new
            {
                value.TenantId,
                value.VendorId,
                value.InvoiceNumber
            })
                .IsUnique()
                .HasFilter("\"VendorId\" IS NOT NULL AND \"InvoiceNumber\" IS NOT NULL");
            invoice.HasIndex(value => new { value.TenantId, value.VendorId });
            invoice.HasOne<Vendor>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, value.VendorId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Restrict);
            invoice.HasMany(value => value.Lines)
                .WithOne()
                .HasForeignKey(value => new { value.TenantId, value.InvoiceId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Cascade);
            invoice.Navigation(value => value.Lines)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<InvoiceLine>(line =>
        {
            line.ToTable("InvoiceLines");
            line.HasKey(value => value.Id);
            line.HasAlternateKey(value => new { value.TenantId, value.Id });
            line.Property(value => value.TenantId).IsRequired();
            line.Property(value => value.InvoiceId).IsRequired();
            line.Property(value => value.LineNumber).IsRequired();
            line.Property(value => value.Description)
                .HasMaxLength(InvoiceLine.DescriptionMaxLength)
                .IsRequired();
            line.Property(value => value.Quantity)
                .HasPrecision(18, 4)
                .IsRequired();
            line.Property(value => value.UnitPrice)
                .HasPrecision(18, 2)
                .IsRequired();
            line.HasIndex(value => new
            {
                value.TenantId,
                value.InvoiceId,
                value.LineNumber
            })
                .IsUnique();
        });

        modelBuilder.Entity<InvoiceDocument>(document =>
        {
            document.ToTable("InvoiceDocuments");
            document.HasKey(value => value.Id);
            document.Property(value => value.TenantId).IsRequired();
            document.Property(value => value.InvoiceId).IsRequired();
            document.Property(value => value.UploadedByUserId).IsRequired();
            document.Property(value => value.OriginalFileName)
                .HasMaxLength(InvoiceDocument.FileNameMaxLength)
                .IsRequired();
            document.Property(value => value.ContentType)
                .HasMaxLength(InvoiceDocument.ContentTypeMaxLength)
                .IsRequired();
            document.Property(value => value.SizeBytes).IsRequired();
            document.Property(value => value.StorageReference)
                .HasMaxLength(InvoiceDocument.StorageReferenceMaxLength)
                .IsRequired();
            document.Property(value => value.Sha256)
                .HasMaxLength(InvoiceDocument.Sha256MaxLength)
                .IsFixedLength()
                .IsRequired();
            document.Property(value => value.UploadedAtUtc).IsRequired();
            document.HasIndex(value => new { value.TenantId, value.InvoiceId }).IsUnique();
            document.HasIndex(value => value.StorageReference).IsUnique();
            document.HasOne<Invoice>()
                .WithOne()
                .HasForeignKey<InvoiceDocument>(value => new { value.TenantId, value.InvoiceId })
                .HasPrincipalKey<Invoice>(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Cascade);
            document.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, value.UploadedByUserId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceExtractionSnapshot>(snapshot =>
        {
            snapshot.ToTable("InvoiceExtractionSnapshots");
            snapshot.HasKey(value => value.Id);
            snapshot.Property(value => value.TenantId).IsRequired();
            snapshot.Property(value => value.InvoiceId).IsRequired();
            snapshot.Property(value => value.OriginalPayload)
                .HasColumnType("jsonb")
                .HasMaxLength(InvoiceExtractionSnapshot.PayloadMaxLength)
                .IsRequired();
            snapshot.Property(value => value.ExtractedAtUtc).IsRequired();
            snapshot.Property(value => value.ReviewedPayload)
                .HasColumnType("jsonb")
                .HasMaxLength(InvoiceExtractionSnapshot.PayloadMaxLength);
            snapshot.Property(value => value.ReviewedByUserId);
            snapshot.Property(value => value.ReviewedAtUtc);
            snapshot.HasIndex(value => new { value.TenantId, value.InvoiceId }).IsUnique();
            snapshot.HasOne<Invoice>()
                .WithOne()
                .HasForeignKey<InvoiceExtractionSnapshot>(value => new { value.TenantId, value.InvoiceId })
                .HasPrincipalKey<Invoice>(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Cascade);
            snapshot.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, UserId = value.ReviewedByUserId })
                .HasPrincipalKey(value => new { value.TenantId, UserId = value.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MatchingRule>(rule =>
        {
            rule.ToTable("MatchingRules", table =>
            {
                table.HasCheckConstraint(
                    "CK_MatchingRules_AllowedTolerancePercent",
                    "\"AllowedTolerancePercent\" >= 0 AND \"AllowedTolerancePercent\" <= 100");
                table.HasCheckConstraint(
                    "CK_MatchingRules_Version",
                    "\"Version\" > 0");
                table.HasCheckConstraint(
                    "CK_MatchingRules_EffectivePeriod",
                    "\"EffectiveToUtc\" IS NULL OR \"EffectiveToUtc\" > \"EffectiveFromUtc\"");
            });
            rule.HasKey(value => value.Id);
            rule.HasAlternateKey(value => new { value.TenantId, value.Id });
            rule.Property(value => value.TenantId).IsRequired();
            rule.Property(value => value.Type)
                .HasConversion<string>()
                .HasMaxLength(MatchingRule.RuleTypeMaxLength)
                .IsRequired();
            rule.Property(value => value.AllowedTolerancePercent)
                .HasPrecision(7, MatchingRule.ToleranceScale)
                .IsRequired();
            rule.Property(value => value.Version).IsRequired();
            rule.Property(value => value.EffectiveFromUtc).IsRequired();
            rule.Property(value => value.EffectiveToUtc);
            rule.Ignore(value => value.IsCurrent);
            rule.HasIndex(value => new { value.TenantId, value.Type, value.Version })
                .IsUnique();
            rule.HasIndex(value => new { value.TenantId, value.Type })
                .IsUnique()
                .HasFilter("\"EffectiveToUtc\" IS NULL");
            rule.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(value => value.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MatchResult>(result =>
        {
            result.ToTable("MatchResults", table =>
            {
                table.HasCheckConstraint(
                    "CK_MatchResults_Prices",
                    "\"InvoiceUnitPrice\" >= 0 AND \"PurchaseOrderUnitPrice\" > 0");
                table.HasCheckConstraint(
                    "CK_MatchResults_AllowedTolerancePercent",
                    "\"AllowedTolerancePercent\" >= 0 AND \"AllowedTolerancePercent\" <= 100");
                table.HasCheckConstraint(
                    "CK_MatchResults_MatchingRuleVersion",
                    "\"MatchingRuleVersion\" > 0");
                table.HasCheckConstraint(
                    "CK_MatchResults_Outcome",
                    "(\"Status\" = 'Passed' AND \"ExceptionCode\" IS NULL) OR " +
                    "(\"Status\" = 'Failed' AND \"ExceptionCode\" = 'PriceVariance')");
            });
            result.HasKey(value => value.Id);
            result.HasAlternateKey(value => new { value.TenantId, value.Id });
            result.Property(value => value.TenantId).IsRequired();
            result.Property(value => value.InvoiceLineId).IsRequired();
            result.Property(value => value.PurchaseOrderLineId).IsRequired();
            result.Property(value => value.MatchingRuleId).IsRequired();
            result.Property(value => value.MatchingRuleVersion).IsRequired();
            result.Property(value => value.InvoiceUnitPrice)
                .HasPrecision(18, 2)
                .IsRequired();
            result.Property(value => value.PurchaseOrderUnitPrice)
                .HasPrecision(18, 2)
                .IsRequired();
            result.Property(value => value.AllowedTolerancePercent)
                .HasPrecision(7, MatchingRule.ToleranceScale)
                .IsRequired();
            result.Property(value => value.VariancePercent)
                .HasPrecision(29, MatchResult.VarianceScale)
                .IsRequired();
            result.Property(value => value.Status)
                .HasConversion<string>()
                .HasMaxLength(MatchResult.StatusMaxLength)
                .IsRequired();
            result.Property(value => value.ExceptionCode)
                .HasConversion<string>()
                .HasMaxLength(MatchResult.ExceptionCodeMaxLength);
            result.Property(value => value.EvaluatedAtUtc).IsRequired();
            result.HasIndex(value => new { value.TenantId, value.InvoiceLineId });
            result.HasIndex(value => new { value.TenantId, value.PurchaseOrderLineId });
            result.HasIndex(value => new { value.TenantId, value.MatchingRuleId });
            result.HasOne<InvoiceLine>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, value.InvoiceLineId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Restrict);
            result.HasOne<PurchaseOrderLine>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, value.PurchaseOrderLineId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Restrict);
            result.HasOne<MatchingRule>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, value.MatchingRuleId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Restrict);
            result.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(value => value.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExceptionCase>(exceptionCase =>
        {
            exceptionCase.ToTable("ExceptionCases", table =>
            {
                table.HasCheckConstraint(
                    "CK_ExceptionCases_Resolution",
                    "(\"Status\" IN ('Resolved', 'Closed') AND \"ResolvedAtUtc\" IS NOT NULL) OR " +
                    "(\"Status\" NOT IN ('Resolved', 'Closed') AND \"ResolvedAtUtc\" IS NULL)");
                table.HasCheckConstraint(
                    "CK_ExceptionCases_ResolvedAt",
                    "\"ResolvedAtUtc\" IS NULL OR \"ResolvedAtUtc\" >= \"OpenedAtUtc\"");
                table.HasCheckConstraint(
                    "CK_ExceptionCases_Severity",
                    "\"Severity\" IN ('Low', 'Medium', 'High', 'Critical')");
            });
            exceptionCase.HasKey(value => value.Id);
            exceptionCase.HasAlternateKey(value => new { value.TenantId, value.Id });
            exceptionCase.Property(value => value.TenantId).IsRequired();
            exceptionCase.Property(value => value.InvoiceId).IsRequired();
            exceptionCase.Property(value => value.PurchaseOrderId).IsRequired();
            exceptionCase.Property(value => value.MatchResultId).IsRequired();
            exceptionCase.Property(value => value.Type)
                .HasConversion<string>()
                .HasMaxLength(ExceptionCase.TypeMaxLength)
                .IsRequired();
            exceptionCase.Property(value => value.Status)
                .HasConversion<string>()
                .HasMaxLength(ExceptionCase.StatusMaxLength)
                .IsRequired();
            exceptionCase.Property(value => value.Severity)
                .HasConversion<string>()
                .HasMaxLength(ExceptionCase.SeverityMaxLength)
                .IsRequired();
            exceptionCase.Property(value => value.AssignedToUserId);
            exceptionCase.Property(value => value.OpenedAtUtc).IsRequired();
            exceptionCase.Property(value => value.ResolvedAtUtc);
            exceptionCase.HasIndex(value => new { value.TenantId, value.MatchResultId })
                .IsUnique();
            exceptionCase.HasIndex(value => new { value.TenantId, value.InvoiceId });
            exceptionCase.HasIndex(value => new { value.TenantId, value.PurchaseOrderId });
            exceptionCase.HasIndex(value => new { value.TenantId, value.Status, value.OpenedAtUtc });
            exceptionCase.HasIndex(value => new
            {
                value.TenantId,
                value.AssignedToUserId,
                value.Status
            });
            exceptionCase.HasOne<Invoice>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, value.InvoiceId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Restrict);
            exceptionCase.HasOne<PurchaseOrder>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, value.PurchaseOrderId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Restrict);
            exceptionCase.HasOne<MatchResult>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, value.MatchResultId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Restrict);
            exceptionCase.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(value => value.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            exceptionCase.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, UserId = value.AssignedToUserId })
                .HasPrincipalKey(value => new { value.TenantId, UserId = value.Id })
                .OnDelete(DeleteBehavior.Restrict);
            exceptionCase.Navigation(value => value.Evidences).UsePropertyAccessMode(PropertyAccessMode.Field);
            exceptionCase.Navigation(value => value.Analyses).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ExceptionCaseEvidence>(evidence =>
        {
            evidence.ToTable("ExceptionCaseEvidences", table =>
            {
                table.HasCheckConstraint(
                    "CK_ExceptionCaseEvidences_Type",
                    "\"Type\" IN ('InvoicePrice', 'PurchaseOrderPrice', 'PriceVariance', 'RuleTolerance')");
                table.HasCheckConstraint(
                    "CK_ExceptionCaseEvidences_TypedReference",
                    "(\"Type\" = 'InvoicePrice' AND \"InvoiceLineId\" IS NOT NULL AND \"PurchaseOrderLineId\" IS NULL AND \"MatchResultId\" IS NULL AND \"MatchingRuleId\" IS NULL) OR " +
                    "(\"Type\" = 'PurchaseOrderPrice' AND \"PurchaseOrderLineId\" IS NOT NULL AND \"InvoiceLineId\" IS NULL AND \"MatchResultId\" IS NULL AND \"MatchingRuleId\" IS NULL) OR " +
                    "(\"Type\" = 'PriceVariance' AND \"MatchResultId\" IS NOT NULL AND \"InvoiceLineId\" IS NULL AND \"PurchaseOrderLineId\" IS NULL AND \"MatchingRuleId\" IS NULL) OR " +
                    "(\"Type\" = 'RuleTolerance' AND \"MatchingRuleId\" IS NOT NULL AND \"InvoiceLineId\" IS NULL AND \"PurchaseOrderLineId\" IS NULL AND \"MatchResultId\" IS NULL)");
            });
            evidence.HasKey(value => value.Id);
            evidence.HasAlternateKey(value => new { value.TenantId, value.Id });
            evidence.Property(value => value.TenantId).IsRequired();
            evidence.Property(value => value.ExceptionCaseId).IsRequired();
            evidence.Property(value => value.Type)
                .HasConversion<string>()
                .HasMaxLength(ExceptionCaseEvidence.TypeMaxLength)
                .IsRequired();
            evidence.Property(value => value.InvoiceLineId);
            evidence.Property(value => value.PurchaseOrderLineId);
            evidence.Property(value => value.MatchResultId);
            evidence.Property(value => value.MatchingRuleId);
            evidence.Property(value => value.RecordedAtUtc).IsRequired();

            evidence.HasIndex(value => new { value.TenantId, value.ExceptionCaseId, value.Type })
                .IsUnique();
            evidence.HasIndex(value => new { value.TenantId, value.InvoiceLineId });
            evidence.HasIndex(value => new { value.TenantId, value.PurchaseOrderLineId });
            evidence.HasIndex(value => new { value.TenantId, value.MatchResultId });
            evidence.HasIndex(value => new { value.TenantId, value.MatchingRuleId });

            evidence.HasOne<ExceptionCase>()
                .WithMany(caseEntity => caseEntity.Evidences)
                .HasForeignKey(value => new { value.TenantId, value.ExceptionCaseId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Cascade);

            evidence.HasOne<InvoiceLine>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, value.InvoiceLineId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Restrict);

            evidence.HasOne<PurchaseOrderLine>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, value.PurchaseOrderLineId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Restrict);

            evidence.HasOne<MatchResult>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, value.MatchResultId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Restrict);

            evidence.HasOne<MatchingRule>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, value.MatchingRuleId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Restrict);

            evidence.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(value => value.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Resolution>(resolution =>
        {
            resolution.ToTable("Resolutions", table =>
            {
                table.HasCheckConstraint(
                    "CK_Resolutions_Type",
                    "\"Type\" = 'Override'");
                table.HasCheckConstraint(
                    "CK_Resolutions_State",
                    "\"PreviousStatus\" IN ('Open', 'WaitingAction', 'WaitingExternal') AND \"NewStatus\" = 'Resolved'");
            });
            resolution.HasKey(value => value.Id);
            resolution.HasAlternateKey(value => new { value.TenantId, value.Id });
            resolution.Property(value => value.TenantId).IsRequired();
            resolution.Property(value => value.ExceptionCaseId).IsRequired();
            resolution.Property(value => value.Type)
                .HasConversion<string>()
                .HasMaxLength(Resolution.TypeMaxLength)
                .IsRequired();
            resolution.Property(value => value.Reason)
                .HasMaxLength(Resolution.ReasonMaxLength)
                .IsRequired();
            resolution.Property(value => value.ActorUserId).IsRequired();
            resolution.Property(value => value.PreviousStatus)
                .HasConversion<string>()
                .HasMaxLength(ExceptionCase.StatusMaxLength)
                .IsRequired();
            resolution.Property(value => value.NewStatus)
                .HasConversion<string>()
                .HasMaxLength(ExceptionCase.StatusMaxLength)
                .IsRequired();
            resolution.Property(value => value.ResolvedAtUtc).IsRequired();
            resolution.HasIndex(value => new { value.TenantId, value.ExceptionCaseId })
                .IsUnique();
            resolution.HasOne<ExceptionCase>()
                .WithOne()
                .HasForeignKey<Resolution>(value => new { value.TenantId, value.ExceptionCaseId })
                .HasPrincipalKey<ExceptionCase>(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Cascade);
            resolution.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, UserId = value.ActorUserId })
                .HasPrincipalKey(value => new { value.TenantId, UserId = value.Id })
                .OnDelete(DeleteBehavior.Restrict);
            resolution.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(value => value.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditEvent>(auditEvent =>
        {
            auditEvent.ToTable("AuditEvents", table =>
            {
                table.HasCheckConstraint(
                    "CK_AuditEvents_Action",
                    "\"Action\" = 'ExceptionOverride'");
                table.HasCheckConstraint(
                    "CK_AuditEvents_State",
                    "\"PreviousStatus\" IN ('Open', 'WaitingAction', 'WaitingExternal') AND \"NewStatus\" = 'Resolved'");
            });
            auditEvent.HasKey(value => value.Id);
            auditEvent.HasAlternateKey(value => new { value.TenantId, value.Id });
            auditEvent.Property(value => value.TenantId).IsRequired();
            auditEvent.Property(value => value.ExceptionCaseId).IsRequired();
            auditEvent.Property(value => value.Action)
                .HasConversion<string>()
                .HasMaxLength(AuditEvent.ActionMaxLength)
                .IsRequired();
            auditEvent.Property(value => value.ActorUserId).IsRequired();
            auditEvent.Property(value => value.Reason)
                .HasMaxLength(Resolution.ReasonMaxLength)
                .IsRequired();
            auditEvent.Property(value => value.PreviousStatus)
                .HasConversion<string>()
                .HasMaxLength(ExceptionCase.StatusMaxLength)
                .IsRequired();
            auditEvent.Property(value => value.NewStatus)
                .HasConversion<string>()
                .HasMaxLength(ExceptionCase.StatusMaxLength)
                .IsRequired();
            auditEvent.Property(value => value.OccurredAtUtc).IsRequired();
            auditEvent.HasIndex(value => new
            {
                value.TenantId,
                value.ExceptionCaseId,
                value.OccurredAtUtc
            });
            auditEvent.HasOne<ExceptionCase>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, value.ExceptionCaseId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Cascade);
            auditEvent.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(value => new { value.TenantId, UserId = value.ActorUserId })
                .HasPrincipalKey(value => new { value.TenantId, UserId = value.Id })
                .OnDelete(DeleteBehavior.Restrict);
            auditEvent.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(value => value.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AiAnalysis>(analysis =>
        {
            analysis.ToTable("AiAnalyses", table =>
            {
                table.HasCheckConstraint(
                    "CK_AiAnalyses_Status",
                    "\"Status\" IN ('Success', 'Failed', 'Unavailable')");
                table.HasCheckConstraint(
                    "CK_AiAnalyses_ConfidenceScore",
                    "\"ConfidenceScore\" IS NULL OR (\"ConfidenceScore\" >= 0 AND \"ConfidenceScore\" <= 1)");
                table.HasCheckConstraint(
                    "CK_AiAnalyses_DurationMs",
                    "\"DurationMs\" >= 0");
            });

            analysis.HasKey(value => value.Id);
            analysis.HasAlternateKey(value => new { value.TenantId, value.Id });
            analysis.Property(value => value.TenantId).IsRequired();
            analysis.Property(value => value.ExceptionCaseId).IsRequired();
            analysis.Property(value => value.AnalysisType)
                .HasMaxLength(AiAnalysis.AnalysisTypeMaxLength)
                .IsRequired();
            analysis.Property(value => value.Status)
                .HasConversion<string>()
                .HasMaxLength(AiAnalysis.StatusMaxLength)
                .IsRequired();
            analysis.Property(value => value.Model)
                .HasMaxLength(AiAnalysis.ModelMaxLength)
                .IsRequired();
            analysis.Property(value => value.PromptVersion)
                .HasMaxLength(AiAnalysis.PromptVersionMaxLength)
                .IsRequired();
            analysis.Property(value => value.ConfidenceScore)
                .HasPrecision(5, 4);
            analysis.Property(value => value.Summary)
                .HasMaxLength(AiAnalysis.SummaryMaxLength);
            analysis.Property(value => value.RootCauseCategory)
                .HasMaxLength(AiAnalysis.RootCauseCategoryMaxLength);
            analysis.Property(value => value.RecommendedAction)
                .HasMaxLength(AiAnalysis.RecommendedActionMaxLength);
            analysis.Property(value => value.RiskAssessment)
                .HasMaxLength(AiAnalysis.RiskAssessmentMaxLength);
            analysis.Property(value => value.PromptTokens);
            analysis.Property(value => value.CompletionTokens);
            analysis.Property(value => value.TotalTokens);
            analysis.Property(value => value.DurationMs).IsRequired();
            analysis.Property(value => value.ErrorMessage)
                .HasMaxLength(AiAnalysis.ErrorMessageMaxLength);
            analysis.Property(value => value.RawPayloadJson);
            analysis.Property(value => value.CreatedAtUtc).IsRequired();

            analysis.HasIndex(value => new { value.TenantId, value.ExceptionCaseId, value.CreatedAtUtc });

            analysis.HasOne<ExceptionCase>()
                .WithMany(caseEntity => caseEntity.Analyses)
                .HasForeignKey(value => new { value.TenantId, value.ExceptionCaseId })
                .HasPrincipalKey(value => new { value.TenantId, value.Id })
                .OnDelete(DeleteBehavior.Cascade);

            analysis.HasOne<Tenant>()
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
