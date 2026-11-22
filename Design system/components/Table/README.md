Tables list products, movements and transactions for scanning and comparison.

**Provide:** a `<table class="eq-table">` with `<thead>` / `<tbody>`. Add `num` to numeric cells (right-aligned, tabular), `muted` for secondary columns (SKU), and `aria-selected="true"` on a selected row.

- `surface` body, `surface-elevated` header in `caption` style, 1px `border` dividers, `radius-lg` outline.
- Cell padding `space-12` × `space-16`. Hover rows go `surface-elevated`; a selected row adds a 3px `gold` marker.
- Right-align quantities, prices and percentages; left-align text. Show status with a Badge, not coloured text.
- Don't zebra-stripe and don't centre numbers.
- Spanish: headers may wrap; values never do. Locale-format every number, price and date in the cell, and keep columns right-aligned in both languages.
