The Selector picks one option from a small set of visual tiles, such as the payment method at checkout or the type of stock movement.

**Provide:** a `<fieldset class="eq-selector">` with an `eq-selector__legend`, and one `<label class="eq-selector__option">` per option containing a hidden `<input type="radio">` (same `name`), an icon (`eq-icon`, or an `<img class="eq-selector__logo">` for a provider's official mark) and the label. Add `eq-selector--compact` for a single-row, icon-left version. Set columns with `grid-template-columns` to suit the count (4 per row for payment methods).

- Tile: `surface`, 1px `border`, `radius-md`, min 72px tall, icon `icon-md` in `text-secondary`, label 13px/500 in `text`. Hover: `surface-elevated`.
- Selected: `gold-tint` fill, 2px `gold-ink` border, filled icon in `gold-ink`, label in `on-gold-tint`. Exactly one option is always selected; preselect the most common (Cash).
- Keyboard: it's a radio group: Tab enters, arrow keys move, focus shows the 2px `focus-ring`. Disabled options: 40% opacity.
- Labels are one or two words and wrap rather than truncate, so Spanish fits ("Pago móvil", "Transferencia").
- Payment providers (Zelle, USDT/Tether, PayPal): use each provider's official mark from its brand kit as `eq-selector__logo`, at the same 24px as the icons; never redraw it. The preview uses neutral Material Symbols as placeholders.
- Use a Selector for 2–8 options that benefit from an icon; for longer lists or text-only choices use a select field.
