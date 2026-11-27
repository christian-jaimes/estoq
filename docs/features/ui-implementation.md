# UI implementation

Cross-screen interaction and layout rules shared by app features.

## Text inputs

- Enter submits only the focused single-line input's value or field-level action; it must not trigger a larger enclosing transaction. For scanner-ready article inputs, Enter submits the scanned barcode and adds the exact matching article to the current action without requiring another click. Apply this convention to sales, returns, stock movements and other flows that accept article scans. Payment amount inputs submit only the payment currently being entered. Keep final transaction submission on its own button or shortcut.
- When an input rejects typed content, keep the last valid value and caret position, briefly shake the field, and show its border in the danger color. Restore the normal border when the animation ends. Apply this consistently to validated inputs; the POS payment amount field is the reference implementation.
- For fixed-height WinUI `TextBox` and `PasswordBox` controls, verify text is vertically centered in the rendered field. `VerticalContentAlignment="Center"` alone may not be sufficient with zero vertical padding. Use small vertical padding such as `0,6` or `0,9`, separately from horizontal padding, and avoid horizontal-only padding such as `10,0`.

## Dropdowns

- Use the shared `EqAutoSuggestBoxStyle` autocomplete dropdown for app dropdowns by default. It has a single rounded themed input surface, left-aligned padded text, a compact filtered suggestion list with scrolling, and a popup that joins the field with no top edge and rounded lower corners. The implementation and template rules are in [[ui-implementation]] (technical).
- Close the suggestion list when focus/click moves elsewhere in its containing flow. Allow unmatched text only when that field is intended to create a new value; selection-only fields accept existing suggestions only.

## Form field sizing

- Fields stay within their assigned width. Long entered or selected values must not resize sibling columns or their containing card. Constrain grid columns and wrappers with `MinWidth="0"`; use explicit widths where the layout requires fixed-size fields. Keep long single-line text inside the input rather than allowing it to determine the form width.

## Tables and lists

- Use a distinct header row on an elevated surface, with its column labels aligned to the data below.
- Alternate a subtle darker background on every other data row. The band fills the full row width without horizontal margins; keep cell padding on the content layer inside the row.
- Separate rows with a thin border and keep column widths and alignment consistent between the header and rows.

## Keyboard shortcut hints

- When a screen or pop-up opens, show its visible keyboard shortcut badges for four seconds, then hide them automatically. Keep the shortcuts themselves active after their badges disappear.

## Pop-up pattern

- Feature pop-ups use their own layout and do not include the shared application frame.
