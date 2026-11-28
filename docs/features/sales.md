# Sales

Visual reference: `7 - sales mockup.png`. Page. Look back at sales, returns and voids and export them.

## Layout

- Title "Sales", subtitle; actions: Print report, "New sale F1" (Export lives in the table footer).
- KPI cards: total sales, transactions, items sold, average sale.
- Filter bar, same as [[inventory]], in this order: sort | date filter ([[ui-implementation]] "Date filter"), payment method, staff | search in sales (receipt number, staff, product) | Clear all. Payment method and staff are multi-select dropdowns (checkboxes with a search field). There is no status filter: returned and voided sales show their status badge in the table and the status column can be sorted. The date filter is a button with the From and To pickers.
- Table: the standard full-bleed list table in a card, same look as [[inventory]] ([[ui-implementation]]): receipt number, date and time, staff, payment method badge, status badge, items, total, tax (the tax amount of the sale), row menu (view, print receipt, return).
- Detail pop-up (standard row detail, see [[ui-implementation]]: opens on a row click over the page at full height, slides in from the right, closes on the close button, a click outside or a click on the open row): receipt number, status, date, staff, items as a small table without a header row (product with SKU below, unit price, quantity shown as x<N> only when above 1, line total), subtotal, discount, tax, total, payments with currencies and rates, "Print receipt (F8)".

## Rules

- No customer column: customers are not captured in the MVP.
- Cashiers see their own sales; managers and owners see the whole shop.
- Returns and voids appear in the same list, filtered by status; voided sales remain visible.
- Export (manager, owner) sits in the table footer, on the right. It writes exactly the filtered table (every sale the filters match, not only the rows loaded so far) and is audit-logged.
- Figures use the same transaction data as the [[dashboard]] so they never disagree.
- Starting a return: [[returns-and-voids]].
- KPI cards count completed sales only, over everything the filters match (not just the visible page); voided and returned sales stay in the list but are not revenue.
- Date filter: no date filter (all dates) by default, shown as "All dates". The picker offers quick select (Today, This week, This month, This year) and a From/To range with two month calendars. A shortcut applies at once; a typed or picked range is applied by Apply. The flyout never scrolls. Weeks start on Monday. Clear all returns to all dates. There are no period tabs.
- No pagination: rows load in batches of 25 as the table is scrolled near its end. The footer shows "Showing X of Y sales" on the left. Rows highlight on hover; the open sale is tinted gold.
- Filters sit in a card toolbar above the table.
- Sort options: date and time (default, newest first), receipt number, staff, total, items, tax, status, with an ascending/descending toggle. Sorting applies to every filtered sale before rows load on scroll; ties fall back to newest first.
- Several ticked values in one filter match any of them; different filters combine. Each filter button shows its single value, or "N selected".
- Clear all shows only when a filter, the search or a non-default date range is active. It clears filters, search and the date range (back to all sales) and keeps the sort. Changing the language clears the payment method ticks.
- Export writes UTF-8 CSV with fixed numbers and ISO dates, and cells starting with `=`, `+`, `-` or `@` are neutralised.
- Return in the row menu opens the returns pop-up.
