# estoq documentation

Single source of truth for what estoq does and how it is built. Update a doc in the same change as the code it describes.

The [[roadmap]] sets what is built and when and tracks what is done; these docs specify how each feature works. Mockups in `docs/mockups/` are a visual reference for UI elements only, so differences from the docs are expected. Undecided items: [[open-questions]].

## Plan

| Doc | Purpose |
|---|---|
| [[roadmap]] | Build order, checklist and status |
| [[open-questions]] | Decisions waiting on the product owner |

## Product

| Doc | Purpose |
|---|---|
| [[product-overview]] | Vision, users, MVP scope, out of scope |
| [[roles-and-permissions]] | Owner, Manager, Cashier capability matrix |
| [[glossary]] | Shared vocabulary |

## Features (front end order of the roadmap)

| Doc | Visual reference |
|---|---|
| [[setup-wizard]] | `0 - Elegant Dark Store Setup Wizard.png` |
| [[sign-in]] | `1 - Premium Dark Gold Login Screen.png`, `1.1 - estoQ Dark Login Screen.png` |
| [[app-shell]] (sidebar, top bar, shortcuts, notifications, theme) | all page mockups |
| [[add-product]] (F2) | `2 - Dark Gold-Accented Product Form.png` |
| [[point-of-sale]] (F1) | `3 - Dark Mode Point of Sale Interface.png` |
| [[stock-adjustments]] (F3) | `4 - Stock Adjustment Modal Dashboard.png` |
| [[returns-and-voids]] (F4) | `5 - Dark Return Process Modal.png` |
| [[inventory]] | `6 - inventory mock up.png` |
| [[sales]] | `7 - sales mockup.png` |
| [[dashboard]] | `8 - Estoq Analytics Dashboard Overview.png` |
| [[settings]] (Account, Store, Staff, Exchange rates) | `9`, `10`, `11` |
| [[backup]] (extra feature) | none |
| [[label-printing]] (future) | none |

## Technical

| Doc | Purpose |
|---|---|
| [[architecture]] | Projects, layers, runtime, cross-cutting rules |
| [[data-model]] | Naming rules and planned schema |
| [[development]] | Toolchain, build, run, CI, troubleshooting |
| [[ui-implementation]] | How the design system maps to XAML; pop-up pattern |
| [[encryption]] | Encryption at rest: library, setup, keys |
| [[decisions]] | Architecture decision log |

The visual spec lives in `Design system/` (`estoq-design-system.md`, `tokens.json`, components).

## Conventions

- One topic per file; link with `[[wikilinks]]` instead of copying.
- Clear and concise, no filler. No personal data, secrets or emojis.
- A product rule goes in its feature doc; a technical decision goes in [[decisions]].
