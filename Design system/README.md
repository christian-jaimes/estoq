> **Precision in every move.** Inventory. Sales. Control.

estoq is inventory, purchasing, sales and analytics software for independent retailers — a Windows-first desktop app. It should feel like a precision instrument: engineered, dependable, thoughtfully designed. Premium without luxury, confident without noise, minimal: only what is necessary, exactly where it belongs.

> Every product has a place. Every movement has a record. Every decision has clarity.

## The name

- Write it in lowercase, always: `estoq` — in UI copy, headings, documents and the wordmark. Never `ESTOQ`, `EstoQ` or `Estoq`, even at the start of a sentence.
- Primary tagline: **Precision in every move.** (the philosophy). Secondary: **Inventory in control.** (the benefit).
- The Q is the brand's visual signature; the wordmark is typography-led and minimal.

## Content fundamentals

Voice is clear, concise, professional, helpful and precise. The interface stays calm and informative — even for errors and warnings.

- Say what happened, in plain words, with the figure. No exclamation marks, no "Oops", no emoji, no slang.
- Address the user as "you" only when an action is needed; otherwise describe the state.
- Use exact terminology: *stock level*, *minimum stock level*, *variant*, *SKU*, *incoming stock*, *outgoing stock*, *transaction*.
- Sentence case for buttons, titles and labels ("Add product", "Low-stock products").

| Instead of | Write |
|---|---|
| Oops! Something went wrong! | Unable to complete the transaction. |
| Wow, great sales! | Sales are up 12% compared to last month. |
| You have stuff running low! | 5 products are below their minimum stock level. |

## Languages: English and Spanish

estoq launches in English and Spanish. Both are first-class: write each natively, never translate word for word, and design every screen for both.

### Voice in Spanish

The same calm, precise voice. Describe the state; don't address the user unless an action is needed.

| English | Español |
|---|---|
| Unable to complete the transaction. | No se ha podido completar la transacción. |
| Sales are up 12% compared to last month. | Las ventas han subido un 12 % respecto al mes pasado. |
| 5 products are below their minimum stock level. | 5 productos están por debajo de su stock mínimo. |
| Add product | Añadir producto |
| Delete product? | ¿Eliminar producto? |

- Buttons and menu items use the infinitive in Spanish ("Guardar cambios", "Recibir stock"), never the imperative.
- For messages, prefer impersonal phrasing ("No se ha podido…", "Se han guardado los cambios") so the tú/usted choice rarely shows. Where the user must be addressed, use one form consistently across the app.
- Sentence case in both languages. Spanish questions and exclamations open with ¿ ¡.
- Keep the brand name and taglines as they are: `estoq` stays lowercase in both languages.

### Terminology

Use one term per concept, in both languages, everywhere.

| English | Español |
|---|---|
| Inventory | Inventario |
| Stock level | Nivel de stock |
| Minimum stock level | Stock mínimo |
| Low stock / Out of stock / In stock | Stock bajo / Sin stock / En stock |
| Incoming / Outgoing stock | Entradas / Salidas de stock |
| Product · Variant · SKU | Producto · Variante · SKU |
| Sales · Purchasing · Analytics | Ventas · Compras · Análisis |
| Transaction | Transacción |
| Supplier | Proveedor |

### Layout for two languages

- Spanish runs 15–30% longer than English. Never give text a fixed width: buttons, badges, tabs and nav items grow with their label; table columns wrap headers, not values.
- Design and review every screen with the Spanish strings, the longer case. Truncate only long product names (with a tooltip), never labels, buttons or figures.
- Keep strings whole: no sentences built from fragments, because word order differs. Use placeholders ("{count} productos están por debajo de su stock mínimo") and plural rules for counts.
- Icons and layout never carry language: no text inside images. Language is chosen in Settings, labelled in its own language ("English", "Español"), never with flags.

### Numbers, money and dates

Format by the user's locale, never by hand. Always tabular numerals.

| | English (en) | Español (es-ES) |
|---|---|---|
| Number | 48,250.75 | 48.250,75 |
| Currency | €48,250.75 | 48.250,75 € |
| Percent | 12% | 12 % |
| Date | Nov 19, 2026 | 19 nov 2026 |
| Short date | 11/19/2026 | 19/11/2026 |
| Time | 4:30 PM | 16:30 |

Language and number format are separate settings: a user can read English with Spanish number formats. Store amounts and dates raw; format at display.

## Visual foundations

### Colour

The identity is warm gold, charcoal and warm ivory.

- `gold` is the signature, not a background. Use it for primary actions, active navigation, selected elements and the primary chart series — one or two gold moments per screen.
- Put `on-gold` (charcoal) text on gold fills; never white.
- On light grounds, gold text and icons use `gold-ink`; `gold` itself is too pale for text there.
- Structure with `bg` → `surface` → `surface-elevated`, divided by `border`. Tables, forms and dashboards sit on neutral surfaces.
- `success`, `warning`, `danger`, `info` are for real statuses only. Never communicate with colour alone: every status carries a word and, where space allows, an icon.
- Text: `text` for content, `text-secondary` for labels and metadata. In light theme `text-secondary` is 3.7:1 on `bg` — keep it at 13px+ and prefer it on `surface`.

### Themes

Both themes are deliberate, not inversions. **Light** sits on Warm Ivory with white surfaces; **Dark** sits on Charcoal with lifted graphite surfaces. Gold stays the accent in both; only `gold-ink` changes (darker on light).

- A single toggle in the top bar switches themes: a moon in light theme, a sun in dark. First launch follows the Windows theme; after that, remember the user's choice.
- Switching is instant and app-wide: every component reads colour tokens only, so changing the theme changes nothing but token values. Never hard-code a hex in a component; the only fixed colours are white on `danger` and the dialog scrim.
- Logos follow the theme too: the lockup is inked with `text` (charcoal on light, ivory on dark).
- Review every screen in both themes before shipping.

### Typography

- **Poppins** (`brand`) for the wordmark context, onboarding, marketing and major headings: `display`, `page-title`, `section-title`.
- **Manrope** (`ui`) for everything in the working interface: `card-title`, `body`, `secondary`, `caption`, `label`, `kpi`.
- **Segoe UI Variable** is the native fallback when a bundled font cannot load. Bundle Poppins and Manrope with the app; never assume they are installed.
- Set prices, quantities, percentages and every financial figure with tabular numerals (`font-variant-numeric: tabular-nums`) so columns align.
- Dashboard figures use `kpi` (28–32px, 600).

### Spacing, radius, elevation

- 8px-based spacing: `space-4` `space-8` `space-12` `space-16` `space-24` `space-32` `space-48` `space-64`. Card padding `space-24`; gaps between cards `space-24`; between page sections `space-32`.
- Radius: `radius-sm` small controls and badges, `radius-md` inputs and buttons, `radius-lg` cards, `radius-xl` dialogs.
- Cards: `surface`, 1px `border`, `radius-lg`, `shadow-card`. Borders carry the structure; shadows only lift (`shadow-dialog` for dialogs and menus).

### States

- Hover: gold actions go `gold-hover`; neutral rows and nav items go `surface-elevated`.
- Pressed: `gold-pressed`.
- Focus: 2px solid `focus-ring` outline, 2px offset, on every interactive element. It meets 3:1 on all grounds in both themes.
- Disabled: 40% opacity, no pointer, no hover.

### Layout

- Persistent, collapsible left sidebar (`sidebar-width`; 64px rail when collapsed): logo, then Dashboard · Inventory · Sales, then a divider and the user with Settings at the bottom. The active item gets a gold indicator and a filled icon.
- Top bar: search filling the width | four round shortcut buttons — Point of sale, Add/receive product, Stock adjustment, Return | theme toggle and notifications. Profile and language live in Settings.
- Main content on `bg` with clearly titled sections; dense but uncluttered. Align everything to the spacing scale.
- Dialogs for focused actions and confirmations only.

### Data visualization

- Neutral chart backgrounds (`surface`); subtle axes, gridlines and legends in `border` and `text-secondary`.
- Series colours come from `chart-1` (gold, the primary series) → `chart-4`, always in that order and never re-ordered by rank; comparison periods and "Other" use `chart-muted`. These are validated for colour-blind readers in both themes. Never add a fifth hue.
- `success` and `danger` mean good and bad change only (▲/▼ deltas), never a series. Say which is which in words or with ▲/▼ — not hue alone.
- Chart text (values, axes, legends) uses text colours, never the series colour.
- Every chart has a legend for 2+ series and a hover/focus tooltip; each bar, day or segment carries its exact values in an `aria-label` for screen readers.
- Components: BarChart (compare amounts), LineChart (change over time, ≤ 3 series), DonutChart (parts of a whole, 2–5 parts).
- Show exact values on hover or selection. Prefer trends, comparisons and stock movement over decorative charts.
- Core dashboard metrics: total stock value, total products and variants, today's sales, monthly revenue, low-stock products, incoming stock, outgoing stock, best-selling products.

## Iconography

- **Google Material Symbols Rounded**, bundled locally. No other icon library, no decorative icons without a function, no emoji.
- Settings: weight 500, optical size 24, grade 0, fill 0 — fill 1 for selected or active icons.

```css
font-family: "Material Symbols Rounded";
font-variation-settings: 'FILL' 0, 'wght' 500, 'GRAD' 0, 'opsz' 24;
```

- Sizes: `icon-sm` (20px) in tables, inputs and buttons; `icon-md` (24px) in sidebar and top bar.
- Common glyphs: `inventory_2` products, `point_of_sale` sales, `local_shipping` purchasing / incoming, `monitoring` analytics, `warning` low stock, `check_circle` success, `error` errors.

## Logo

- Use the files in **Logos**: the `estoq` wordmark and the Q mark, each in charcoal, gold and ivory.
- Approved pairings: charcoal on `ivory`, gold on `charcoal`, ivory on `charcoal`. Monochrome (charcoal or ivory alone) for single-colour reproduction.
- Use the mark + wordmark lockup in the sidebar header, splash, login and documents; the Q mark alone for the collapsed sidebar, Windows app icon, favicon and tight spaces.
- Never recolour outside these three inks, add effects, outline, stretch or set the name in another face.
- Keep the estoc reference felt through precision, geometry and restraint — no shields, medieval textures or literal historical imagery around the logo.

## Applying the brand

Keep the identity consistent across the Windows app icon, splash screen, login and onboarding, sidebar, reports and exported documents, website, and any future mobile or web app. Exported reports use the light theme, `ivory`/white grounds, the charcoal wordmark and tabular figures.

## Components

| Component | Use it for |
|---|---|
| Button | Text actions; one gold primary per view |
| IconButton | Icon-only actions: round (shortcuts, quick actions) or plain (toolbars) |
| Input | Single-value fields with label, hint and error |
| Selector | One choice from 2–8 icon tiles (payment method) |
| Chip | Pill choices: one (currency) or several (filters) |
| Card | Panels and KPI figures |
| Badge | Stock and transaction status, always with a word |
| Table | Lists of products, movements and transactions |
| ImageGallery | Product photos with thumbnails |
| BarChart · LineChart · DonutChart | Compare amounts · change over time · parts of a whole |
| Sidebar | Primary navigation, user and Settings |
| TopBar | Search, four shortcuts, theme toggle, notifications |
| NotificationPanel | Popover from the top bar bell: stock, delivery, sales and return alerts |
| Dialog | Focused confirmations |

All components read tokens only, so both themes come for free; never hard-code a colour.
