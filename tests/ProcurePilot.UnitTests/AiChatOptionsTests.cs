using ProcurePilot.Application.AI;

namespace ProcurePilot.UnitTests;

public sealed class AiChatOptionsTests
{
    [Fact]
    public void DisabledProviderDoesNotRequireSecrets()
    {
        var options = new AiChatOptions();

        Assert.Equal(AiChatProvider.Disabled, options.Provider);
        Assert.True(options.IsValid);
    }

    [Theory]
    [InlineData(null, "secret")]
    [InlineData("", "secret")]
    [InlineData("model", null)]
    [InlineData("model", "")]
    public void OpenAiProviderRequiresModelAndApiKey(string? model, string? apiKey)
    {
        var options = new AiChatOptions
        {
            Provider = AiChatProvider.OpenAI,
            Model = model,
            ApiKey = apiKey
        };

        Assert.False(options.IsValid);
    }

    [Fact]
    public void OpenAiProviderAcceptsConfigurationWithoutHardCodedDefaults()
    {
        var options = new AiChatOptions
        {
            Provider = AiChatProvider.OpenAI,
            Model = "configured-model",
            ApiKey = "configured-secret"
        };

        Assert.True(options.IsValid);
    }
}
