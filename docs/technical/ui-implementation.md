# UI implementation

The visual spec is `Design system/` (brand book, `tokens.json`, `tokens.css`, component specs). This page maps it to the WinUI code. Product behaviour of the shell: [[app-shell]].

## Rules for every screen

- Components read tokens only; no hard-coded colours. The scrim and white-on-danger are the only fixed colours.
- Gold for one or two moments per screen; one gold primary button per view.
- Status is never colour alone: badges carry text.
- Sentence case for titles, buttons and labels. `estoq` always lowercase.
- No literal strings in XAML or view models once localisation is wired (English and Spanish).

## Implementation map

| Design system | Code (`src/estoq.App`) |
|---|---|
| Colour tokens, light and dark | `Themes/Tokens.xaml`, brushes `Eq<Token>Brush`; gold replaces `SystemAccentColor` |
| Radius, spacing, sizes | `EqRadius*`, `EqSpace*`, `EqSidebarWidth` in `Tokens.xaml` |
| Typography | `EqBrandFont` (Poppins SemiBold), `EqUiFont` / `EqUiFontSemiBold` (Manrope); styles `EqPageTitleStyle`, `EqCardTitleStyle`, `EqLabelStyle`, `EqSecondaryStyle`, `EqCaptionStyle`, `EqKpiStyle` |
| Components | `Controls/EqField` (labelled input with required or optional mark, suffix, error text, gold focus and red error border), `EqAutoSuggestBoxStyle` / `EqAutoSuggestTextBoxStyle` (themed autocomplete field and suggestion popup), `EqCardStyle`, `EqBadgeStyle`, `EqInputContainerStyle`, `EqPrimaryButtonStyle`, `EqSecondaryButtonStyle`, `EqRoundIconButtonStyle`, `EqPlainIconButtonStyle`, `EqLinkButtonStyle`, `EqNavItemStyle`, `EqSidebarCollapseButtonStyle`, `EqIconStyle` in `Themes/Styles.xaml` |
| Icons | Material Symbols Rounded subset in `Assets/Fonts`, addressed by code point (`Assets/Fonts/codepoints.txt`) |
| Logos | `Assets/Logos/estoq-{wordmark,q-mark,lockup}-{charcoal,gold,ivory}.png`, the gold version is used in every theme (sidebar, splash, sign-in, wizard) |

## Dropdown and autocomplete fields

Use the `EqAutoSuggestBoxStyle` in `Themes/Styles.xaml` as the default app dropdown pattern. Do not introduce a native editable `ComboBox` or a separate visual treatment for form dropdowns without a product-specific reason. The product type, category and brand fields in `Views/AddProduct/AddProductView.xaml` are the reference implementation.

- Keep the input as one rounded `EqInputContainerStyle` surface, using theme brushes for its fill and border. The AutoSuggestBox itself is transparent and borderless so its pointer-over and focused fill spans the full field. Apply the horizontal inset to `EqAutoSuggestTextBoxStyle`, not to the outer wrapper; keep the same inset and vertical text padding as `Controls/EqField`.
- Use the shared AutoSuggestBox template in `Themes/Styles.xaml`. It keeps the popup attached to the field, uses the same theme surface brush, overlaps the field slightly, has square top corners and rounded lower corners, and removes the popup's top border so the join has no doubled edge. Keep side and bottom borders.
- Keep suggestions left-aligned, limit the popup to about five rows before scrolling, and filter options as the user types. Update the suggestion collection in place to avoid closing/reopening the popup on each keystroke. If there are fewer results, let the popup shrink to the result count.
- Close an open suggestion list when the user clicks elsewhere in the containing pop-up, even when its query is empty. A field that allows creation can submit unmatched text; a selection-only field should validate that the chosen value exists before accepting it.
- Use the app's theme tokens for all surfaces and borders so light and dark modes stay consistent. Do not hard-code colors.

The popup corner treatment is set directly in the shared template because the WinUI `AutoSuggestBox` popup is a separate popup surface; a local `OverlayCornerRadius` resource did not affect it. Preserve the template part names `TextBox`, `SuggestionsPopup`, `SuggestionsContainer` and `SuggestionsList` when changing the template so WinUI's control logic can continue to connect the input and suggestions.

## Menus and dropdown lists

Every open list, whatever the control (`MenuFlyout`, `ComboBox`, the AutoSuggestBox popup, a custom flyout), uses the same colours so dropdowns look identical in light and dark themes. Never leave the stock grey WinUI list.

- Background `EqSurfaceElevatedBrush`, border `EqBorderBrush` (1 px), item hover `EqHoverFillBrush`, selected item `EqGoldTintBrush`. Text uses the normal theme text brushes.
- The app-wide dictionary in `Themes/Styles.xaml` overrides the WinUI keys (`MenuFlyoutPresenter*`, `MenuFlyoutItem*`, `ToggleMenuFlyoutItem*`, `ComboBoxDropDown*`, `ComboBoxItem*`), so a plain `ComboBox` or `MenuFlyout` is already correct. Do not add per-page overrides.
- A `MenuFlyout` also gets `EqMenuFlyoutPresenterStyle` (rounded `EqRadiusLg`, 1 px border, 4 px padding). Set `MenuFlyoutPresenterStyle` to it, in XAML or in code: `menu.MenuFlyoutPresenterStyle = (Style)Application.Current.Resources["EqMenuFlyoutPresenterStyle"]`. The Inventory sort, category, product type and brand menus are the reference.
- Tick the current choice with a `ToggleMenuFlyoutItem`. Open and close with the 100 ms motion budget; do not add custom animations.
- Multi-select filters follow the "Checkbox dropdown with search" component below.
- Check every new dropdown in both themes before calling it done.

## Checkbox dropdown with search (multi-select filter)

Use this whenever a filter lets the user pick several values from a list (Inventory: product type, category, brand). Reference implementation: `Controls/MultiSelectList` and the three filter buttons in `Views/InventoryPage.xaml` with `ShowMultiSelect` in its code-behind. Do not build a second variant; extend the control if something is missing.

**Parts**

| Part | What it is |
|---|---|
| Trigger button | `InvFilterButtonStyle` (secondary button look) holding the label and a chevron-down glyph (`E313`). Fixed width (176 px in the toolbar) so a long label trims with an ellipsis instead of resizing the bar. |
| Flyout | A `Flyout` using `EqDropdownFlyoutPresenterStyle` (elevated surface, 1 px border, `EqRadiusLg` corners, 8 px padding). `Placement = BottomEdgeAlignedLeft`, so it opens directly below the button, left edges aligned. |
| Content | `Controls/MultiSelectList(values, selected, changed)`: a search field on top, then the checkbox list. |
| Search field | 36 px tall, `EqBgBrush` fill, 1 px `EqBorderBrush`, `EqRadiusMd`, search glyph (`EF7A`) at the start, borderless transparent `TextBox` inside. Placeholder "Search..." (`Inventory_FilterSearch`; reuse the key or add a page-neutral one). Filters the list as the user types, case-insensitive, contains-match. |
| List | One `CheckBox` per value, 34 px high, label centred vertically with the box (`VerticalContentAlignment = Center`, top padding 0, because the stock template pads the label 5 px from the top). Sorted alphabetically with the current culture. |
| Scrolling | The list sits in a `ScrollViewer` with `MaxHeight = 264` (about 7 rows) and an auto vertical scrollbar. The flyout never grows with the number of values. |
| Empty state | "No matches" (`Inventory_FilterNoMatches`) when the search hides every value or there are no values. |

**Behaviour**

- Width: the content is as wide as the trigger button, never narrower than 220 px (minus the flyout padding).
- Ticking or unticking applies at once and keeps the flyout open; the owner updates its view (the control calls the `changed` callback after each tick). There is no Apply button.
- The control edits the `ISet<string>` it is given in place. Keep the selection in the view model as a case-insensitive `HashSet<string>`; an empty set means "no restriction".
- Several values in one filter combine as "any of"; different filters combine as "and".
- Trigger label: nothing ticked shows the "All ..." text (`Inventory_AllTypes` style), exactly one value shows that value, several show "N selected" (`Inventory_FilterSelected`). A "Clear all" action next to the filters resets every set.
- Cascading lists (used on Inventory): each dropdown offers only values that still exist among the items the other filters, any stock filter and the search leave, plus values already ticked so they can always be unticked. Build the list when the dropdown opens (`ViewModel.TypeValues` and siblings call `Filter(...)` without their own filter).
- Light dismiss closes the flyout; Esc closes it too (standard flyout behaviour).

**Colours and motion**: only theme tokens (see "Menus and dropdown lists"): `EqSurfaceElevatedBrush`, `EqBorderBrush`, `EqBgBrush` for the search field, `EqHoverFillBrush` on hover, gold for checked boxes. No custom animation; the flyout uses the platform open and close, inside the 100 ms motion budget.

**Localisation**: every string comes from the resource files in English and Spanish (`Inventory_FilterSearch`, `Inventory_FilterNoMatches`, `Inventory_FilterSelected`, the "All ..." labels). Do not put literal text in the control. Check that long Spanish values trim cleanly in the 176 px trigger.

**Adding a new multi-select filter**

1. In the view model add a `HashSet<string>` (case-insensitive), an `ISet<string>` property for it, a values property that returns the distinct sorted values (cascading, if the screen has other filters), a label property built with the "All / one / N selected" rule, and make `Filter` and the "has active filters" check use the set.
2. In the page add the trigger button (same style, fixed width, chevron) with a `Click` handler that calls `ShowMultiSelect(anchor, values, selectedSet)`; on selection change call the view model so it re-applies the filters.
3. Add the English and Spanish strings, include the new set in "Clear all", and document the filter on the feature page.

## Tables

Every table uses the shared styles in `Themes/Styles.xaml`, so tables look and feel the same; only the cell content differs (colour chips, stock steppers, article pictures, delete buttons).

- Header: `EqTableHeaderStyle` on a `Grid` (44 px, rounded `EqRadiusMd`, elevated surface, 12 px start and 8 px end padding) with `EqTableHeaderTextStyle` titles (semibold 13 px). Numeric columns right-align, short count columns may centre.
- Rows: a `Grid` with `EqTableRowStyle` (1 px divider) holding an `EqTableBandStyle` border first, visible on every second row (`IsAlternateRow` on the row view model), then the cell grid with the same column widths and 12 px start and 8 px end padding as the header.
- Cell text: `EqTableCellStyle`, and `EqTableCellStrongStyle` for right-aligned values. No outer border around the table.
- Used by: POS cart and payments, Add product variants, Product added.

### Full-bleed table in a card (standard for list pages)

A page table that sits in a card (Inventory is the reference, `Views/InventoryPage.xaml`, "Table card") runs edge to edge inside it. Use this layout for every list or table page from now on.

- Card: `EqCardStyle` with `Padding="0"`. The table is never inset from the card edge.
- Layout: one `Grid` with `RowDefinitions="Auto,*,Auto"` and `RowSpacing="0"`: header, scrolling rows, footer.
- Header: `EqTableHeaderStyle` with `Padding="20,0,20,0"` and `CornerRadius="12,12,0,0"` (top corners match the card, bottom square), so the band spans the full card width.
- Rows: `EqTableRowStyle` / `EqTableBandStyle` as above, but the cell grid uses `Padding="20,8,20,8"`, so text keeps a 20 px inset at both ends while banding, hover and selection tints reach both card edges. Use the same column widths as the header.
- Row hover: a `HoverFill` border (`EqHoverFillBrush`, `Opacity` 0, `OpacityTransition` 100 ms) placed after the band and before the selection tint; pointer enter and exit set its opacity to 1 and 0. The selected row uses `EqGoldTintBrush` on top.
- Footer: a `Grid` with `Padding="20,12"` and a 1 px top border (`EqBorderBrush`), holding the row count on the left and page-level actions (Export) on the right. No pagination: rows load in batches when the user scrolls near the end, and the footer says "Showing X of Y".
- Scrolling: only the rows scroll (`ScrollViewer` between header and footer); the header and footer stay put.
- Tables inside narrow pop-ups or side panels (the Inventory detail panel) keep the inset layout above, not full bleed.

## Form field sizing

Form fields must stay within their assigned layout width; the length of entered or selected text must never resize the field, sibling columns or containing card. In grid-based forms, constrain star columns and their field wrappers with `MinWidth="0"`, and let the control stretch to that allocated width. Keep single-line input text on one line so long values scroll or clip inside the field instead of expanding the layout. Use an explicit `Width` when the form calls for a fixed-size field; otherwise use a constrained grid column so the width remains stable and responsive to the available form width.

### Label and field spacing

- Keep the vertical gap between a field label and its input at 6 px, matching `Controls/EqField`'s `StackPanel Spacing="6"` and the Add Product General fields.
- Put the label and input in the same vertical `StackPanel` with `Spacing="6"` when building a custom field. For labels with optional or required marks, put the label text and mark in a horizontal stack, then place the input below it in the parent stack.
- Do not use a star-sized grid row between a label and a compact input; it can absorb extra height and make the label-to-input gap inconsistent. When an editing area is intentionally meant to fill remaining space before the next field, use a label row followed by a star-sized input row, keep the label-to-input row spacing at 6 px, and let the input stretch to the available height. Give that input a `MinHeight` rather than a fixed height.
- Keep spacing between separate fields controlled by the parent grid or stack; do not add that spacing to the label-to-input gap.

## Fonts and icons

Manrope (Regular, SemiBold) and Poppins (SemiBold, Medium) bundled as static fonts with their SIL OFL licences. Material Symbols Rounded (Apache 2.0) is subset to the glyphs in use, in outline and filled variants, weight 500, optical size 24, grade 0.

Adding an icon: instantiate the variable font with `fontTools.varLib.instancer` (FILL 0 or 1, GRAD 0, opsz 24, wght 500), subset to the needed code points, rename the family to `Material Symbols Rounded` / `Material Symbols Rounded Filled`, and add the code point to `codepoints.txt`.

## Motion and layout transitions

Use coordinated motion when a state change adds, removes or rearranges content. The sign-in mode transition in `Views/SignIn/SignInPage.xaml` and `.xaml.cs` is the reference pattern.

- Trigger motion only when the semantic state changes. Track the previous mode and compare it with the new mode; view models can raise property changes for every keystroke even when the mode stays the same.
- Use paired outgoing and incoming page motion for important navigation. Finish the outgoing page's fade and slide before navigating, then start the destination with a matching entrance. Suppress the frame's default transition when providing a custom pair, and enable destination entrance only for the navigation path that needs it.
- Put elements that move together in named containers. Give each moving container a `CompositeTransform` and animate `TranslateY`; keep independent regions in separate containers.
- Set each region's starting offset before starting the storyboard. Animate related regions together so the content above, the entering row, and the content below arrive in their new positions on one shared timeline.
- Use a shared duration and `CubicEase` with `EasingMode="EaseInOut"` for coordinated movement. Pair a new or removed row's slide with an opacity fade.
- Keep a disappearing row visible through its fade and slide. Collapse it only when the storyboard completes, then reset temporary transforms. This avoids removing its layout space before neighboring content has moved.
- Account for parent layout behavior. A vertically centered `ScrollViewer` recenters its content when a row is added or removed. Offset the upper group by half the row's total occupied space, including its margin, and move the lower group by the matching amount. For the sign-in password row, 56 units of height plus 16 units of top margin gives a 72-unit row; the centered content offset is 36 units.
- Keep controls interactive when their motion should not block input. Disable hit testing only on a row while it is hidden or leaving, and return focus to a useful control after a mode switch when needed.

Avoid animating a whole form's opacity when its primary action must remain immediately visible and usable. Avoid collapsing a row before the coordinated motion finishes; that causes a final layout jump. Check both directions of every transition and confirm that repeated property notifications do not restart the animation.

## Invalid-input feedback

When a field contains invalid input, give immediate visual feedback before clearing or changing its value:

- Change the field container border to `EqDangerBrush` and run a short horizontal shake using `DoubleAnimationUsingKeyFrames` on a `CompositeTransform.TranslateX`.
- Keep the shake on the field container, not the inner text control, so the border and its contents move together. If the field already has a transform for another motion, reuse that `CompositeTransform` so animations do not overwrite each other.
- Restore the field's normal border after the shake completes: gold while focused, the standard border when unfocused. Stop an earlier shake before starting another one on the same field.
- Trigger the feedback from the validation result before the view model clears invalid input. Keep explanatory text alongside the color and motion; color alone must not carry the error.
- If authentication intentionally returns one generic failure for several credentials, animate all relevant fields together instead of indicating which credential matched or failed.

The sign-in implementation in `Views/SignIn/SignInPage` and `ViewModels/SignInViewModel` demonstrates this pattern.

## Pop-up pattern

F1 to F4 and [[settings]] share one frame so staff learn one pattern.

- `Controls/PopupShell`: radius-xl surface; header (title above subtitle, close; no icon badge, same as the point of sale header), scrolling body, footer with secondary and one gold primary action. Properties `Title`, `Subtitle`, `PrimaryText`, `IsPrimaryEnabled`, `Body`; events `CloseRequested`, `PrimaryInvoked`. No flow logic.
- `Services/IPopupService` (singleton) owns which pop-up is open (`PopupKind`: PointOfSale, AddItems, StockAdjustments, Returns, Settings). One at a time.
- `ViewModels/PopupViewModel` maps the open kind to header text and icon.
- `Views/ShellPage` hosts the overlay: dimmed `EqScrimBrush` backdrop, shell inset 40 px sides and bottom, 56 px top. Opening moves focus inside so Esc works. Esc, close, and a backdrop click close it.
- F2 shows `Views/AddProduct/AddProductView` (state in the singleton `AddProductViewModel`, reset each time the pop-up opens); `PopupViewModel` enables the primary button from its `CanSave` and closes the pop-up when it saves.
- The point of sale (F1) is the exception: its mockup has its own header (wordmark, F1 badge, currency chips) and no footer, so `Views/PointOfSale/PointOfSaleView` is hosted beside the frame, chosen by `PopupViewModel.IsPointOfSale` and `IsFramed`. Esc and the close button behave the same.
- Adding a flow: put a user control in `Body` chosen by `PopupKind`, drive `IsPrimaryEnabled`, handle `PrimaryInvoked`. Never copy the frame.

### Container sizing inside pop-ups

- Treat the popup shell as the available boundary. Its content area is the space between the fixed header and footer; sections must size and resize inside that area when the popup changes size.
- Keep the body and its columns constrained to the popup viewport. Do not use content-driven minimum heights or widths that push sections behind the footer or outside the popup.
- Within a column, use flexible rows for sections that should absorb available space and `Auto` rows for sections that should stay close to their content height. For example, Add Product's General section grows while Pricing remains compact.
- Each section fills only its assigned container. Use `VerticalAlignment="Stretch"` where it should occupy the row and constrained grid columns with `MinWidth="0"` to prevent child controls or long text from forcing overflow.
- Recalculate viewport-bound dimensions when the popup resizes. Put scrolling on the content that can grow (such as a long list), not on fixed headers, footers, or compact sections.
- Preserve the intended sizing of sibling sections when changing one section. A change to a card's content should not make another card overflow or acquire unnecessary blank space.

## Localisation

- Text lives in `Resources/Strings.resx` (English, neutral) and `Strings.es.resx` (Spanish); keys are `Area_Name`. Every key exists in both files.
- XAML: `{x:Bind services:Loc.Get('Key')}`, evaluated once when a page is built. View models: `ILocalizer` (`localizer["Key"]`, `Format`), refreshed on `LanguageChanged`.
- A language change rebuilds the page that offers it (the wizard) instead of rebinding every label; screen state lives in singleton view models so nothing is lost.
- Validation codes from Core map to `Error_<Code>` keys.
- A missing key shows the key itself, so gaps are visible.

## Known gaps

- Tab focus is not trapped inside pop-ups yet.
- Strings of the shell, pop-ups and placeholder pages are still English literals; only the setup wizard is localised.
- Filled icon state for the active nav item not used yet.
