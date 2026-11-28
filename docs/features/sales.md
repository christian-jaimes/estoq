# Sales

Visual reference: `7 - sales mockup.png`. Page. Look back at sales, returns and voids and export them.

## Layout

- Title "Sales", subtitle; actions: Export (CSV), Print report, "New sale F1".
- Date-range picker in the top bar.
- KPI cards: total sales, transactions, items sold, average sale.
- Period tabs: All sales, Today, This week, This month, Custom.
- Filters: search in sales (receipt number, staff, product), payment method, staff, status (Completed, Returned, Voided).
- Table: receipt number, date and time, items, total, payment method badge, staff, status badge, row menu (view, print receipt, return).
- Detail pop-up (standard row detail, see [[ui-implementation]]: opens on a row click over the page at full height, slides in from the right, closes on the close button, a click outside or a click on the open row): receipt number, status, date, staff, items as a small table without a header row (product with SKU below, unit price, quantity shown as x<N> only when above 1, line total), subtotal, discount, tax, total, payments with currencies and rates, "Print receipt (F8)".

## Rules

- No customer column: customers are not captured in the MVP.
- Cashiers see their own sales; managers and owners see the whole shop.
- Returns and voids appear in the same list, filtered by status; voided sales remain visible.
- Export (manager, owner) writes exactly the filtered table and is audit-logged.
- Figures use the same transaction data as the [[dashboard]] so they never disagree.
- Starting a return: [[returns-and-voids]].
- KPI cards count completed sales only, over everything the filters match (not just the visible page); voided and returned sales stay in the list but are not revenue.
- Weeks start on Monday. Choosing a date in the range picker switches the period to Custom.
- Table pages hold 10 sales; the pager shows the first and last page and the pages next to the current one.
- Export writes UTF-8 CSV with fixed numbers and ISO dates, and cells starting with `=`, `+`, `-` or `@` are neutralised.
- Return in the row menu opens the returns pop-up.
