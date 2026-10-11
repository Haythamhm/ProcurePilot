using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace ProcurePilot.Web.AI;

internal sealed class DisabledChatClient : IChatClient
{
    private const string DisabledMessage = "AI chat is disabled for this environment.";

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        Task.FromException<ChatResponse>(new InvalidOperationException(DisabledMessage));

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        throw new InvalidOperationException(DisabledMessage);
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
    }
}
