using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ProcurePilot.Application.AI;
using ProcurePilot.Application.Invoices.Extraction;

namespace ProcurePilot.Web.AI;

internal static class AiChatServiceCollectionExtensions
{
    public static IServiceCollection AddProcurePilotAiChat(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<AiChatOptions>()
            .Bind(configuration.GetSection(AiChatOptions.SectionName))
            .Validate(
                options => options.IsValid,
                "AI chat requires a supported provider and non-empty model and API key when enabled.")
            .ValidateOnStart();

        services.AddSingleton<IChatClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AiChatOptions>>().Value;
            return options.Provider switch
            {
                AiChatProvider.Disabled => new DisabledChatClient(),
                AiChatProvider.OpenAI => new OpenAI.Chat.ChatClient(
                        options.Model!,
                        options.ApiKey!)
                    .AsIChatClient(),
                _ => throw new InvalidOperationException(
                    $"AI chat provider '{options.Provider}' is not supported.")
            };
        });
        services.AddScoped<IInvoiceExtractor, ChatClientInvoiceExtractor>();

        return services;
    }
}
