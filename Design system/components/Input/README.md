Text inputs collect a single value; always with a visible label.

**Provide:** `<label class="eq-field">` containing `eq-field__label`, an `eq-input` wrapper around the `<input>`, and optionally `eq-field__hint` or `eq-field__error`. Add `eq-input--invalid` on error. A leading icon (`search`, `qr_code_scanner`) goes inside `eq-input`.

- `surface` ground, 1px `border`, `radius-md`, height `control-height`; focus shows the 2px `focus-ring`.
- Numbers use tabular numerals; right-align them in dense forms.
- Error text says what to enter, calmly ("Enter 0 or more."), paired with the `error` icon in `danger` — never red text alone.
- Don't use placeholder text as the label.
- Spanish: labels and errors written natively ("Stock mínimo", "Introduce 0 o más."). Format numbers by locale: 1.120,50 in es-ES, 1,120.50 in en.
