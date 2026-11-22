Buttons trigger actions; one primary (gold) button per view marks the main action.

**Provide:** a `<button>` with `eq-btn` plus one variant — `eq-btn--primary`, `eq-btn--secondary`, `eq-btn--ghost` or `eq-btn--danger` — and optionally `eq-btn--sm`. Label in sentence case, verb first ("Add product", "Receive stock"). An optional leading Material Symbols icon goes in `<span class="eq-icon">`.

- Primary: `gold` fill, `on-gold` label, `gold-hover` / `gold-pressed`. Use once per screen or dialog.
- Secondary: `surface` with `border` — for the rest (Export, Filter, Adjust).
- Ghost: cancel and low-emphasis actions in toolbars and dialogs.
- Danger: destructive actions only, usually inside a confirmation dialog. White on `danger` is 4.2:1 — keep the label at 600 weight.
- Height `control-height` (36px), `radius-md`, focus ring 2px `focus-ring`.
- Don't put two gold buttons side by side; don't use gold for navigation links.
- Spanish: infinitive labels ("Añadir producto", "Guardar cambios"). Width follows the label — never fix a button's width; Spanish labels run longer.
