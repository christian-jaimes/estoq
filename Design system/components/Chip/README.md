Chips are compact pill choices: one from a short set (currency, period) or several as filters.

**Provide:** a `<fieldset class="eq-chips">` with an `aria-label`, and one `<label class="eq-chip">` per option containing a hidden `<input>` — `type="radio"` (same `name`) for a single choice, `type="checkbox"` for filters — then optional `eq-chip__symbol` (currency symbol) or `eq-icon`, then the label. A standalone toggle chip can be a `<button class="eq-chip" aria-pressed>`.

- 32px tall, `radius-pill`, padding `space-16`, gap `space-8` between chips; label 13px/600 with tabular numerals.
- Default: `surface`, 1px `border`, `text`; symbol in `text-secondary`. Hover: `surface-elevated`.
- Selected: `gold` fill and border, label and symbol in `on-gold` (charcoal); hover `gold-hover`. Focus: 2px `focus-ring`.
- Currency chips: symbol then ISO code ("$ USD", "€ EUR", "Bs. VES", "$ COP"); the code makes same-symbol currencies distinct. One is always selected — the store's default currency first.
- Keep to 2–6 chips in a row; for more options use a select field. Chips grow with their label (Spanish too); never truncate.
