The image gallery shows a product's photos: one large image with a rail of thumbnails and an add tile, on the product detail and edit screens.

**Provide:** `.eq-gallery` containing `.eq-gallery__main` with one `<img>` per photo (same order as the thumbnails, each with real `alt` text: product, colour, view), and a `<fieldset class="eq-gallery__rail">` of `<label class="eq-gallery__thumb">` items — a hidden `<input type="radio">` (same `name`) plus the thumbnail `<img>` — followed by an `eq-gallery__add` button where photos can be edited. With no photos, put `eq-gallery__empty` (icon + one line) in the main area.

- Main: square, `surface-elevated` ground, 1px `border`, `radius-lg`; photos `object-fit: contain` so products are never cropped.
- Thumbnails: 56px, `radius-md`, `space-8` apart, cropped to fill. Selected: 2px `gold` border with a `surface` gap ring. Focus: 2px `focus-ring`.
- Add tile: same size, `surface`, `add` icon in `text-secondary`; opens the file picker and accepts drag-and-drop. Hide it in read-only views.
- The first photo is the product's cover (used in tables, receipts and search). Up to 8 photos.
- Clicking a thumbnail (or arrow keys) swaps the main image; it works with CSS alone.
- Shoot or crop product photos on a plain light ground so they sit consistently in both themes.
