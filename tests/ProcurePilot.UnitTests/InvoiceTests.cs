using ProcurePilot.Domain.Invoices;

namespace ProcurePilot.UnitTests;

public sealed class InvoiceTests
{
    [Fact]
    public void NewInvoiceNormalizesHeaderStartsProcessingAndAddsLine()
    {
        var tenantId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var invoice = new Invoice(
            invoiceId,
            tenantId,
            Guid.NewGuid(),
            "  INV-2026-0042  ",
            new DateOnly(2026, 10, 5),
            "usd",
            1250.00m);

        var line = invoice.AddLine(
            Guid.NewGuid(),
            1,
            "  Industrial control module  ",
            10.0000m,
            125.00m);

        Assert.Equal("INV-2026-0042", invoice.InvoiceNumber);
        Assert.Equal("USD", invoice.Currency);
        Assert.Equal(InvoiceProcessingStatus.Processing, invoice.ProcessingStatus);
        Assert.Equal(1250.00m, invoice.TotalAmount);
        Assert.Equal(tenantId, line.TenantId);
        Assert.Equal(invoiceId, line.InvoiceId);
        Assert.Equal("Industrial control module", line.Description);
    }

    [Fact]
    public void InvoiceRejectsDuplicateLineNumber()
    {
        var invoice = CreateInvoice();
        invoice.AddLine(Guid.NewGuid(), 1, "First line", 1m, 10m);

        var error = Assert.Throws<InvalidOperationException>(() =>
            invoice.AddLine(Guid.NewGuid(), 1, "Duplicate line", 1m, 10m));

        Assert.Contains("already exists", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("EURO")]
    [InlineData("12A")]
    public void InvoiceRequiresThreeLetterCurrency(string currency)
    {
        Assert.Throws<ArgumentException>(() => new Invoice(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INV-1",
            new DateOnly(2026, 10, 5),
            currency,
            10m));
    }

    [Fact]
    public void InvoiceRejectsInvalidFinancialPrecision()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Invoice(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INV-1",
            new DateOnly(2026, 10, 5),
            "USD",
            10.001m));

        var invoice = CreateInvoice();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            invoice.AddLine(Guid.NewGuid(), 1, "Line", 0m, 10m));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            invoice.AddLine(Guid.NewGuid(), 1, "Line", 1m, 10.001m));
    }

    private static Invoice CreateInvoice() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "INV-1",
        new DateOnly(2026, 10, 5),
        "USD",
        10m);
}
