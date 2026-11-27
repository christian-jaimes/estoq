# Sales

Visual reference: `7 - sales mockup.png`. Page. Look back at sales, returns and voids and export them.

## Layout

- Title "Sales", subtitle; actions: Export (CSV), Print report, "New sale F1".
- Date-range picker in the top bar.
- KPI cards: total sales, transactions, items sold, average sale.
- Period tabs: All sales, Today, This week, This month, Custom.
- Filters: search in sales (receipt number, staff, product), payment method, staff, status (Completed, Returned, Voided).
- Table: receipt number, date and time, items, total, payment method badge, staff, status badge, row menu (view, print receipt, return, void).
- Detail panel: receipt number, status, date, staff, items (image, name, SKU, colour / size, quantity × price, line total), subtotal, discount, tax, total, payments with currencies and rates, "Print receipt (F8)".

## Rules

- No customer column: customers are not captured in the MVP.
- Cashiers see their own sales; managers and owners see the whole shop.
- Returns and voids appear in the same list, filtered by status; voided sales remain visible.
- Export (manager, owner) writes exactly the filtered table and is audit-logged.
- Figures use the same transaction data as the [[dashboard]] so they never disagree.
- Starting a return or void: [[returns-and-voids]].
- KPI cards count completed sales only, over everything the filters match (not just the visible page); voided and returned sales stay in the list but are not revenue.
- Weeks start on Monday. Choosing a date in the range picker switches the period to Custom.
- Table pages hold 10 sales; the pager shows the first and last page and the pages next to the current one.
- Export writes UTF-8 CSV with fixed numbers and ISO dates, and cells starting with `=`, `+`, `-` or `@` are neutralised.
- Void in the row menu is a placeholder until [[returns-and-voids]] is built; Return opens the returns pop-up.
