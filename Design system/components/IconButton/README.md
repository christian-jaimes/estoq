Icon buttons run a single action shown by an icon alone: round for quick actions and shortcuts, plain for toolbars and the top bar.

**Provide:** a `<button class="eq-icon-btn">` with one Material Symbols `<span class="eq-icon">` and an `aria-label` (also used as the tooltip). Add `eq-icon-btn--round` for the outlined circle, `eq-icon-btn--sm` for the 32px size, `aria-pressed="true"` for a selected toggle, and `eq-icon-btn__dot` for an unread marker.

- Round: 40px circle (32px small), `surface` fill, 1px `border`, icon in `gold-ink` at `icon-sm`. Hover: `surface-elevated` with a `gold-ink` ring. Selected: `gold` fill, `on-gold` icon, filled glyph.
- Plain: 36px (`control-height`), transparent, `radius-md`, icon in `text-secondary` at `icon-md`; hover `surface-elevated` and `text`.
- Focus: 2px `focus-ring`, 2px offset. Disabled: 40% opacity.
- Common glyphs: `shopping_cart` sales, `deployed_code` products, `local_shipping` incoming stock, `sell` pricing, `filter_list`, `more_vert`, `notifications`.
- Always give an `aria-label` that names the action, translated. Where the meaning isn't obvious, use a Button with a text label instead.
- Don't mix round and plain icon buttons in the same row of related actions.
