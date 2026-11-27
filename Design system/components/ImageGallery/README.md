# Image gallery

Use this pattern whenever a screen lets someone browse or manage a set of product photos. Keep the photo order stable; selecting a thumbnail changes the main photo without rearranging the list.

## Layout

- Keep the main image frame at a fixed 4:3 aspect ratio. The frame owns its size and must not grow or shrink to match an image's dimensions.
- Fit the full main photo inside its frame (`Uniform` / `contain`). Never crop the main product photo. Center it over an elevated surface so unused space is intentional and calm.
- Use a vertical thumbnail rail beside the main image. Keep each thumbnail a fixed square, independent of source dimensions, and fill it (`UniformToFill` / `cover`) so the rail stays compact and scannable.
- Round thumbnail corners and clip or paint the image through the rounded frame so the photo, frame, and selection outline share the same radius.
- Match the rail viewport height to the main frame. Allow the rail to scroll when there are more thumbnails than fit; keep the final thumbnail reachable and fully visible.
- Place the add-photo action in a consistent corner of the main frame. When there are no photos, use the same fixed frame for the empty state and offer browse plus drag-and-drop where supported.

## Selection and actions

- Keep the thumbnail order unchanged when selection changes. The selected thumbnail represents the photo shown in the main frame.
- Make the whole thumbnail tile a selection hit area, including its image. Give it an accessible name that identifies the photo when possible.
- Make selection clear with a persistent, high-contrast accent border. Keep this indicator visible independently of hover; do not rely on colour changes in the image alone.
- Keep remove as a separate, small button in the thumbnail corner so clicking the photo selects it and clicking the remove button removes it.
- Reveal the remove button when the pointer enters anywhere over that thumbnail, then hide it when the pointer leaves. Animate opacity briefly, keep the button's danger fill solid red in every state, and keep the button itself clickable.
- Support keyboard selection, such as arrow keys, and keep the selected thumbnail in view when selection changes.

## Accessibility and data

- Give the main photo and each thumbnail meaningful alternative text (product, colour, and view when known).
- Expose which thumbnail is selected to assistive technology and provide accessible names for add and remove actions.
- Keep the photo order and selected item tied to the same source collection so mouse, keyboard, and remove actions stay in sync.
- Use the first photo as the product cover only where the product data model defines that behavior; do not change the order just to emphasize the current selection.

These rules apply to native UI and web implementations. Use the platform's equivalent fit/cover modes, clipping, focus, and pointer-state APIs rather than assuming a specific markup framework.
