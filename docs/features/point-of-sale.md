# Point of sale (F1)

Visual reference: `3 - Dark Mode Point of Sale Interface.png`. Pop-up. Scan, adjust, take payment, done.

Entry: F1, or "New sale" on [[sales]].

## Layout

Header: wordmark, "Point of Sale", F1 badge, currency chips (USD, EUR, Bs. VES, COP; the selected one is gold), close.

Left: scan or search field (focused on open), cart table (#, image, name, SKU, price, quantity stepper, line total, delete), "Clear sale".

Right:
- Sale summary: item count, "Add discount", subtotal, discount, tax (rate and info icon), total.
- Payment method grid, in this order: Cash, Card, Transfer, Zelle, USDT, Mobile payment, PayPal, Other.
- Amount received and change due.
- Complete sale (gold, full width; key: [[open-questions]] Q16).

## Rules

- A scan matches the internal or supplier barcode; scanning again raises the quantity.
- Currency chips switch the display currency; prices are stored in base currency and converted at the current rate ([[settings]] Exchange rates).
- Discounts are percentages only. Cashiers up to the cap (default 10 %); above needs approval.
- Split payments: add payment lines in any method and currency; the app shows what is still owed. The sale completes only when payments, converted to base currency, cover the total. Change can be given in a different currency.
- Every method except cash is recorded as paid externally; no card data is stored.
- Tax: prices are tax-exclusive; one store rate, default 0 %, set in the [[setup-wizard]] and changed in [[settings]].
- Selling an out-of-stock variant is allowed with a clear warning; it raises a notification and flags the variant "needs stock correction" ([[stock-adjustments]]).
- Completing saves sale, items, payments and stock movements in one transaction, then resets for the next sale.
- No customer data is captured in the MVP.

## Receipt

Printed by default, in the store language. Email receipts: deferred, to be decided later ([[open-questions]] Q20). Layout and printer: R2.
