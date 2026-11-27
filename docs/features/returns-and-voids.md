# Returns (F4) and voids

Visual reference: `5 - Dark Return Process Modal.png`. Returns take items back from a settled sale. Voids cancel a whole sale on the same day.

## Returns

Entry: F4, or a sale's menu on [[sales]]. All roles.

Layout: left: original sale (receipt number, date, "View sale"), return items (tick, image, name, colour · size, price, quantity stepper), return reason (dropdown, optional) and note. Right: return type (Refund or Exchange, radio cards), refund summary (subtotal, refund amount). Footer: Cancel, "Proceed with refund" (label follows the type).

Rules:
- Start from the receipt number; this links price, payment and rate.
- Returned items go back to sellable stock as a customer return movement.
- Refund: in any currency the store can hand over, at the current rate, as its own payment line. The total cannot exceed what was paid, in base currency at the original rate.
- Exchange: scan the new items in the same window; the app nets the difference and the customer pays or receives the balance.
- No receipt: allowed at the current price, flagged for the owner.
- Return window: owner setting with a no-limit option ([[settings]]).
- Saved in one transaction: return, movements, refund or exchange.
- Reason: optional in the mockup, required in the legacy flow. Working default: optional.

Built so far (sample data, CPT9): receipt lookup (six digits; "#" and zeros optional), tick and quantity per item (never more than was sold less earlier returns), reason and note, Refund or Exchange, refund share of what was paid (a discount or tax on the sale is refunded in proportion, never more than is left to refund), settle currency at the current rate, the return window (sample setting 30 days), no-receipt returns at the current price with an owner notification, and a "Return completed" confirmation. Stock goes back as `return` rows (and exchange items as `sale` rows) appended to `data/dummy_stock_movements.csv`. Not built: voids, "View sale" (needs the Sales page), choosing the refund method (recorded as cash), approval or limits for no-receipt returns.

## Voids

No mockup. Started from a sale on [[sales]]. Managers and owners; a cashier requests and a manager or owner approves on the same screen.

1. Pick the sale (same day only; older sales go through returns).
2. Choose a required reason: wrong items, wrong price, entry mistake.
3. Confirm: shows the stock that returns and the payments reversed.
4. Save in one transaction: sale marked voided, reversing movements, payments reversed in their original currencies and rates.

The sale is never deleted; it stays in history with reason, approver and time. Audit-logged.
