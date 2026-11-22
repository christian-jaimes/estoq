Status badges label the state of a product, order or transaction.

**Provide:** `<span class="eq-badge eq-badge--{success|warning|danger|info}">` with an `eq-badge__dot` and the status word. No modifier = neutral (Draft, Archived).

- The word carries the meaning; the dot carries the colour. Text stays `text` on `surface-elevated`, so it reads in both themes.
- Height 24px, `radius-sm`, `caption` style.
- Stock: In stock (success), Low stock (warning), Out of stock (danger), Incoming (info).
- Don't use badges as buttons or for counts in navigation.
- Spanish: "En stock", "Stock bajo", "Sin stock", "Entrada pendiente". Badges grow with the word; don't truncate.
