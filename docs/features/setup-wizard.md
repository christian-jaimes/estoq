# Setup wizard

Visual reference: `0 - Elegant Dark Store Setup Wizard.png`. Runs once, on first launch, before [[sign-in]] exists. A fuller onboarding (categories, first products) comes after the MVP.

## Layout

Left rail: gold logo and wordmark (the gold lockup in both themes), "Setup Wizard", step list with number, title and subtitle; the current step has a gold marker. Right: "Step n of N" progress bar, step title and subtitle, form, Back and Next (gold) buttons.

## Steps

| # | Step | Content |
|---|---|---|
| 1 | Welcome | Short welcome to estoq, language choice (English, Spanish) for the rest of the wizard, note that data stays on this computer, Start |
| 2 | Owner information | Full name, email (required), password and confirmation (at least 8 characters, [[open-questions]] Q21), generated six-digit user code (shown, copyable) |
| 3 | Store information | Logo (PNG or JPG, max 2 MB), store name (required), registration number / tax ID, address, city, postal code, country, phone and email (optional), store language (English or Spanish; starts as the Welcome choice, adjustable here) |
| 4 | Preferences | Base and default currency (one dropdown of every currency; USD unless changed), also-accepted currencies (search by code or name, picked currencies appear as removable chips; none by default; the base is always accepted), sales tax rate (default 0 %, 0 to 100, up to two decimals, comma or dot) with a toggle: tax not included (default) or included in prices |
| 5 | Recovery key | Shown once the account and store are set up. Explains that it is the only way to recover owner access and encrypted data, and must be kept in a secure place (printed or written down, not on this computer). Print and copy buttons; a confirmation tick ("I have stored my recovery key safely") enables Finish |

Finish signs the owner in and opens the [[dashboard]].

## Rules

- No install or data location choice: Windows installs the MSIX package to its protected location, and data always lives in `%ProgramData%\estoq`, shared by every Windows account on the PC ([[decisions]] 4).
- Required fields block Next; errors appear after the first Next and then follow edits. Back keeps entered values. Optional fields (store address, tax ID, phone, email, logo) never block.
- Field rules live in `estoq.Core` (`SetupRules`, `TaxRate`, `UserCode`, `Currencies`) and return codes, not text, so messages are translated in the app.
- Currency names come from Windows region data and are shown in English in both languages; codes are what staff search by. Changing the base removes it from the also-accepted list. Rates to the base are set later in [[settings]].
- Only the owner has a user code in the wizard. Roles and permissions: [[roles-and-permissions]].
- The recovery key cannot be shown again. Losing it and the Windows account makes data unrecoverable; the screen says so plainly.
- The wizard language becomes the owner's app language and the default for new users.
- Everything set here can be changed later in [[settings]].

## Front end notes

Full-window page, not a pop-up (`Views/Setup/SetupWizardPage`; one user control per step, one view model per step, `SetupWizardViewModel` drives them). Launch shows it while `ISetupService.IsSetupComplete` is false.

- `SampleSetupService` only validates with the Core rules and stores nothing, so the wizard shows on every launch until the back end replaces it. Its recovery key format is a placeholder ([[encryption]]).
- Language: the choice on Welcome applies at once. Static labels are read when a page is built, so the page is rebuilt on a language change; entered values live in the view models and survive. Strings: [[ui-implementation]] (Localisation).
- Copy uses the clipboard without history or sync for the recovery key. Print opens the Windows print dialog with the key on one page; if it cannot start, the screen says so and asks the owner to write the key down.
- The logo is validated (PNG or JPG, 2 MB) and previewed; it is not copied anywhere until the back end exists.
