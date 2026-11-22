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

## Receiving stock

Same pop-up. The duplicate check opens the existing product; added quantities are recorded as stock received movements. New products record opening balance movements.

## Layout guidance

From the visual reference: sections General (type, category, brand, model label, generated name, description, supplier barcode), Pricing (cost, selling, margin), Size and colour (chips, swatches, "More colours"), Product image (main image and thumbnails), Summary (category, brand, price, margin, variant count such as "6 (3 sizes × 2 colours)"), Variants + stock table (swatch and name, SKU, barcode, stock stepper, delete). Footer: Cancel, Save product.

## Rules

- Cost price and margin are hidden from cashiers; editing an existing product's details and prices is manager and owner only ([[roles-and-permissions]]).
- Products are active or archived; archiving happens in [[inventory]].
