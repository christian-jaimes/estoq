# Roles and permissions

Three roles: Owner, Manager, Cashier. Account status: Active or Inactive. There is exactly one owner, who is the administrator and must have an email. Permissions are enforced in application services, never only by hiding controls.

Legend: Yes, No, Own (own records only), Approve (a manager or owner signs in on the same screen; the approver is recorded).

| Capability | Cashier | Manager | Owner |
|---|---|---|---|
| Make a sale (F1) | Yes | Yes | Yes |
| Discount up to the cap (default 10 %) | Yes | Yes | Yes |
| Discount above the cap | Approve | Yes | Yes |
| Sell an out-of-stock item (warning and notification) | Yes | Yes | Yes |
| Add product, receive stock (F2) | Yes | Yes | Yes |
| Browse inventory, open product read-only | Yes | Yes | Yes |
| See cost prices, margins and stock value | No | Yes | Yes |
| Edit product details and prices | No | Yes | Yes |
| Add variants, reprint labels | Yes | Yes | Yes |
| Archive or restore products | No | Yes | Yes |
| Stock adjustments (F3) | No | Yes | Yes |
| Return or exchange (F4) | Yes | Yes | Yes |
| Void a sale (same day) | Approve | Yes | Yes |
| View sales history | Own | Whole shop | Whole shop |
| Export CSV | No | Yes | Yes |
| Update exchange rates | Yes | Yes | Yes |
| Settings: Account | Yes | Yes | Yes |
| Settings: Store, Staff | No | Yes | Yes |
| Settings: Defaults and rules | No | View | Yes |
| Settings: Data (backup, restore) | No | No | Yes |
| Change the owner's account or role | No | No | Yes |
| Dashboard figures ([[dashboard]]) | Own sales, low stock | Whole shop | Whole shop |

Rules:
- Voids, refunds, adjustments, approvals and exports write an audit log entry.
- Every settings change (discount cap, tax rate, timeout, store details and so on) and every staff change (added, deactivated, activated, role changed, code regenerated) writes an audit log entry with the old value, the new value, who and when. Settings are never changed silently.
- A manager cannot deactivate the owner or change the owner's role.
- An inactive account cannot sign in.
