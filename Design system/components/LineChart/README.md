Line charts show change over time: daily sales, stock level across weeks, sales by channel.

**Provide:** the same `eq-card eq-chart` shell as BarChart (head with a line-key `eq-legend`, inline SVG). In the SVG: one `polyline.line.s{n}` per series, end dots `circle.dot.f{n}` with direct end labels (`lbl-name` + `lbl` value), then one `g.mark.m{i}` per time step, each with a full-height `rect.hit` and a `g.hover` holding the crosshair and the dots. All tooltips `g.tipl.t{i}` go at the end of the SVG.

- At most 3 series: `chart-1`, `chart-2`, `chart-3`, in that order. They stay distinct for colour-blind readers in any combination. For more, show the top 2 and sum the rest into "Other" (`chart-muted`), or split into small charts.
- Lines 2px, round joins; dots 8px with a 2px `surface` ring; no area fill unless there's one series (then the series colour at 10%).
- Label line ends with the series name and latest value; if labels would collide, use a short leader line, never stack them loosely.
- Hover or focus a day: a 1px crosshair, a dot on each line, and a tooltip with the date, total and each series.
- The y-axis starts at 0 for amounts and quantities; never two y-axes.
- Keep the right gutter wide enough for the end labels in both languages.
