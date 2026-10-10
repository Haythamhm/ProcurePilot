using ProcurePilot.Domain.Exceptions;

namespace ProcurePilot.Application.Exceptions;

public sealed class ExceptionManagementOptions
{
    public const string SectionName = "ExceptionManagement";

    public ExceptionSeverity DefaultPriceVarianceSeverity { get; set; }
}
