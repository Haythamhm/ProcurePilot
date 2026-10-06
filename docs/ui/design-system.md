# Frontend design system

This foundation applies to the implemented application shell, sign-in page, workspace dashboard, Purchase Orders surface, invoice intake and review, and system feedback states. It is tracked by the Delivery Backlog tasks **Unifier l’UI des surfaces frontend livrées** and **Unifier les états système frontend**, and follows the existing Figma information architecture without adding product capabilities.

## Color tokens

Application-owned styles use only the approved palette:

| Token | Value | Usage |
| --- | --- | --- |
| `--pp-surface` | `#F1F6F9` | Page, card, form, and inverse text surface |
| `--pp-primary` | `#394867` | Primary actions, active navigation, secondary text |
| `--pp-ink` | `#212A3E` | Main text, sidebar, strongest emphasis |
| `--pp-muted` | `#9BA4B5` | Borders, table headings, supporting accents |

Alpha variants may only use RGB channels from one of these four tokens. Status and validation meaning must always be communicated with text or symbols, never by color alone.

The unit test `FrontendDesignSystemTests.ApplicationStylesUseOnlyTheApprovedPalette` rejects any additional hexadecimal color in application-owned CSS. Bootstrap remains a vendored dependency, but the application overrides its visible semantic tokens with this palette.

## Typography and spacing

- Use the native system sans-serif stack; no runtime font download is required.
- Page titles use a responsive `2rem`–`3rem` scale and compact line height.
- Eyebrows and section labels use uppercase text only for short navigational context.
- Cards use a `1rem` radius, a one-pixel border, and spacing in a consistent `0.25rem` rhythm.
- Financial and quantity columns use tabular numerals and right alignment.

## Icons

Navigation icons are generated with [Fontello](https://fontello.com/) from the Font Awesome 4.7 glyph set distributed by Fontello under the SIL license.

- Reproducible selection: `src/ProcurePilot.Web/wwwroot/assets/fontello/config.json`
- Local webfont: `src/ProcurePilot.Web/wwwroot/assets/fontello/font/procurepilot-icons.woff2`
- CSS classes: `src/ProcurePilot.Web/wwwroot/assets/fontello/procurepilot-icons.css`
- Upstream license notice: `src/ProcurePilot.Web/wwwroot/assets/fontello/LICENSE.txt`

The bundle intentionally contains only `home`, `purchase-order`, `logout`, `menu`, and `user`. Add icons by updating the Fontello configuration and regenerating the bundle; do not embed arbitrary SVG data URLs in component CSS.

## Accessibility and responsive behavior

- Keyboard focus uses a high-contrast, three-pixel outline.
- Interactive controls have a minimum height of approximately 44 pixels.
- The sidebar is persistent above 700 pixels and collapsible below that breakpoint.
- Data tables preserve semantic headers and use an explicitly labelled horizontal scroll region when space is constrained.
- Loading, empty, error, active, and validation states combine text with shape or icon cues.
- Motion is limited to functional loading/reconnection feedback and is disabled or simplified when reduced motion is requested.

## Dashboard and review patterns

- Summary cards use live tenant-scoped counts, short labels, and supporting text; they are not decorative hard-coded metrics.
- Activity rows expose the document, timestamp, status text, and a direct next action.
- Review pages show workflow position, source context, confidence next to the affected field, and one explicit confirmation action.
- Financial controls state what automation does not do, so the reviewer understands the boundary of the decision.
- The navigation groups work by user intent: overview, invoice operations, and reference data.

The card, grid, and responsive panel composition was informed by the public Radzen, Syncfusion, and MudBlazor dashboard examples requested for PP-12/PP-13. The application keeps its native Blazor components: adding UI frameworks for these patterns would create overlapping dependencies, and Syncfusion would also introduce a commercial licensing decision. A new framework requires a separate justified ADR.

## System feedback states

- `SystemState` provides a shared responsive pattern for route-level errors and missing pages.
- The 404 and application-error pages always provide a clear route back to the authenticated workspace.
- Technical exception details are not rendered; the support-safe request reference is preserved when available.
- The reconnection dialog announces state changes, labels its controls, preserves a 44-pixel minimum target, and respects reduced-motion preferences.
