The donut chart shows how one total splits into a few parts, such as this month's sales by category or payment methods used. Use it only for 2–5 parts that add up to a whole.

**Provide:** an `eq-card eq-chart` with the head, then `.eq-donut` holding a 200px `<svg role="img" aria-label="…">` and an `eq-legend` list (key, name, share `pct`, amount `amt`). In the SVG: a `g.center-default` with the total, then one `g.mark` per part holding its `path.seg` and a `g.hover` with its own centre text.

- Ring 28px thick with a 2px `surface` gap between parts; start at 12 o'clock and go clockwise, largest first, "Other" always last.
- Colours in order `chart-1` → `chart-4`; "Other" and anything past 4 parts is `chart-muted`.
- Centre shows the total and the period; hovering or focusing a part swaps it for that part's share and amount, and the other parts dim.
- The legend carries every name, share and amount, so colour is never the only key; text stays in text colours.
- For close values, or comparing across periods, use a BarChart instead. Never show a donut with one part; show the number as a KPI card.
