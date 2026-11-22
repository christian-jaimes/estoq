The top bar holds search, four shortcut buttons, then the theme toggle and notifications, in three groups split by dividers.

**Provide:** `<header class="eq-topbar">` containing, in order:
1. A search field (`eq-input eq-topbar__search`) that fills all the remaining width.
2. An `eq-topbar__sep` divider, then an `eq-topbar__actions` group (`role="group"`, `aria-label="Shortcuts"`) of four round icon buttons (`eq-icon-btn eq-icon-btn--round`), then another `eq-topbar__sep`.
3. The theme toggle (`eq-icon-btn eq-theme-toggle` holding an `eq-theme-toggle__moon` and an `eq-theme-toggle__sun` icon), then the notifications `eq-icon-btn` (add `eq-icon-btn__dot` when unread).

Shortcuts, always in this order:

| # | Action | Icon | Key |
|---|---|---|---|
| 1 | Point of sale (POS) | `point_of_sale` | F1 |
| 2 | Add / receive product | `add_box` | F2 |
| 3 | Stock adjustment | `tune` | F3 |
| 4 | Return | `assignment_return` | F4 |

- Icon-only: each button has a translated `aria-label` and a tooltip with the action and its key ("Point of sale (F1)"). Each opens its flow from any page. The keys are suggestions; change them as you like.
- Round buttons are 40px circles: `surface`, 1px `border`, icon in `gold-ink`. Dividers are 1px × 24px in `border`, with a `space-12` gap on each side.
- Height 56px, `surface` ground, bottom `border`, padding `space-16`. It sits right of the sidebar and spans the content area.
- Theme toggle: one plain icon button. In light theme it shows the moon (`dark_mode`) and switches to dark; in dark theme the sun (`light_mode`) and switches to light. The right icon shows automatically from `data-theme` on the root; its translated `aria-label` names the action ("Switch to dark theme"). First launch follows the Windows theme; then remember the user's choice.
- The top bar holds nothing else: profile and language are in Settings.
- In a narrow window the search field shrinks first; the shortcuts never move into a menu.
