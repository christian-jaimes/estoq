Cards group related content on the page; the KPI card shows one dashboard metric.

**Provide:** a container with `eq-card`. For a KPI: `eq-kpi__label` (icon + label), `eq-kpi__value` (the figure) and optionally `eq-kpi__delta` with `--up` or `--down`. Free content: an `eq-card__title` and anything below.

- `surface`, 1px `border`, `radius-lg`, `shadow-card`, padding `space-24`; gap `space-24` between cards.
- KPI values use the `kpi` style with tabular numerals; include the currency and exact figure.
- Deltas state the comparison in words ("12% vs. last Thursday"); the arrow is `success` or `danger` — the direction is in the icon, not only the colour.
- Don't tint whole cards with status colours or gold.
- Spanish: format figures by locale (2.847,50 € vs €2,847.50) and let labels wrap to two lines rather than truncate. Cards size by their grid, not their text.
