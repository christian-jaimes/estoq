Dialogs focus the user on one action or confirmation.

**Provide:** an element with `eq-dialog` and `role="dialog"`, containing `eq-dialog__title`, `eq-dialog__body` and `eq-dialog__actions` (buttons, primary or danger last). Place it over a scrim (`eq-scrim`) and trap focus inside.

- `surface-elevated`, 1px `border`, `radius-xl`, `shadow-dialog`, padding `space-24`, 420px wide.
- Title states the action as a question with the exact object; body states the consequence with figures.
- The confirming button repeats the verb ("Delete product"), never "OK" / "Yes".
- Don't use dialogs for information that can live on the page.
- Spanish: open questions with ¿, quote product names with « » , and repeat the verb on the confirming button ("Eliminar producto").
