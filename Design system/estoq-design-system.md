# estoq — Design System

*Version 1.1 · generated from the estoq design system artifact · November 2026*

## Contents

1. Brand book
2. Tokens (colour, typography, spacing, radius, shadow, size)
3. Logos
4. Components

---

# 1. Brand book

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
- Top bar: search filling the width, the New sale and Add product shortcuts (round icon buttons), the theme toggle and notifications. Profile, language and preferences live in Settings.
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

## Component index

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
| TopBar | Search, shortcuts, theme toggle, notifications |
| Dialog | Focused confirmations |

All components read tokens only, so both themes come for free; never hard-code a colour.

---

# 2. Tokens

## Colour

| Token | Light | Dark | Usage |
|---|---|---|---|
| `gold` | `#D6B77A` | `#D6B77A` | Champagne Gold, the signature accent. Primary button fill, active nav indicator, selected rows, primary chart series, logo. Never a large background and never small text on light grounds (1.7:1 on ivory) — use `gold-ink` there. |
| `gold-hover` | `#E2C995` | `#E2C995` | Hover state of gold actions (primary button, gold toggles). |
| `gold-pressed` | `#B99A5E` | `#B99A5E` | Pressed/active state of gold actions. |
| `gold-ink` | `#9C7B40` | `#D6B77A` | Gold for text, icons and links. Light: darker gold #9C7B40 — 3.4:1 on `bg`, 3.9:1 on `surface`: meets 3:1 for icons, focus rings and text 24px+, NOT 4.5:1 for body-size text (source value kept). Dark: 9.4:1 on `bg`, 8.5:1 on `surface`. |
| `on-gold` | `#17191C` | `#17191C` | Text and icons on a `gold` fill (charcoal on gold, 9.2:1). Never white on gold. |
| `gold-tint` | `#F9F5EC` | `#2F2F2C` | Selected tile/option background (Selector), behind a `gold-ink` border. Assumption: 14% gold over `surface` (light), 10% (dark). |
| `on-gold-tint` | `{text}` | `{gold}` | Label on `gold-tint`: charcoal text in light (14:1), gold in dark (7.0:1). |
| `charcoal` | `#17191C` | `#17191C` | Brand Charcoal. Dark logo ground, splash screen, structure in brand moments. Same in both themes. |
| `ivory` | `#F1EFE9` | `#F1EFE9` | Brand Warm Ivory. Light logo ground and marketing backgrounds. Same in both themes. |
| `bg` | `#F1EFE9` | `#141619` | App background behind sidebar, top bar and content. |
| `surface` | `#FFFFFF` | `#1C2023` | Cards, panels, tables, sidebar, top bar and inputs (containers). |
| `surface-elevated` | `#FAF9F6` | `#282C31` | Dialogs, menus, popovers, table header rows and hover rows. |
| `text` | `#24262A` | `#F1EFE9` | Primary text on `bg`, `surface`, `surface-elevated` (13:1+ light, 12:1+ dark). |
| `text-secondary` | `#777A80` | `#A9AAAD` | Labels, captions, metadata. Dark: 6:1+ on every ground. Light: 3.7:1 on `bg`, 4.3:1 on `surface` — below 4.5:1 for small text (source value kept); prefer it on `surface` and at 13px+. |
| `border` | `#DFDDD6` | `#33373D` | Card outlines, table dividers, input borders. Light from source; dark value is an assumption (not in the guidelines). |
| `focus-ring` | `{gold-ink}` | `{gold-ink}` | 2px keyboard focus outline on every control, offset 2px. 3:1+ on all grounds in both themes. |
| `success` | `#43876A` | `#43876A` | Positive status: in stock, completed, positive trend. Dot, icon, chart line or badge marker — always with a word or icon, never colour alone. As text it misses 4.5:1 in both themes. |
| `warning` | `#C18B42` | `#C18B42` | Warnings and low-stock states. Marker colour only; pair with the word ("Low stock") and the `warning` icon. |
| `danger` | `#C45D58` | `#C45D58` | Errors, out-of-stock, destructive actions (danger button fill with white text, 4.2:1 — bold 14px label), negative trend. |
| `info` | `#5C83A8` | `#5C83A8` | Informational states: incoming stock, pending, notices. Marker colour only. |
| `chart-1` | `#C4912A` | `#BC8A26` | Chart series 1 — gold, the primary series (this period, the series the story is about). Deeper than `gold` so marks hold on white; 2.8:1 on light `surface`, so light-mode charts carry direct labels or a table view. |
| `chart-2` | `#2563D9` | `#3B82F6` | Chart series 2 — blue. Assign series in fixed order 1→4; never reorder by rank. |
| `chart-3` | `#D6338A` | `#E0509E` | Chart series 3 — magenta. Slots 1–3 are distinguishable in any combination (validated all-pairs, colour-blind safe), so line charts use at most these three. |
| `chart-4` | `#3838B8` | `#8B7BFF` | Chart series 4 — indigo. Only for bars and donut segments, where it never touches `chart-2` (validated adjacent pairs incl. the donut's wrap-around). |
| `chart-muted` | `#C9C7C0` | `#4A4F57` | De-emphasised data: the comparison period (last year) and the "Other" bucket. Series 5+ fold into it, never a new hue. |

## Typography

Families:

- `brand`: "Poppins", "Segoe UI Variable Display", "Segoe UI", system-ui, sans-serif
- `ui`: "Manrope", "Segoe UI Variable Text", "Segoe UI", system-ui, sans-serif
- `icon`: "Material Symbols Rounded"

| Style | Family | Size / line | Weight | Usage |
|---|---|---|---|---|
| `display` | brand | 32px / 40px | 600 | Onboarding, splash, marketing headlines. |
| `page-title` | brand | 26px / 34px | 600 | One per screen, top of the content area. |
| `section-title` | brand | 20px / 28px | 600 | Major sections within a page. |
| `card-title` | ui | 16px / 24px | 600 | Card and dialog titles. |
| `body` | ui | 14px / 20px | 400 | Default UI text, table cells, form values. |
| `secondary` | ui | 13px / 18px | 400 | Helper text, metadata, secondary table columns. |
| `caption` | ui | 12px / 16px | 500 | Labels, table headers, badges. |
| `label` | ui | 14px / 20px | 600 | Buttons and nav items. |
| `kpi` | ui | 32px / 38px | 600 | Dashboard figures (28–32px). Always tabular numerals. |

## Spacing

| Token | Value | Usage |
|---|---|---|
| `space-4` | 4px | Icon-to-label gap, badge padding. |
| `space-8` | 8px | Gap inside controls, between stacked labels and inputs. |
| `space-12` | 12px | Button horizontal padding (small), table cell vertical padding. |
| `space-16` | 16px | Card padding (compact), gap between form fields. |
| `space-24` | 24px | Card padding, gap between cards. |
| `space-32` | 32px | Gap between page sections. |
| `space-48` | 48px | Content area outer margin on wide screens. |
| `space-64` | 64px | Onboarding and splash layouts. |

## Radius

| Token | Value | Usage |
|---|---|---|
| `radius-sm` | 6px | Small controls: badges, checkboxes, chips, nav items. |
| `radius-md` | 8px | Inputs and buttons. |
| `radius-lg` | 12px | Cards and panels. |
| `radius-xl` | 16px | Dialogs. |
| `radius-pill` | 999px | Pills: chips (currency, filters) and fully rounded tags. |

## Shadow

Restrained: borders carry structure, shadows only lift. Values are an assumption — the guidelines ask for restraint but give none.

| Token | Light | Dark | Usage |
|---|---|---|---|
| `shadow-card` | `0 1px 2px rgba(23,25,28,0.06)` | `0 1px 2px rgba(0,0,0,0.30)` | Cards, with a `border` outline. |
| `shadow-dialog` | `0 16px 48px rgba(23,25,28,0.16)` | `0 16px 48px rgba(0,0,0,0.50)` | Dialogs, menus and popovers. |

## Size

| Token | Value | Usage |
|---|---|---|
| `icon-sm` | 20px | Icons in tables, inputs, buttons. |
| `icon-md` | 24px | Sidebar navigation and top bar icons. |
| `sidebar-width` | 240px | Expanded left sidebar. |
| `control-height` | 36px | Buttons and inputs (assumption). |

---

# 3. Logos

The estoq wordmark and Q mark, each as a transparent PNG in one solid ink. The ink is in the file name: `charcoal` (#17191C) for `ivory` or white grounds, `gold` (#D6B77A) and `ivory` (#F1EFE9) for `charcoal` grounds.

- `estoq-wordmark-*.png` — sidebar header, splash, login, reports, website. Minimum width 72px on screen.
- `estoq-q-mark-*.png` — Windows app icon, favicon, avatar-size placements. Minimum 24px.
- Clear space around either mark: at least the height of the wordmark's "o".

Files: `estoq-wordmark-{charcoal,gold,ivory}.png`, `estoq-q-mark-{charcoal,gold,ivory}.png` (transparent PNG, in the design system's Logos group).

---

# 4. Components

## Button

Buttons trigger actions; one primary (gold) button per view marks the main action.

**Provide:** a `<button>` with `eq-btn` plus one variant — `eq-btn--primary`, `eq-btn--secondary`, `eq-btn--ghost` or `eq-btn--danger` — and optionally `eq-btn--sm`. Label in sentence case, verb first ("Add product", "Receive stock"). An optional leading Material Symbols icon goes in `<span class="eq-icon">`.

- Primary: `gold` fill, `on-gold` label, `gold-hover` / `gold-pressed`. Use once per screen or dialog.
- Secondary: `surface` with `border` — for the rest (Export, Filter, Adjust).
- Ghost: cancel and low-emphasis actions in toolbars and dialogs.
- Danger: destructive actions only, usually inside a confirmation dialog. White on `danger` is 4.2:1 — keep the label at 600 weight.
- Height `control-height` (36px), `radius-md`, focus ring 2px `focus-ring`.
- Don't put two gold buttons side by side; don't use gold for navigation links.
- Spanish: infinitive labels ("Añadir producto", "Guardar cambios"). Width follows the label — never fix a button's width; Spanish labels run longer.

## IconButton

Icon buttons run a single action shown by an icon alone: round for quick actions and shortcuts, plain for toolbars and the top bar.

**Provide:** a `<button class="eq-icon-btn">` with one Material Symbols `<span class="eq-icon">` and an `aria-label` (also used as the tooltip). Add `eq-icon-btn--round` for the outlined circle, `eq-icon-btn--sm` for the 32px size, `aria-pressed="true"` for a selected toggle, and `eq-icon-btn__dot` for an unread marker.

- Round: 40px circle (32px small), `surface` fill, 1px `border`, icon in `gold-ink` at `icon-sm`. Hover: `surface-elevated` with a `gold-ink` ring. Selected: `gold` fill, `on-gold` icon, filled glyph.
- Plain: 36px (`control-height`), transparent, `radius-md`, icon in `text-secondary` at `icon-md`; hover `surface-elevated` and `text`.
- Focus: 2px `focus-ring`, 2px offset. Disabled: 40% opacity.
- Common glyphs: `shopping_cart` sales, `deployed_code` products, `local_shipping` incoming stock, `sell` pricing, `filter_list`, `more_vert`, `notifications`.
- Always give an `aria-label` that names the action, translated. Where the meaning isn't obvious, use a Button with a text label instead.
- Don't mix round and plain icon buttons in the same row of related actions.

## Input

Text inputs collect a single value; always with a visible label.

**Provide:** `<label class="eq-field">` containing `eq-field__label`, an `eq-input` wrapper around the `<input>`, and optionally `eq-field__hint` or `eq-field__error`. Add `eq-input--invalid` on error. A leading icon (`search`, `qr_code_scanner`) goes inside `eq-input`.

- `surface` ground, 1px `border`, `radius-md`, height `control-height`; focus shows the 2px `focus-ring`.
- Numbers use tabular numerals; right-align them in dense forms.
- Error text says what to enter, calmly ("Enter 0 or more."), paired with the `error` icon in `danger` — never red text alone.
- Don't use placeholder text as the label.
- Spanish: labels and errors written natively ("Stock mínimo", "Introduce 0 o más."). Format numbers by locale: 1.120,50 in es-ES, 1,120.50 in en.

## Selector

The Selector picks one option from a small set of visual tiles, such as the payment method at checkout or the type of stock movement.

**Provide:** a `<fieldset class="eq-selector">` with an `eq-selector__legend`, and one `<label class="eq-selector__option">` per option containing a hidden `<input type="radio">` (same `name`), an icon (`eq-icon`, or an `<img class="eq-selector__logo">` for a provider's official mark) and the label. Add `eq-selector--compact` for a single-row, icon-left version. Set columns with `grid-template-columns` to suit the count (4 per row for payment methods).

- Tile: `surface`, 1px `border`, `radius-md`, min 72px tall, icon `icon-md` in `text-secondary`, label 13px/500 in `text`. Hover: `surface-elevated`.
- Selected: `gold-tint` fill, 2px `gold-ink` border, filled icon in `gold-ink`, label in `on-gold-tint`. Exactly one option is always selected; preselect the most common (Cash).
- Keyboard: it's a radio group: Tab enters, arrow keys move, focus shows the 2px `focus-ring`. Disabled options: 40% opacity.
- Labels are one or two words and wrap rather than truncate, so Spanish fits ("Pago móvil", "Transferencia").
- Payment providers (Zelle, USDT/Tether, PayPal): use each provider's official mark from its brand kit as `eq-selector__logo`, at the same 24px as the icons; never redraw it. The preview uses neutral Material Symbols as placeholders.
- Use a Selector for 2–8 options that benefit from an icon; for longer lists or text-only choices use a select field.

## Chip

Chips are compact pill choices: one from a short set (currency, period) or several as filters.

**Provide:** a `<fieldset class="eq-chips">` with an `aria-label`, and one `<label class="eq-chip">` per option containing a hidden `<input>` — `type="radio"` (same `name`) for a single choice, `type="checkbox"` for filters — then optional `eq-chip__symbol` (currency symbol) or `eq-icon`, then the label. A standalone toggle chip can be a `<button class="eq-chip" aria-pressed>`.

- 32px tall, `radius-pill`, padding `space-16`, gap `space-8` between chips; label 13px/600 with tabular numerals.
- Default: `surface`, 1px `border`, `text`; symbol in `text-secondary`. Hover: `surface-elevated`.
- Selected: `gold` fill and border, label and symbol in `on-gold` (charcoal); hover `gold-hover`. Focus: 2px `focus-ring`.
- Currency chips: symbol then ISO code ("$ USD", "€ EUR", "Bs. VES", "$ COP"); the code makes same-symbol currencies distinct. One is always selected — the store's default currency first.
- Keep to 2–6 chips in a row; for more options use a select field. Chips grow with their label (Spanish too); never truncate.

## Card

Cards group related content on the page; the KPI card shows one dashboard metric.

**Provide:** a container with `eq-card`. For a KPI: `eq-kpi__label` (icon + label), `eq-kpi__value` (the figure) and optionally `eq-kpi__delta` with `--up` or `--down`. Free content: an `eq-card__title` and anything below.

- `surface`, 1px `border`, `radius-lg`, `shadow-card`, padding `space-24`; gap `space-24` between cards.
- KPI values use the `kpi` style with tabular numerals; include the currency and exact figure.
- Deltas state the comparison in words ("12% vs. last Thursday"); the arrow is `success` or `danger` — the direction is in the icon, not only the colour.
- Don't tint whole cards with status colours or gold.
- Spanish: format figures by locale (2.847,50 € vs €2,847.50) and let labels wrap to two lines rather than truncate. Cards size by their grid, not their text.

## Badge

Status badges label the state of a product, order or transaction.

**Provide:** `<span class="eq-badge eq-badge--{success|warning|danger|info}">` with an `eq-badge__dot` and the status word. No modifier = neutral (Draft, Archived).

- The word carries the meaning; the dot carries the colour. Text stays `text` on `surface-elevated`, so it reads in both themes.
- Height 24px, `radius-sm`, `caption` style.
- Stock: In stock (success), Low stock (warning), Out of stock (danger), Incoming (info).
- Don't use badges as buttons or for counts in navigation.
- Spanish: "En stock", "Stock bajo", "Sin stock", "Entrada pendiente". Badges grow with the word; don't truncate.

## Table

Tables list products, movements and transactions for scanning and comparison.

**Provide:** a `<table class="eq-table">` with `<thead>` / `<tbody>`. Add `num` to numeric cells (right-aligned, tabular), `muted` for secondary columns (SKU), and `aria-selected="true"` on a selected row.

- `surface` body, `surface-elevated` header in `caption` style, 1px `border` dividers, `radius-lg` outline.
- Cell padding `space-12` × `space-16`. Hover rows go `surface-elevated`; a selected row adds a 3px `gold` marker.
- Right-align quantities, prices and percentages; left-align text. Show status with a Badge, not coloured text.
- Don't zebra-stripe and don't centre numbers.
- Spanish: headers may wrap; values never do. Locale-format every number, price and date in the cell, and keep columns right-aligned in both languages.

## ImageGallery

The image gallery shows a product's photos: one large image with a rail of thumbnails and an add tile, on the product detail and edit screens.

**Provide:** `.eq-gallery` containing `.eq-gallery__main` with one `<img>` per photo (same order as the thumbnails, each with real `alt` text: product, colour, view), and a `<fieldset class="eq-gallery__rail">` of `<label class="eq-gallery__thumb">` items — a hidden `<input type="radio">` (same `name`) plus the thumbnail `<img>` — followed by an `eq-gallery__add` button where photos can be edited. With no photos, put `eq-gallery__empty` (icon + one line) in the main area.

- Main: square, `surface-elevated` ground, 1px `border`, `radius-lg`; photos `object-fit: contain` so products are never cropped.
- Thumbnails: 56px, `radius-md`, `space-8` apart, cropped to fill. Selected: 2px `gold` border with a `surface` gap ring. Focus: 2px `focus-ring`.
- Add tile: same size, `surface`, `add` icon in `text-secondary`; opens the file picker and accepts drag-and-drop. Hide it in read-only views.
- The first photo is the product's cover (used in tables, receipts and search). Up to 8 photos.
- Clicking a thumbnail (or arrow keys) swaps the main image; it works with CSS alone.
- Shoot or crop product photos on a plain light ground so they sit consistently in both themes.

## BarChart

Bar charts compare amounts across categories or periods, such as monthly revenue against last year or units sold per product.

**Provide:** a `section.eq-card.eq-chart` containing `eq-chart__head` (an `eq-chart__title`, an `eq-chart__sub` naming the period, and an `eq-legend` when there are 2+ series) and one inline `<svg role="img" aria-label="…">`. In the SVG, each category is a `g.mark.m{i}` (tabindex 0, an `aria-label` with its values) holding a full-height `rect.hit` and its `path.bar` columns. All tooltips `g.tipl.t{i}` go at the END of the SVG so they draw on top.

- Columns ≤ 24px wide, 4px rounded top, square at the baseline, a 2px gap between neighbours; values start at 0.
- Colour: this period `chart-1` (gold); comparison period `chart-muted`; other series `chart-2` → `chart-4` in that order. One series is all `chart-1`, never shaded by size.
- Gridlines 1px `border`; tick labels `tick` (`text-secondary`, tabular), rounded and compact (€10K).
- Label only the value that matters (usually the latest) above its column; everything else is in the tooltip and each column's `aria-label`.
- Hover or focus a category: the others dim and the tooltip shows each value plus the change, as "▲ 12.3%" (arrow + number, not colour alone).
- Never two y-axes. For different units, use two charts.
- Spanish: format figures by locale (27.640 €, 27,6 mil €) and translate month names; never build labels by joining fragments.

## LineChart

Line charts show change over time: daily sales, stock level across weeks, sales by channel.

**Provide:** the same `eq-card eq-chart` shell as BarChart (head with a line-key `eq-legend`, inline SVG). In the SVG: one `polyline.line.s{n}` per series, end dots `circle.dot.f{n}` with direct end labels (`lbl-name` + `lbl` value), then one `g.mark.m{i}` per time step, each with a full-height `rect.hit` and a `g.hover` holding the crosshair and the dots. All tooltips `g.tipl.t{i}` go at the end of the SVG.

- At most 3 series: `chart-1`, `chart-2`, `chart-3`, in that order. They stay distinct for colour-blind readers in any combination. For more, show the top 2 and sum the rest into "Other" (`chart-muted`), or split into small charts.
- Lines 2px, round joins; dots 8px with a 2px `surface` ring; no area fill unless there's one series (then the series colour at 10%).
- Label line ends with the series name and latest value; if labels would collide, use a short leader line, never stack them loosely.
- Hover or focus a day: a 1px crosshair, a dot on each line, and a tooltip with the date, total and each series.
- The y-axis starts at 0 for amounts and quantities; never two y-axes.
- Keep the right gutter wide enough for the end labels in both languages.

## DonutChart

The donut chart shows how one total splits into a few parts, such as this month's sales by category or payment methods used. Use it only for 2–5 parts that add up to a whole.

**Provide:** an `eq-card eq-chart` with the head, then `.eq-donut` holding a 200px `<svg role="img" aria-label="…">` and an `eq-legend` list (key, name, share `pct`, amount `amt`). In the SVG: a `g.center-default` with the total, then one `g.mark` per part holding its `path.seg` and a `g.hover` with its own centre text.

- Ring 28px thick with a 2px `surface` gap between parts; start at 12 o'clock and go clockwise, largest first, "Other" always last.
- Colours in order `chart-1` → `chart-4`; "Other" and anything past 4 parts is `chart-muted`.
- Centre shows the total and the period; hovering or focusing a part swaps it for that part's share and amount, and the other parts dim.
- The legend carries every name, share and amount, so colour is never the only key; text stays in text colours.
- For close values, or comparing across periods, use a BarChart instead. Never show a donut with one part; show the number as a KPI card.

## Sidebar

The persistent, collapsible left sidebar holds the logo, primary navigation, and the signed-in user with a link to Settings.

**Provide:** `<nav class="eq-sidebar">` (add `eq-sidebar--collapsed` for the rail) with, in this order:
1. **Top:** `eq-sidebar__brand` holding `eq-sidebar__lockup` (`eq-logo-mark` + `eq-logo-wordmark`, `role="img" aria-label="estoq"`). The collapse control is a `<button class="eq-sidebar__collapse">` placed directly in the nav: a 24px circle that floats on the sidebar's right border, level with the logo, with `chevron_left` (collapse) or `chevron_right` (expand), `aria-expanded`, and a translated `aria-label`.
2. **Navigation:** `<a class="eq-nav">` items in this order — Dashboard, Inventory, Sales. Each has an `eq-icon`, the label in `eq-nav__label` and a `title` (the tooltip when collapsed). Mark the current page with `aria-current="page"`.
3. **Divider:** `eq-sidebar__divider`, which pushes the user block to the bottom.
4. **Bottom:** `eq-sidebar__user` with the user's photo in `eq-avatar` (an `<img>`, or initials when there's no photo), `eq-sidebar__name` (name in `<strong>`, role below), and a Settings `eq-icon-btn` (`settings` icon).

- Expanded: `sidebar-width`, full lockup and labels. Collapsed: 64px rail with only the Q mark, nav icons, the avatar and the Settings icon; labels and the name hide.
- `surface` ground, right `border`. Items 36px, `radius-sm`, `icon-md` icons in `text-secondary`; hover `surface-elevated`.
- Active item: `surface-elevated`, a 3px `gold` indicator, label at 600, icon filled in `gold-ink`.
- No counts or badges on nav items; numbers live on the pages themselves.
- Long names truncate with an ellipsis; the full name is in the avatar's tooltip.
- Remember the collapsed or expanded choice between sessions. Language, theme defaults and the profile are changed in Settings.
- Spanish: Panel, Inventario, Ventas; Ajustes for Settings; tooltips in the collapsed rail are translated too.

## TopBar

The top bar holds search, the two main shortcuts (New sale, Add product), the theme toggle and notifications.

**Provide:** `<header class="eq-topbar">` containing a search field (`eq-input eq-topbar__search`), an `eq-topbar__actions` group with the two round shortcut icon buttons, the theme toggle (`eq-icon-btn eq-theme-toggle` holding an `eq-theme-toggle__moon` and an `eq-theme-toggle__sun` icon) and an `eq-icon-btn` for notifications (add `eq-icon-btn__dot` when unread).

- The search field fills all the width left after the icon buttons, with a `space-12` gap and `space-16` padding at both ends.
- Shortcuts, in this order: **New sale** (`point_of_sale`) then **Add product** (`add_box`), both round icon buttons (`eq-icon-btn eq-icon-btn--round`: 40px circle, `surface`, `border`, icon in `gold-ink`). No text labels: each has a translated `aria-label` and a tooltip naming the action and its keyboard shortcut ("New sale (Ctrl+N)", "Add product (Ctrl+P)"). Each opens its flow from any page.
- Height 56px, `surface` ground, bottom `border`. It sits right of the sidebar and spans the content area.
- Theme toggle: one icon button. In light theme it shows the moon (`dark_mode`) and switches to dark; in dark theme it shows the sun (`light_mode`) and switches to light. The right icon shows automatically from `data-theme` on the root. Its `aria-label` names the action ("Switch to dark theme"), translated.
- First launch follows the Windows theme; after the user toggles, remember their choice.
- Profile, account and language live in Settings, not in the top bar.
- Icon-only buttons always carry a translated `aria-label`.

## Dialog

Dialogs focus the user on one action or confirmation.

**Provide:** an element with `eq-dialog` and `role="dialog"`, containing `eq-dialog__title`, `eq-dialog__body` and `eq-dialog__actions` (buttons, primary or danger last). Place it over a scrim (`eq-scrim`) and trap focus inside.

- `surface-elevated`, 1px `border`, `radius-xl`, `shadow-dialog`, padding `space-24`, 420px wide.
- Title states the action as a question with the exact object; body states the consequence with figures.
- The confirming button repeats the verb ("Delete product"), never "OK" / "Yes".
- Don't use dialogs for information that can live on the page.
- Spanish: open questions with ¿, quote product names with « » , and repeat the verb on the confirming button ("Eliminar producto").

---

Class names (`eq-…`) refer to the stylesheet `components/bundle.css` in the design system; all values come from the tokens above.
