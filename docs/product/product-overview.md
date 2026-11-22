# Product overview

estoq is an offline-first Windows desktop point of sale and inventory app for independent clothing retailers. Tagline: "Precision in every move." Brand and voice: `Design system/estoq-design-system.md`.

## Users and context

- Small shops with one till computer, often offline, several staff sharing it.
- First markets: Venezuela and Colombia, near the border. Sales happen in USD, EUR, COP and VES with rates that change often.
- Roles: Owner, Manager, Cashier ([[roles-and-permissions]]).

## Principles

- Every core workflow works without internet. Only [[backup]] uses the network, and it degrades gracefully.
- Stock never changes silently: every change is a recorded movement ([[glossary]]).
- Sensitive actions (voids, refunds, adjustments, discounts above the cap) need a manager or owner.
- Owners never invent SKUs or barcodes; estoq generates them.
- The four daily tasks open as pop-ups from anywhere: F1 [[point-of-sale]], F2 [[add-product]], F3 [[stock-adjustments]], F4 [[returns-and-voids]].

## MVP scope

| Area | Features |
|---|---|
| Access | [[setup-wizard]], [[sign-in]], staff and roles ([[settings]]) |
| Catalogue and stock | [[add-product]] (also receives stock), [[inventory]], [[stock-adjustments]] |
| Selling | [[point-of-sale]], [[returns-and-voids]], [[sales]] history and CSV export |
| Insight | [[dashboard]] |
| Operations | Exchange rates and settings ([[settings]]), notifications and theme ([[app-shell]]), English and Spanish (both in the MVP) |
| Back end | SQLite, encryption at rest ([[architecture]]) |
| Extra | Scheduled Google Drive [[backup]] |

## Out of scope for the MVP

Purchasing and supplier orders, Transactions page, any customer data (names, emails, email receipts, marketing), a full onboarding beyond the [[setup-wizard]], [[label-printing]] until a printer is chosen, PIN sign-in, SSO, OneDrive or Dropbox, sync, business types other than clothing.

Open scope items: [[open-questions]].
