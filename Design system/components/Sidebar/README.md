The persistent, collapsible left sidebar holds the logo, primary navigation, and the signed-in user with a link to Settings.

**Provide:** `<nav class="eq-sidebar">` (add `eq-sidebar--collapsed` for the rail) with, in this order:
1. **Top:** `eq-sidebar__brand` holding `eq-sidebar__lockup` (`eq-logo-mark` + `eq-logo-wordmark`, `role="img" aria-label="estoq"`). The collapse control is a `<button class="eq-sidebar__collapse">` placed directly in the nav: a 24px circle that floats on the sidebar's right border, level with the logo, with `chevron_left` (collapse) or `chevron_right` (expand), `aria-expanded`, and a translated `aria-label`.
2. **Navigation:** `<a class="eq-nav">` items in this order — Dashboard, Inventory, Sales. Each has an `eq-icon`, the label in `eq-nav__label` and a `title` (the tooltip when collapsed). Mark the current page with `aria-current="page"`.
3. **Divider:** `eq-sidebar__divider`, which pushes the user block to the bottom.
4. **Bottom:** `eq-sidebar__user` with the user's photo in `eq-avatar` (an `<img>`, or initials when there's no photo), `eq-sidebar__name` (name in `<strong>`, role below), and a Settings `eq-icon-btn` (`settings` icon).

- Expanded: `sidebar-width`, full lockup and labels. Collapsed: 64px rail with only the Q mark, nav icons and the avatar; labels, the name and the Settings button hide. In the rail the avatar itself opens Settings (tooltip "Settings").
- `surface` ground, right `border`. Items 36px, `radius-sm`, `icon-md` icons in `text-secondary`; hover `surface-elevated`.
- Active item: `surface-elevated`, a 3px `gold` indicator, label at 600, icon filled in `gold-ink`.
- No counts or badges on nav items; numbers live on the pages themselves.
- Long names truncate with an ellipsis; the full name is in the avatar's tooltip.
- Remember the collapsed or expanded choice between sessions. Language, theme defaults and the profile are changed in Settings.
- Spanish: Panel, Inventario, Ventas; Ajustes for Settings; tooltips in the collapsed rail are translated too.
