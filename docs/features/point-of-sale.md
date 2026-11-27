# Point of sale (F1)

Visual reference: `3 - Dark Mode Point of Sale Interface.png`. Pop-up. Scan, adjust, take payment, done.

Entry: F1, or "New sale" on [[sales]].

## Layout

Header: wordmark, "Point of Sale", temporary F1 hint (shown for four seconds on open), currency chips (USD, EUR, Bs. VES, COP; the selected one is gold), close.

Left: scan or search field (focused on open), cart table (#, image, name, SKU, price, quantity stepper, line total, delete), "Clear sale".

Right:
- Payment method grid first, ordered by completed store usage (most used first; ties follow Cash, Card, Transfer, Zelle, USDT, Mobile payment, PayPal, Other).
- Sale summary below it: item count, "Add discount", subtotal, discount, tax (rate and info icon), total, then payment rows and amount to return when payments exist.
- Complete sale (gold, full width; key: [[open-questions]] Q16).

## Behaviour in the app

- The pop-up has its own layout and does not use the shared frame ([[ui-implementation]], Pop-up pattern).
- Scan field: Enter submits only the field value. An exact barcode or SKU match is added directly; otherwise the first search match is added. Typing two or more characters searches article names, barcodes and SKUs and lists up to six matches; suggestions show the name, SKU and barcode. Click one to add it. No match shows a message under the field. This follows the app-wide scanner and Enter behavior ([[ui-implementation]]).
- Quantity: stepper, or type a number (1 to 9999); 0 removes the line.
- Add discount opens a compact flyout with one horizontal row of active store discount presets, followed by a divider and the approval-limit hint. Each preset button has equal width; the flyout resizes with the active preset count. The sample POS reads the owner-managed active status from `data/dummy_discount.csv`; Settings will manage these presets later ([[settings]]). Selecting an allowed preset applies it immediately and recalculates the sale. Above the cashier cap, the flyout asks for a manager or owner six-digit code before applying.
- Selecting a payment tile opens a numeric amount flyout below it with the currency symbol and submit icon. Enter or the submit icon records only that amount under the selected method; it does not complete the sale. Sale completion remains Ctrl+Enter or the Complete sale button. Invalid characters follow the shared input feedback rule ([[ui-implementation]]). Selecting another method adds another payment. Payment rows show a delete icon, method and right-aligned amount.
- Cart rows follow the shared table pattern: elevated header, aligned columns, full-width alternating row bands and thin row separators ([[ui-implementation]]).
- The summary shows a payment log with aligned method and amount headers and full-width alternating row bands after Total's divider. Payment rows keep their method and amount fixed; hovering a row overlays a red trash button on the amount. Show "Amount to return" only when change is due and "Remaining to pay" only while a nonzero balance remains after payment rows are added. Allow sale completion as soon as payments cover the total, even when cash change is due; show the overpayment reminder so the cashier knows to return the change in cash.
- The Complete sale and clear sale buttons stay pinned below the scrolling payment and summary cards. The cart panel has no footer divider.
- Complete sale: Ctrl+Enter or the button. Its shortcut badge is shown for four seconds after opening the POS pop-up. Enabled when the cart has items and payments cover the total. The completion overlay shows the sale number and a receipt-style, scrollable list of line items (article name, unit price and quantity) and recorded payments (method and amount), followed by tax, sale total and amount paid. It offers Close, Print receipt (opens the system print dialog), and New sale.
- Closing the POS with its close button, the scrim, or F1 clears the in-progress cart, payments and discount. F1 toggles the POS pop-up. Clear sale (with confirmation) is also available while it is open.
- Out-of-stock or partial-stock lines show a warning under the item name; completing raises a "sold without stock" notification ([[app-shell]]).
- Cart rows show the product photo when available; otherwise the photo square shows the product name's first letter until [[add-product]] stores images.
- "Add discount" is a gold-tint button, not a second gold fill: one gold primary per view ([[ui-implementation]]).
- Zelle, USDT and PayPal use a letter on a brand-colour disc, the only fixed colours besides the scrim.

## Core rules

All in `estoq.Core/Sales`, unit-tested in `SaleRulesTests`:

| Rule | Type |
|---|---|
| Rounding: two decimals, half away from zero | `Money` |
| Rates per base unit, conversion both ways, amount that covers a balance | `ExchangeRates` |
| Cart: add or raise quantity, set, remove, subtotal | `SaleCart` |
| Subtotal, discount, tax (added or included), total; discount before tax | `SaleTotals` |
| Discount cap, approval above the cap by role | `DiscountPolicy` |
| Paid, remaining, change; cashier returns change in cash | `PaymentSettlement` |
| Enough, partial or no stock; units sold without stock | `StockCheck` |
| Payment method tie order | `PaymentMethods` |

## Stubs until the back end and sign-in

`IPointOfSaleService` (`SamplePointOfSaleService`) loads the sellable articles from `data/dummy_article.csv` (active rows, supplier barcode may be empty), plus store settings, the role (owner) and the sale save. Stock on hand is the sum of an article's `stock_quantity` in `data/dummy_stock_movements.csv` across locations (never below zero). Article names and barcodes are searchable; exact barcode or SKU entry adds the item directly, and partial name/barcode/SKU matches appear in suggestions. Stock drops per sale for the session only; completed payment-method counts persist locally and sort the methods on the next sale. The sample store charges 16 % tax, prices excluded, to exercise the tax rows; the setup default stays 0 %. The sample manager approval code is a placeholder.

## Rules

- A scan matches the internal or supplier barcode; scanning again raises the quantity.
- Currency chips switch the display currency; prices are stored in base currency and converted at the current rate ([[settings]] Exchange rates).
- Discounts are percentages only. Cashiers up to the cap (default 10 %); above needs approval.
- Split payments: add payment lines in any method and currency; the app shows what is still owed. The sale completes once payments, converted to base currency, cover the total. If overpaid, the cashier returns the change in cash.
- Every method except cash is recorded as paid externally; no card data is stored.
- Tax: prices are tax-exclusive; one store rate, default 0 %, set in the [[setup-wizard]] and changed in [[settings]].
- Selling an out-of-stock variant is allowed with a clear warning; it raises a notification and flags the variant "needs stock correction" ([[stock-adjustments]]).
- Completing saves sale, items, payments and stock movements in one transaction, then resets for the next sale.
- No customer data is captured in the MVP.

## Receipt

Printed by default, in the store language. Email receipts: deferred, to be decided later ([[open-questions]] Q20). Layout and printer: R2.
