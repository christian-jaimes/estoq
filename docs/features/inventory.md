# Inventory

Visual reference: `6 - inventory mock up.png`. Page. See what is in stock and open any product.

## Layout

- Title "Inventory", subtitle; actions: Import (hidden until a CSV import flow is specified, [[open-questions]] Q13), "Add Product F2" (Export lives in the table footer).
- KPI cards: total products, total variants, in stock, low stock, out of stock.
- No tabs above the table. The KPI cards filter it: In stock, Low stock and Out of stock list the products that have at least one variant with that status (same unit as the card counts); clicking the active card again, or Total products / Total variants, shows everything. The active card gets a gold outline.
- One toolbar above the table, in this order: sort (name, quantity, price, recently updated) with a direction toggle, then the product type, category and supplier / brand filters (each a multi-select dropdown that lists only values still available after the other filters, the stock card and the search (ticked values always stay listed): checkboxes, a search field on top and a fixed-height scrolling list, opened right below the button; the button shows "All ...", the single choice, or "N selected"), then a search box (shares its text with the top bar search), then Clear all (shown only while a filter or search is active).
- Table, one row per product, full bleed in its card, columns in this order: status badge, product (initial tile, name and SKU), category, brand, variant count, total stock, price (the selling price, or lowest – highest when variants differ). Rows highlight on hover; a click opens the detail panel.
- No pagination: rows load in batches as the table is scrolled near its end. A footer shows "Showing 25 of 248 products". Rows highlight on hover.
- Detail panel, 460 px wide, floating on the right for the selected row. Header: initial tile, name, SKU, an Edit icon (managers and owners; opens the product in the Add product pop-up in edit mode, see [[add-product]]) and a Close icon, no status badge. Four equal tabs: Overview (product type, category, brand, model label, selling price, cost price, margin, variants, total stock, low-stock threshold), Variants (colour circles with the size in the centre, no stock or details), Stock (each variant: size, colour, SKU, barcodes, quantity, price), History (movements: type, quantity, reason, user, time). It slides in from the right and out to the right in 100 ms.

## Rules

- Search (Ctrl+K) matches name, SKU, either barcode or supplier.
- Low and out-of-stock show a badge with text, never colour alone.
- Low-stock threshold per product, default from settings.
- There is no archive. Stock that leaves the shop is booked off with a stock adjustment ([[stock-adjustments]]). The detail panel shows a status only for low or out of stock.
- Double-clicking a row opens the same edit pop-up directly (managers and owners only; cashiers have no edit icon and double-click does nothing).
- The detail panel opens when a row is clicked, floats over the table, updates when another row is clicked and closes on a second click on the same row, a click outside or the close button.
- Cost price, margin and stock value hidden from cashiers; editing and export are manager and owner only ([[roles-and-permissions]]).
- Export sits in the table footer, on the right. It writes a CSV of exactly the rows shown: the current filters, sort and search, and only the rows loaded so far (for example 25 rows when nothing more has been scrolled in). Audit-logged.
- Lazy-loaded on scroll for 10,000 variants.
