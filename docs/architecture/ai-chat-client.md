# AI chat client

PP-24 wires the existing provider-neutral `Microsoft.Extensions.AI.IChatClient` boundary at the Web composition root.

## Configuration

The default configuration is safe for local and CI environments:

```json
{
  "AI": {
    "Chat": {
      "Provider": "Disabled"
    }
  }
}
```

To enable the OpenAI provider, supply configuration through a secret manager or environment variables. Do not add the key to an appsettings file:

```text
AI__Chat__Provider=OpenAI
AI__Chat__Model=<configured-model>
AI__Chat__ApiKey=<secret>
```

Provider, model, and secret are validated at application startup. `Disabled` deliberately resolves an `IChatClient` that rejects calls with a stable availability error, allowing the application and non-AI workflows to run without a provider secret.

## Boundaries

- Domain and Application reference only `IChatClient`; OpenAI types remain in the Web composition root.
- PP-24 does not invoke the provider, add a prompt, persist analysis, or expose an AI action in the UI.
- Supplier documents remain untrusted data. The existing extractor instructs the model to ignore document-borne instructions and never delegates arithmetic, authorization, matching, or state transitions.
- API keys are configuration secrets and must never be logged or committed.
