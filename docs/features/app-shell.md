# App shell

The frame around every page: sidebar, top bar, pop-ups, notifications panel and theme. Visible in mockups 1, 6, 7, 8. Implementation details: [[ui-implementation]].

## Splash

At launch, before the wizard or the shell: always dark background, gold logo and wordmark (the gold lockup) centred, shown for 2 seconds (`Views/SplashPage`, `MainWindow.ShowSplash`). Then [[setup-wizard]] if no owner exists, otherwise the shell.

## Sidebar

Floating, collapsible (240 px, 64 px collapsed). Wordmark (Q mark when collapsed).

- Top: Dashboard, Inventory, Sales. Active item has a gold fill.
- Bottom: divider, then Account settings (user avatar, name and role; opens [[settings]]). Expanded: gear icon on the right. Collapsed: only the avatar; the gear appears over it on hover.

Purchasing, Transactions and Analytics in the visual references are not part of the MVP.

## Top bar

- Search with Ctrl+K and Ctrl+F; placeholder changes per page (products, SKU, barcode; or sales by receipt, staff, product).
- Four round gold-outline shortcut buttons with tooltips showing the key.
- Date or date-range picker on pages that filter by date.
- Theme toggle and notifications bell.

## Shortcuts

| Key | Opens |
|---|---|
| F1 | [[point-of-sale]] |
| F2 | [[add-product]] |
| F3 | [[stock-adjustments]] |
| F4 | [[returns-and-voids]] |
| Ctrl+K, Ctrl+F | Focus search |
| Esc | Close the open pop-up |

Decided by the product owner on 2026-11-21. Only one pop-up is open at a time; another F key replaces it, the same key does nothing.

Inside pop-ups, local keys (for example "Complete Sale F2" in the POS mockup) conflict with the global keys. Working default: Complete sale uses Ctrl+Enter ([[open-questions]] Q16). Print receipt is F8 on the Sales page.

## Notifications panel

Pop-up anchored below the bell, right-aligned, 380 px wide, list scrolls past 400 px (design system `NotificationPanel`). Closes on Esc, outside click.

Notification types:
- Low stock (variant at or below its threshold).
- Sold without stock (opens [[stock-adjustments]] on that item).
- Backup stale or failed ([[backup]]).

Each item: icon, text with the figure, detail, relative time, read state (gold dot and full-strength text while unread; dimmed once read).

- Bell shows a gold dot while anything is unread.
- Clicking an item marks it read. Navigating to the source comes with the target screens.
- "Mark all as read" marks every item read.
- Read items show a clear button; "Clear read" removes all read items. Unread items cannot be cleared.
- Empty state: "No notifications".

Front end first: `Services/INotificationService` is the source; `MockNotificationService` holds in-memory samples until the back end exists. View models: `NotificationsViewModel`, `NotificationItemViewModel`. Not built yet: All/Unread filter chips, "View all" and "Settings" footer, grouping by Today and Earlier.

## Theme

Light, dark, or system, per user. Toggle in the top bar (moon in light, sun in dark) and in [[settings]] Account. Tokens for both themes come from `Design system/tokens.json`.
