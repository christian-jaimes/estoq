# Setup wizard

Visual reference: `0 - Elegant Dark Store Setup Wizard.png`. Runs once, on first launch, before [[sign-in]] exists. A fuller onboarding (categories, first products) comes after the MVP.

## Layout

Left rail: wordmark, "Setup Wizard", step list with number, title and subtitle; the current step has a gold marker. Right: "Step n of N" progress bar, step title and subtitle, form, Back and Next (gold) buttons.

## Steps

| # | Step | Content |
|---|---|---|
| 1 | Welcome | Short welcome to estoq, language choice (English, Spanish) for the rest of the wizard, note that data stays on this computer, Start |
| 2 | Owner information | Full name, email (required), password and confirmation, generated six-digit user code (shown, copyable) |
| 3 | Store information | Logo (PNG or JPG, max 2 MB), store name (required), registration number / tax ID, address, city, postal code, country, phone and email (optional) |
| 4 | Preferences | Base and default currency (USD), accepted currencies (EUR, COP, VES), tax rate (default 0 %), store language |
| 5 | Recovery key | Shown once the account and store are set up. Explains that it is the only way to recover owner access and encrypted data, and must be kept in a secure place (printed or written down, not on this computer). Print and copy buttons; a confirmation tick ("I have stored my recovery key safely") enables Finish |

Finish signs the owner in and opens the [[dashboard]].

## Rules

- No install or data location choice: Windows installs the MSIX package to its protected location, and data always lives in `%ProgramData%\estoq`, shared by every Windows account on the PC ([[decisions]] 4).
- Required fields block Next; Back keeps entered values.
- The recovery key cannot be shown again. Losing it and the Windows account makes data unrecoverable; the screen says so plainly.
- The wizard language becomes the owner's app language and the default for new users.
- Everything set here can be changed later in [[settings]].

## Front end notes

Full-window page, not a pop-up. Build with sample data and an `ISetupService` that only validates.
