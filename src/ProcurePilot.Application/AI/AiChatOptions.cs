namespace ProcurePilot.Application.AI;

public sealed class AiChatOptions
{
    public const string SectionName = "AI:Chat";

    public AiChatProvider Provider { get; init; } = AiChatProvider.Disabled;

    public string? Model { get; init; }

    public string? ApiKey { get; init; }

    public bool IsValid => Provider is AiChatProvider.Disabled
        || Provider is AiChatProvider.OpenAI
        && !string.IsNullOrWhiteSpace(Model)
        && !string.IsNullOrWhiteSpace(ApiKey);
}

public enum AiChatProvider
{
    Disabled,
    OpenAI
}
