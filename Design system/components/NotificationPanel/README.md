The notification panel is the popover that opens from the bell in the top bar. It lists stock, delivery, sales and return alerts, newest first.

**Provide:** an `eq-notif-anchor` wrapping the bell `eq-icon-btn` (`aria-expanded`, `aria-controls`, and an `aria-label` that includes the unread count) and a `section.eq-notif` (`role="dialog"`, `aria-label="Notifications"`) containing:
- `eq-notif__head`: the `eq-notif__title` and a "Mark all as read" `eq-notif__link`.
- `eq-notif__filters`: two Chips, All and Unread (with the count).
- `ul.eq-notif__list`: `eq-notif__group` headings (Today, Earlier) and one `a.eq-notif__item` per notification. Each item has an `eq-notif__icon` (with `--warning`, `--danger`, `--success` or `--info`), an `eq-notif__body` (one-line `eq-notif__text` and an `eq-notif__detail`), and an `eq-notif__time`. Unread items add an `eq-notif__unread` dot inside the time; read items add `eq-notif__item--read`.
- `eq-notif__foot`: "View all" and "Settings" ghost buttons.
- With nothing to show: `eq-notif__empty` (icon, a short headline, one line of text).

- Placement: anchored below the bell, right-aligned, 8px gap; 380px wide, list scrolls past 400px. `surface-elevated`, 1px `border`, `radius-lg`, `shadow-dialog`.
- Item: 36px round icon on `surface` with `border`, icon in the status colour; title 14px/600 (500 once read) in `text`; detail 13px in `text-secondary`; time 12px. Hover `surface`; focus 2px `focus-ring` inset.
- Unread: an 8px `gold` dot and bold title. Opening an item marks it read and goes to the related page (product, purchase order, sale).
- Status colour is never the only signal: the icon and the sentence carry the meaning. Write like the rest of estoq: state the fact with the number ("5 products are below their minimum stock level"), no exclamation marks.
- Times: relative for today ("10 min", "3 h"), "Yesterday", then a date (18 Nov / 18 nov). Locale-format amounts.
- Opens on click; closes on Escape, an outside click or picking an item. Focus moves into the panel and returns to the bell on close. The bell's dot disappears when there's nothing unread.
