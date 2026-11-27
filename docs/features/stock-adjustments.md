# Stock adjustments (F3)

Visual reference: `4 - Stock Adjustment Modal Dashboard.png`. Pop-up. Correct stock when the system count differs from the shelf, with a record of why.

Entry: F3, or a "sold without stock" notification, which opens with that item already added. Managers and owners only.

## One adjustment, one type

An adjustment has one type and reason that apply to every item in it. Items of another type need their own adjustment: apply the current one, then start a new one.

## Steps

1. Choose the adjustment type and reason (required), from a fixed list:

   | Type | Reasons |
   |---|---|
   | Recount | Stock count difference |
   | Sold without stock | Correction after an out-of-stock sale |
   | Damaged | Damaged, unsellable |
   | Lost | Lost or stolen |

2. Scan or search items to add them. Each row shows the product (image, name, colour · size), current stock, an input for the counted quantity and a +/- stepper. Typing the counted quantity sets the difference; the stepper changes it directly. New stock is shown.
3. Optional reference (for example a count sheet number) and notes.
4. Review the summary (increases, decreases, net change) and apply.

After applying, the pop-up resets to empty and stays open for the next adjustment.

## Rules

- Every item writes a stock movement, never an overwrite; type, reason, user and time recorded; audit-logged.
- All items in one adjustment are saved in one transaction.
- Applying clears the "needs stock correction" flag and its notification for those items.
- Unsellable returned items are written off here with the Damaged type ([[returns-and-voids]]).

## Build notes (CPT8)

- The type is chosen with four chips instead of a dropdown, since the list is fixed. Each type has one reason, shown next to it and filled in with the type.
- The counted quantity is the new stock, so there is no separate "new stock" column: the input column is the new stock, and the stepper and the difference beside it stay in step with it.
- Damaged and Lost can only decrease stock (counted quantity cannot be above current stock). Recount and Sold without stock may go either way. Stock never goes below zero.
- Scanning or choosing an item that is already in the list does not add it twice.
- Sample data: each changed item appends one row to `data/dummy_stock_movements.csv` with movement type `recount`, `correction` (sold without stock), `defect` (damaged) or `lost`, and the signed difference as `stock_quantity`. Items without a difference write nothing. Reason, reference, notes and user have no columns in the sample table; the back end keeps them in the audit log.
- After applying, an "Adjustment applied" confirmation covers the whole pop-up; "New adjustment" returns to an empty form and "Close" closes the pop-up.
- Not built yet: opening the pop-up from a "sold without stock" notification with that item already added (notifications do not carry the article yet).
