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
