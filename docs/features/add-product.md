# Add product (F2)

Visual reference: `2 - Dark Gold-Accented Product Form.png`. Pop-up. Owners never work out a product name, SKU or barcode: they pick master data and estoq builds the rest. Also how stock is received; there is no separate receiving flow in the MVP.

Entry: F2, the "Add Product" button on [[inventory]], or "Edit" on a product.

## Steps

1. Pick master data: product type, category and brand from seeded, editable lists (create inline if missing). The product type limits sizes to its size set, or none for one-size items.
2. Enter the model label: a short design name such as "Red graphic". The one thing the owner types.
3. Duplicate check: if brand, product type and model label already exist, offer to open that product to add variants or stock. A different model label is a new product.
4. Automatic name from brand, product type and model label, for example "Hugo Boss T-shirt, Red graphic". Size and colour are never in the name. The name stays editable.
5. Optional description, supplier barcode (scanned) and images.
6. Select sizes and colours with a searchable picker; only ticked combinations become variants.
7. Automatic identifiers per variant: SKU (overridable, unique) and internal barcode (unique).
8. Prices: cost and selling price in the default currency, set once for the product, overridable per variant. Estimated margin shown.
9. Optional opening stock per variant.
10. Save creates the product and variants in one transaction.
11. Offer to print labels once [[label-printing]] exists.

Worked example: two Hugo Boss T-shirts, both black and size S, one with a blue graphic and one with a red graphic, are two products. Entering "Red graphic" again is flagged as existing.

## Edit mode

Opened from the Edit icon in the Inventory detail panel or by double-clicking a row there; managers and owners only. It is the same pop-up and form, loaded with the product (`AddProductViewModel.PrepareEdit`, then `IsEditing`).

- Editable: product type, category, brand, model label, name, description, supplier barcode, and the cost and selling price, plus a different price for any single variant.
- Locked: the variants. The size and colour picker is hidden, there are no stock steppers and no remove buttons; SKUs and barcodes stay. Stock changes are booked as stock adjustments ([[stock-adjustments]]); adding sizes or colours to an existing product is not part of edit mode yet.
- The duplicate check ignores the product itself and still flags a clash with a different product.
- The pop-up opens with every field filled in and all suggestion lists closed; they open only when the user clicks into a field or types.
- Saving is two steps. "Save changes" opens a "Confirm changes" card over the pop-up with a table of every change: product / variant (the product name for a product field, the variant for a variant price), field, before, an arrow, after. A variant is listed only when its own price changed, not when it just follows the product price.
- Three choices on the card: "Discard changes" (left) throws the changes away and closes the pop-up without saving; "Keep editing" closes only the card and the form stays as it was; "Confirm and save" saves, closes the pop-up and returns to Inventory with nothing open. With no changes the card says so and Confirm is disabled.
- Inventory always starts with nothing selected (no detail panel), and the detail panel closes when the edit pop-up opens, so after saving, discarding or closing the user lands on a clean table.
- Title "Edit product", primary button "Save changes". Saving updates the article rows (`ICatalogService.UpdateProductAsync`), closes the pop-up and the Inventory page refreshes. Not yet audit-logged (back end).
- The Inventory table shows the product name built from brand, product type and model label, so editing the free-text name alone does not change what the table shows.

## Receiving stock

Same pop-up. The duplicate check opens the existing product; added quantities are recorded as stock received movements. New products record opening balance movements.

## Layout guidance

The popup body is constrained between the shell's fixed header and footer and resizes with the popup. Sections stay inside their assigned containers: the left column contains General and Pricing, with General expanding to use available height and Pricing remaining content-height; the right column has Product image and Size and colour side by side. Future sections added below must fit and resize within the same body rather than extending behind the footer. Keep scrolling local to content that grows, such as a long list. See [[ui-implementation]] for container sizing rules.

## Rules

- Cost price and margin are hidden from cashiers; editing an existing product's details and prices is manager and owner only ([[roles-and-permissions]]).
- Products are active or archived; archiving happens in [[inventory]].

## Front end build notes

Built on sample data (`ICatalogService`, `SampleCatalogService`); the rules are in `estoq.Core` (`Products/`, unit-tested) and the pop-up body is `Views/AddProduct/AddProductView` driven by `AddProductViewModel`.

- Product type, category and brand are autocomplete fields populated from `data/dummy_product_types.csv`, `data/dummy_categories.csv` and the active rows of `data/dummy_brands.csv`. Typing filters the suggestion list. When there are no matches, a `+ <typed value>` suggestion lets the owner stage a new value in that field. Staging does not modify master data; only saving the product appends the new value to its CSV. Canceling or closing the product popup discards it. New values receive a unique ID (`TYP-##`, `CAT-##` or `BRD-##`); new brands and product types also receive a unique short SKU code. Other master-data columns remain empty for the future master-data flow. The type's current size choices are derived from `requires_size` and a small local type-to-size mapping; a future master-data flow will own size sets. The "Add size" control adds one more size for this product.
- Variants are every ticked colour with every ticked size. Nothing ticked leaves the variants table empty. Deleting a row removes that combination until its size or colour is ticked again. Rows keep their stock and SKU while the ticks change.
- SKU is built from the product type, colour and size (for example `TS-WHT-S`), made unique with `-2`, `-3`; typing over it keeps the typed value. The internal barcode is an EAN-13 in the store-use range (prefix 2) from a running sequence.
- Prices are entered in the store's base currency. Margin is the selling price minus cost, also as a percentage of the selling price. Cashiers see neither cost nor margin.
- A duplicate (same brand, type and model label) shows a notice and blocks Save. Opening the existing product to add variants or stock waits for [[inventory]] and the stock receiving work.
- Differences from the mockup: no Draft or Active switch (products are active or archived, archived in [[inventory]]); no per-variant print button (labels come later, [[label-printing]]); no row select boxes; the icons for General, Image and delete reuse glyphs already in the icon font.
- Saving appends to the sample data: one row per variant in `data/dummy_article.csv` (new `ART-<n>` identifier, `is_active` True) and one `add` row per variant in `data/dummy_stock_movements.csv` (new `MOV-<n>` identifier, `dd-MM-yy HH:mm` timestamp, sample location `STORE-GHENT-CENTER`). Stock on hand is the sum of an article's movements, so there is no separate inventory table. `model_label` sits after `product_type`; the existing articles and SKUs feed the duplicate check on open. The article table has no internal-barcode column: only the supplier barcode is stored.
- Product added pop-up: after Save the form is covered by a confirmation modelled on the point of sale's "Sale completed". Variants with 0 opening stock are not created (no article or movement row) and Save needs stock on at least one variant. It lists every article created: name with size, colour and SKU, selling price, quantity and total price. The summary shows Articles added (the units received) and Total price (selling price times units, in the store currency). Close closes the pop-up; Add another product starts a fresh form. Save stays disabled while it shows.
- Images are stored as file paths for now; copying them into the data folder belongs to the back end.
