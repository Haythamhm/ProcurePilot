using ProcurePilot.Application.Invoices.Extraction;
using ProcurePilot.Application.Invoices.Review;

namespace ProcurePilot.UnitTests;

public sealed class InvoiceExtractionReviewPolicyTests
{
    [Fact]
    public void AssessmentBlocksMissingAndLowConfidenceCriticalFields()
    {
        var result = CreateExtraction(invoiceNumber: null, invoiceConfidence: 0.99, totalConfidence: 0.79);
        var options = new InvoiceExtractionReviewOptions
        {
            ConfidenceThreshold = 0.8,
            CriticalFields = ["InvoiceNumber", "TotalAmount"]
        };

        var assessment = InvoiceExtractionReviewPolicy.Assess(result, options);

        Assert.True(assessment.RequiresConfirmation);
        Assert.Collection(
            assessment.Issues,
            issue =>
            {
                Assert.Equal(InvoiceExtractionField.InvoiceNumber, issue.Field);
                Assert.True(issue.IsMissing);
            },
            issue =>
            {
                Assert.Equal(InvoiceExtractionField.TotalAmount, issue.Field);
                Assert.False(issue.IsMissing);
                Assert.Equal(0.79, issue.Confidence);
            });
    }

    [Fact]
    public void AssessmentAllowsCriticalFieldsAtConfiguredThreshold()
    {
        var assessment = InvoiceExtractionReviewPolicy.Assess(
            CreateExtraction("INV-42", 0.8, 0.8),
            new InvoiceExtractionReviewOptions
            {
                ConfidenceThreshold = 0.8,
                CriticalFields = ["InvoiceNumber", "TotalAmount"]
            });

        Assert.False(assessment.RequiresConfirmation);
        Assert.Empty(assessment.Issues);
    }

    [Fact]
    public void OptionsRejectUnknownCriticalField()
    {
        var options = new InvoiceExtractionReviewOptions
        {
            ConfidenceThreshold = 0.8,
            CriticalFields = ["InventedField"]
        };

        var error = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("Unknown critical", error.Message, StringComparison.Ordinal);
    }

    private static InvoiceExtractionResult CreateExtraction(
        string? invoiceNumber,
        double invoiceConfidence,
        double totalConfidence) => new()
        {
            InvoiceNumber = Field(invoiceNumber, invoiceConfidence),
            SupplierName = Field<string?>("Atlas Supply", 0.95),
            PurchaseOrderNumber = Field<string?>("PO-100", 0.95),
            InvoiceDate = Field<DateOnly?>(new DateOnly(2026, 10, 6), 0.95),
            Currency = Field<string?>("USD", 0.95),
            SubtotalAmount = Field<decimal?>(100m, 0.95),
            TaxAmount = Field<decimal?>(20m, 0.95),
            TotalAmount = Field<decimal?>(120m, totalConfidence),
            LineItems = []
        };

    private static ExtractedField<T> Field<T>(T value, double confidence) => new()
    {
        Value = value,
        Confidence = confidence
    };
}
