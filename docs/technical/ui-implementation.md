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
| Components | `Controls/EqField` (labelled input with required or optional mark, suffix, error text, gold focus and red error border), `EqCardStyle`, `EqBadgeStyle`, `EqInputContainerStyle`, `EqPrimaryButtonStyle`, `EqSecondaryButtonStyle`, `EqRoundIconButtonStyle`, `EqPlainIconButtonStyle`, `EqLinkButtonStyle`, `EqNavItemStyle`, `EqSidebarCollapseButtonStyle`, `EqIconStyle` in `Themes/Styles.xaml` |
| Icons | Material Symbols Rounded subset in `Assets/Fonts`, addressed by code point (`Assets/Fonts/codepoints.txt`) |
| Logos | `Assets/Logos/estoq-{wordmark,q-mark,lockup}-{charcoal,gold,ivory}.png`, swapped by theme with `Controls/ThemedImage` |

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

- `Controls/PopupShell`: radius-xl surface; header (icon badge, title, subtitle, close), scrolling body, footer with secondary and one gold primary action. Properties `Title`, `Subtitle`, `Glyph`, `PrimaryText`, `IsPrimaryEnabled`, `Body`; events `CloseRequested`, `PrimaryInvoked`. No flow logic.
- `Services/IPopupService` (singleton) owns which pop-up is open (`PopupKind`: PointOfSale, AddItems, StockAdjustments, Returns, Settings). One at a time.
- `ViewModels/PopupViewModel` maps the open kind to header text and icon.
- `Views/ShellPage` hosts the overlay: dimmed `EqScrimBrush` backdrop, shell inset 40 px sides and bottom, 56 px top. Opening moves focus inside so Esc works. Esc, close, and a backdrop click close it.
- F2 shows `Views/AddProduct/AddProductView` (state in the singleton `AddProductViewModel`, reset each time the pop-up opens); `PopupViewModel` enables the primary button from its `CanSave` and closes the pop-up when it saves.
- Adding a flow: put a user control in `Body` chosen by `PopupKind`, drive `IsPrimaryEnabled`, handle `PrimaryInvoked`. Never copy the frame.

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
