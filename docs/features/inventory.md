# Inventory

Visual reference: `6 - inventory mock up.png`. Page. See what is in stock and open any product.

## Layout

- Title "Inventory", subtitle; actions: Import (hidden until a CSV import flow is specified, [[open-questions]] Q13), Export (CSV), "Add Product F2".
- KPI cards: total products, total variants, in stock, low stock, out of stock.
- Tabs: All products, Low stock, Out of stock.
- Filters: category, supplier / brand, status; sort (name, quantity, price, recently updated) with direction.
- Table, one row per product: select box, image, name and SKU, category, supplier / brand, variant count, total stock, status badge (Active, Archived), row menu.
- Pagination: "Showing 1–5 of 248 products".
- Detail panel on the right for the selected row: image, name, SKU, status, Edit, more menu; tabs Overview (product type, category, brand, model label, selling price, cost price, margin, variants, total stock, low-stock threshold), Variants (size, colour, SKU, both barcodes, quantity, price), Stock (per-variant levels), History (movements: type, quantity, reason, user, time).

## Rules

- Search (Ctrl+K) matches name, SKU, either barcode or supplier.
- Low and out-of-stock show a badge with text, never colour alone.
- Low-stock threshold per product, default from settings.
- Archived products are hidden by default and shown with the status filter. Products are archived, never deleted.
- Cost price, margin and stock value hidden from cashiers; editing, archiving and export are manager and owner only ([[roles-and-permissions]]).
- Export writes exactly the filtered table to CSV and is audit-logged.
- Virtualised or paginated for 10,000 variants.
