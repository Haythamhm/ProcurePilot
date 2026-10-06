using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProcurePilot.Application.Invoices.Extraction;
using ProcurePilot.Application.Invoices.Review;
using ProcurePilot.Application.Tenancy;
using ProcurePilot.Domain.Invoices;
using ProcurePilot.Infrastructure.Persistence;

namespace ProcurePilot.Infrastructure.Invoices;

internal sealed class InvoiceExtractionReviewService(
    DbContextOptions<ProcurePilotDbContext> dbContextOptions,
    ITenantContext tenantContext,
    IOptions<InvoiceExtractionReviewOptions> options,
    TimeProvider timeProvider) : IInvoiceExtractionReviewService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly InvoiceExtractionReviewOptions _options = ValidateOptions(options.Value);

    public async Task<InvoiceReviewReadModel?> GetAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        if (invoiceId == Guid.Empty)
        {
            return null;
        }

        await using var dbContext = CreateDbContext();
        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == invoiceId, cancellationToken);
        if (invoice is null)
        {
            return null;
        }

        var document = await dbContext.InvoiceDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(value => value.InvoiceId == invoiceId, cancellationToken);
        var snapshot = await dbContext.InvoiceExtractionSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(value => value.InvoiceId == invoiceId, cancellationToken);
        var vendors = await dbContext.Vendors
            .AsNoTracking()
            .OrderBy(value => value.Name)
            .Select(value => new InvoiceReviewVendor(value.Id, value.Name))
            .ToListAsync(cancellationToken);

        var original = snapshot is null
            ? CreateEmptyExtraction()
            : Deserialize(snapshot.OriginalPayload);
        var displayed = snapshot?.ReviewedPayload is { Length: > 0 } reviewedPayload
            ? Deserialize(reviewedPayload)
            : original;

        return new InvoiceReviewReadModel(
            invoice.Id,
            document?.OriginalFileName ?? "Invoice document",
            invoice.ProcessingStatus.ToString(),
            displayed,
            InvoiceExtractionReviewPolicy.Assess(original, _options),
            vendors,
            snapshot?.ReviewedAtUtc.HasValue == true);
    }

    public async Task SaveExtractionAsync(
        Guid invoiceId,
        InvoiceExtractionResult extraction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(extraction);
        var tenantId = tenantContext.RequireTenantId();
        await using var dbContext = CreateDbContext();
        var invoice = await dbContext.Invoices
            .SingleOrDefaultAsync(value => value.Id == invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("The invoice was not found for the active tenant.");

        if (invoice.ProcessingStatus is InvoiceProcessingStatus.Ready)
        {
            throw new InvalidOperationException("A confirmed invoice cannot be extracted again.");
        }

        var payload = Serialize(extraction);
        var now = timeProvider.GetUtcNow();
        var snapshot = await dbContext.InvoiceExtractionSnapshots
            .SingleOrDefaultAsync(value => value.InvoiceId == invoiceId, cancellationToken);
        if (snapshot is null)
        {
            snapshot = new InvoiceExtractionSnapshot(
                Guid.NewGuid(),
                tenantId,
                invoiceId,
                payload,
                now);
            dbContext.InvoiceExtractionSnapshots.Add(snapshot);
        }
        else
        {
            snapshot.ReplaceOriginal(payload, now);
        }

        invoice.MarkReviewRequired();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ConfirmAsync(
        ConfirmInvoiceExtractionCommand command,
        Guid reviewedByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (reviewedByUserId == Guid.Empty)
        {
            throw new InvoiceReviewValidationException("The authenticated reviewer is missing.");
        }

        var tenantId = tenantContext.RequireTenantId();
        await using var dbContext = CreateDbContext();
        var reviewerExists = await dbContext.Users
            .AnyAsync(user => user.Id == reviewedByUserId, cancellationToken);
        if (!reviewerExists)
        {
            throw new InvoiceReviewValidationException(
                "The reviewer does not belong to the active workspace.");
        }

        var invoice = await dbContext.Invoices
            .Include(value => value.Lines)
            .SingleOrDefaultAsync(value => value.Id == command.InvoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("The invoice was not found for the active tenant.");
        var vendor = await dbContext.Vendors
            .SingleOrDefaultAsync(value => value.Id == command.VendorId, cancellationToken)
            ?? throw new InvoiceReviewValidationException("Select a vendor from the active workspace.");
        var snapshot = await dbContext.InvoiceExtractionSnapshots
            .SingleOrDefaultAsync(value => value.InvoiceId == command.InvoiceId, cancellationToken);
        var original = snapshot is null
            ? CreateEmptyExtraction()
            : Deserialize(snapshot.OriginalPayload);
        var assessment = InvoiceExtractionReviewPolicy.Assess(original, _options);
        if (assessment.RequiresConfirmation && !command.ConfirmLowConfidenceFields)
        {
            throw new InvoiceReviewValidationException(
                "Confirm that every flagged critical field was checked before continuing.");
        }

        if (command.Lines.Count == 0)
        {
            throw new InvoiceReviewValidationException("Add at least one invoice line.");
        }

        try
        {
            invoice.ConfirmExtraction(
                vendor.Id,
                command.InvoiceNumber,
                command.InvoiceDate,
                command.Currency,
                command.TotalAmount,
                command.Lines
                    .Select(line => new ReviewedInvoiceLine(
                        line.LineNumber,
                        line.Description,
                        line.Quantity,
                        line.UnitPrice))
                    .ToArray());
        }
        catch (ArgumentException exception)
        {
            throw new InvoiceReviewValidationException(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvoiceReviewValidationException(exception.Message);
        }

        dbContext.InvoiceLines.AddRange(invoice.Lines);

        var reviewed = new InvoiceExtractionResult
        {
            InvoiceNumber = Confirmed<string?>(command.InvoiceNumber),
            SupplierName = Confirmed<string?>(vendor.Name),
            PurchaseOrderNumber = Confirmed(command.PurchaseOrderNumber),
            InvoiceDate = Confirmed<DateOnly?>(command.InvoiceDate),
            Currency = Confirmed<string?>(command.Currency.Trim().ToUpperInvariant()),
            SubtotalAmount = Confirmed(command.SubtotalAmount),
            TaxAmount = Confirmed(command.TaxAmount),
            TotalAmount = Confirmed<decimal?>(command.TotalAmount),
            LineItems = command.Lines
                .OrderBy(line => line.LineNumber)
                .Select(line => new InvoiceLineExtraction
                {
                    LineNumber = line.LineNumber,
                    Description = Confirmed<string?>(line.Description),
                    Quantity = Confirmed<decimal?>(line.Quantity),
                    UnitPrice = Confirmed<decimal?>(line.UnitPrice),
                    LineTotal = Confirmed<decimal?>(CalculateLineTotal(line.Quantity, line.UnitPrice))
                })
                .ToArray()
        };

        if (snapshot is null)
        {
            snapshot = new InvoiceExtractionSnapshot(
                Guid.NewGuid(),
                tenantId,
                command.InvoiceId,
                Serialize(original),
                timeProvider.GetUtcNow());
            dbContext.InvoiceExtractionSnapshots.Add(snapshot);
        }

        snapshot.MarkReviewed(Serialize(reviewed), reviewedByUserId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ExtractedField<T> Confirmed<T>(T value) => new()
    {
        Value = value,
        Confidence = 1
    };

    private static decimal CalculateLineTotal(decimal quantity, decimal unitPrice) =>
        decimal.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);

    private ProcurePilotDbContext CreateDbContext() =>
        new(dbContextOptions, tenantContext);

    private static InvoiceExtractionReviewOptions ValidateOptions(InvoiceExtractionReviewOptions options)
    {
        options.Validate();
        return options;
    }

    private static string Serialize(InvoiceExtractionResult extraction) =>
        JsonSerializer.Serialize(extraction, SerializerOptions);

    private static InvoiceExtractionResult Deserialize(string payload) =>
        JsonSerializer.Deserialize<InvoiceExtractionResult>(payload, SerializerOptions)
        ?? throw new InvalidOperationException("Stored invoice extraction payload is invalid.");

    private static InvoiceExtractionResult CreateEmptyExtraction() => new()
    {
        InvoiceNumber = Empty<string?>(),
        SupplierName = Empty<string?>(),
        PurchaseOrderNumber = Empty<string?>(),
        InvoiceDate = Empty<DateOnly?>(),
        Currency = Empty<string?>(),
        SubtotalAmount = Empty<decimal?>(),
        TaxAmount = Empty<decimal?>(),
        TotalAmount = Empty<decimal?>(),
        LineItems = []
    };

    private static ExtractedField<T> Empty<T>() => new()
    {
        Value = default!,
        Confidence = 0
    };
}
