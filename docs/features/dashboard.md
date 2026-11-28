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

## Build status (CPT12)

Built on sample data: title with the period, range chips (Today, Last 7 days, Last 30 days, This month, Last month; default This month), the six KPI cards with the change against the previous period of the same length, sales over time (Daily or Weekly, Monday to Sunday), sales by payment method (the four biggest, the rest as "Other methods"), top five products by units, sales by category, low-stock items (at or below 5 units for now, until Settings stores the threshold) with a "View all" link, and Export report (a CSV of the figures shown). The role table above is enforced by `DashboardAccess` from the permission matrix. All figures come from `Estoq.Core.Dashboard`, tested without a database, over the sale and return rows of `dummy_stock_movements.csv`.

Sample-data limits: the movements keep no payment method and no seller, so the payment split is a fixed repeatable mix worked out from the transaction, and a cashier's "own" figures are empty until sales carry their seller. The pictures in Top selling products are a placeholder tile (the sample tables hold none). The "View all" link opens Inventory but cannot select the Low stock tab until that page lands. The date picker is the chip row in the page header, not the shell's top bar.

