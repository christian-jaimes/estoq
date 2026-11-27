# Returns (F4) and voids

Visual reference: `5 - Dark Return Process Modal.png` (UI elements only; the layout follows Stock adjustments, see below). Returns take items back from a settled sale. Voids cancel a whole sale on the same day.

## Returns

Entry: F4, or a sale's menu on [[sales]]. All roles.

Layout (same two-column shape as [[stock-adjustments]]): the wide left column holds the receipt lookup and the tables; the narrow right column (fixed width, scrolls) holds Details (mode, reason, note) above Summary (three figures side by side). Footer: Cancel and the primary button (label follows the mode).

- Return mode (default): look up or scan a receipt; its items load into a table with a tick box on the left of each row, then price and a quantity stepper. Tick the items to return. Reason is optional.
- Exchange mode: look up or scan a receipt; its items load into the same table (tick the ones coming back). A second table below searches or scans the articles the customer takes (name, quantity stepper, total, remove). Reason is optional.
- Currency chips in the pop-up header (like the point of sale) choose the currency the refund or balance is settled in; there is no "Settle in" drop-down.
- Summary: Return shows items, subtotal and refund amount; Exchange shows credit, new items and the balance (customer pays, receives, or no difference), and, when the settle currency is not the base currency, the amount to hand over or take.

Rules:
- Start from the receipt number; this links price, payment and rate.
- Returned items go back to sellable stock as a customer return movement.
- Refund: in any currency the store can hand over, at the current rate, as its own payment line. The total cannot exceed what was paid, in base currency at the original rate.
- Exchange: scan the new items in the same window; the app nets the difference and the customer pays or receives the balance.
- No receipt: allowed at the current price, flagged for the owner.
- Return window: owner setting with a no-limit option ([[settings]]).
- Saved in one transaction: return, movements, refund or exchange.
- Reason: optional in both modes (the mockup shows it optional; the legacy flow required it).

Built so far (sample data, CPT9): receipt lookup (six digits; "#" and zeros optional), tick and quantity per item (never more than was sold less earlier returns), reason and note, Return or Exchange mode (the Core "Refund" type is the Return mode), refund share of what was paid (a discount or tax on the sale is refunded in proportion, never more than is left to refund), settle currency at the current rate, the return window (sample setting 30 days), no-receipt returns at the current price with an owner notification, and a "Return completed" confirmation. Stock goes back as `return` rows (and exchange items as `sale` rows) appended to `data/dummy_stock_movements.csv`. Sample data: a sale at the till appends `sale` rows to `data/dummy_stock_movements.csv` with the new `receipt_number` column; returns append `return` rows with the original receipt number, and the receipt lookup rebuilds sales from those rows (the file keeps no discount or payment, so an old sale's total follows the store tax setting). The new items of an exchange are logged as `sale` rows with the same receipt number and join that sale, so they can be returned or exchanged later (the sale's subtotal and total grow by their cost; the return window still counts from the original sale date). The `return` rows also carry a `movement_description` with the reason and note as `<reason> - <note>` (one of them alone when the other is empty; the reason in English). Items already returned (read from the logged `return` rows) stay in the table but are disabled and carry a "Returned" pill (or "N returned" when only part of the line came back), so they cannot be returned twice. Not built: voids, "View sale" (needs the Sales page), choosing the refund method (recorded as cash), approval or limits for no-receipt returns.

## Voids

No mockup. Started from a sale on [[sales]]. Managers and owners; a cashier requests and a manager or owner approves on the same screen.

1. Pick the sale (same day only; older sales go through returns).
2. Choose a required reason: wrong items, wrong price, entry mistake.
3. Confirm: shows the stock that returns and the payments reversed.
4. Save in one transaction: sale marked voided, reversing movements, payments reversed in their original currencies and rates.

The sale is never deleted; it stays in history with reason, approver and time. Audit-logged.
