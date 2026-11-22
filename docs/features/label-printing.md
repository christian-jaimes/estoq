# Label printing (future)

Starts once the label printer is chosen ([[open-questions]] R1). Hooks already shown in [[add-product]] and [[inventory]].

- After saving products or received stock, ask "Print n labels?" (Print, Skip). A setting switches to automatic printing.
- One label per physical unit; a variant with no quantity asks for a count.
- Content: product name, size, colour, price (default currency) and internal barcode. Text in the store language.
- The supplier barcode is never printed.
- One fixed generic layout so the printer model does not change the design.
- Reprint from the product in [[inventory]] and from the variant row in [[add-product]].
