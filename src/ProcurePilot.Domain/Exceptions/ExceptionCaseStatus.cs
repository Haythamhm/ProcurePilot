namespace ProcurePilot.Domain.Exceptions;

public enum ExceptionCaseStatus
{
    New = 1,
    Analyzing = 2,
    Open = 3,
    WaitingAction = 4,
    WaitingExternal = 5,
    Resolved = 6,
    Closed = 7
}
