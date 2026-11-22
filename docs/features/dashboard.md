# Dashboard

Visual reference: `8 - Estoq Analytics Dashboard Overview.png`. Landing page after [[sign-in]]. A quick, trustworthy overview of the shop for a chosen period.

## Layout

- Title "Dashboard", subtitle, "Export report" (CSV of the figures shown).
- Date-range picker in the top bar (default: this month).
- KPI cards with change vs the previous period: total sales, transactions, items sold, average sale, refunds, gross profit.
- Sales over time: area chart with Daily, Weekly granularity.
- Sales by payment method: donut with total in the centre; legend with share and amount per method.
- Top selling products: top five by units (rank, image, name, SKU, units, sales).
- Sales by category: horizontal bars with share and amount.
- Low-stock items: count with a link to [[inventory]] on the Low stock tab (not in the visual reference; kept from the agreed flow).

Tabs in the visual reference (Sales, Inventory, Products, Staff) come after the MVP; the MVP ships the overview only.

Amounts in the default currency. Empty states when data is thin.

## By role

| Element | Cashier | Manager | Owner |
|---|---|---|---|
| Sales, transactions, items sold, average sale | Own | Whole shop | Whole shop |
| Refunds | Own | Whole shop | Whole shop |
| Gross profit | No | Yes | Yes |
| Charts and top products | No | Yes | Yes |
| Low-stock items | Yes | Yes | Yes |
| Export report | No | Yes | Yes |

Gross profit is hidden from cashiers because it reveals cost. Every figure comes from the same transaction data as [[sales]] so they never disagree.
