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

## Pop-up pattern

F1 to F4 and [[settings]] share one frame so staff learn one pattern.

- `Controls/PopupShell`: radius-xl surface; header (icon badge, title, subtitle, close), scrolling body, footer with secondary and one gold primary action. Properties `Title`, `Subtitle`, `Glyph`, `PrimaryText`, `IsPrimaryEnabled`, `Body`; events `CloseRequested`, `PrimaryInvoked`. No flow logic.
- `Services/IPopupService` (singleton) owns which pop-up is open (`PopupKind`: PointOfSale, AddItems, StockAdjustments, Returns, Settings). One at a time.
- `ViewModels/PopupViewModel` maps the open kind to header text and icon.
- `Views/ShellPage` hosts the overlay: dimmed `EqScrimBrush` backdrop, shell inset 40 px sides and bottom, 56 px top. Opening moves focus inside so Esc works. Esc, close, and a backdrop click close it.
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
