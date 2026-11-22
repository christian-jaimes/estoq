Bar charts compare amounts across categories or periods, such as monthly revenue against last year or units sold per product.

**Provide:** a `section.eq-card.eq-chart` containing `eq-chart__head` (an `eq-chart__title`, an `eq-chart__sub` naming the period, and an `eq-legend` when there are 2+ series) and one inline `<svg role="img" aria-label="…">`. In the SVG, each category is a `g.mark.m{i}` (tabindex 0, an `aria-label` with its values) holding a full-height `rect.hit` and its `path.bar` columns. All tooltips `g.tipl.t{i}` go at the END of the SVG so they draw on top.

- Columns ≤ 24px wide, 4px rounded top, square at the baseline, a 2px gap between neighbours; values start at 0.
- Colour: this period `chart-1` (gold); comparison period `chart-muted`; other series `chart-2` → `chart-4` in that order. One series is all `chart-1`, never shaded by size.
- Gridlines 1px `border`; tick labels `tick` (`text-secondary`, tabular), rounded and compact (€10K).
- Label only the value that matters (usually the latest) above its column; everything else is in the tooltip and each column's `aria-label`.
- Hover or focus a category: the others dim and the tooltip shows each value plus the change, as "▲ 12.3%" (arrow + number, not colour alone).
- Never two y-axes. For different units, use two charts.
- Spanish: format figures by locale (27.640 €, 27,6 mil €) and translate month names; never build labels by joining fragments.
