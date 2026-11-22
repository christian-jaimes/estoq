using Estoq.App.Services;
using Estoq.App.ViewModels.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Views.Setup;

public sealed partial class PreferencesStepView : UserControl
{
    public PreferencesStepView()
    {
        ViewModel = App.Current.Services.GetRequiredService<SetupWizardViewModel>().Preferences;
        InitializeComponent();
    }

    public PreferencesStepViewModel ViewModel { get; }

    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.UpdateSuggestions(sender.Text);
        }
    }

    private void OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is CurrencyItem item)
        {
            ViewModel.AddCurrency(item);
        }

        ClearSearch(sender);
    }

    private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        // A picked suggestion is handled in OnSuggestionChosen; Enter on typed text adds the best match.
        if (args.ChosenSuggestion is null)
        {
            ViewModel.AddBestMatch(args.QueryText);
            ClearSearch(sender);
        }
    }

    private void OnRemoveCurrency(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string code })
        {
            ViewModel.RemoveCurrency(code);
        }
    }

    private void ClearSearch(AutoSuggestBox box)
    {
        box.Text = string.Empty;
        ViewModel.UpdateSuggestions(string.Empty);
    }
}
