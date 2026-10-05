# Invoice extraction boundary

PP-11 introduces a provider-neutral invoice extraction boundary for `FR-INV-004`.

- `IInvoiceExtractor` is the application contract.
- `ChatClientInvoiceExtractor` depends only on `Microsoft.Extensions.AI.IChatClient`, so a concrete AI provider can be replaced at composition time.
- Supplier documents are sent as untrusted binary data. The system prompt explicitly rejects instructions embedded in a document.
- The response is constrained to a JSON schema and validated before it reaches later workflow steps.
- Extraction returns confidence per field. It does not perform matching, authorization, state transitions, or any financial decision.

Provider configuration, document storage/upload, persistence, human review, and matching remain separate backlog slices.
