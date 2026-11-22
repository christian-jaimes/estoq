using Estoq.App.Services;
using Estoq.App.ViewModels;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Estoq.App.Views;

public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<ShellViewModel>();
        PopupState = App.Current.Services.GetRequiredService<PopupViewModel>();
        Notifications = App.Current.Services.GetRequiredService<NotificationsViewModel>();
        var navigation = App.Current.Services.GetRequiredService<INavigationService>();
        InitializeComponent();

        navigation.Initialize(ContentFrame);
        Loaded += (_, _) => navigation.NavigateTo(AppRoute.Dashboard);
        PopupState.PropertyChanged += OnPopupPropertyChanged;
    }

    public ShellViewModel ViewModel { get; }

    public PopupViewModel PopupState { get; }

    public NotificationsViewModel Notifications { get; }

    /// <summary>The window uses this area as its title bar so the app can be dragged from it.</summary>
    public UIElement TitleBarDragRegion => DragRegion;

    private void OnFocusSearch(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        SearchBox.Focus(FocusState.Keyboard);
        SearchBox.SelectAll();
        args.Handled = true;
    }

    private void OnPopupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PopupViewModel.IsOpen) && PopupState.IsOpen)
        {
            // Wait for the overlay to become visible, then move focus inside so Esc and Tab act on the pop-up.
            DispatcherQueue.TryEnqueue(() => PopupFrame.FocusFirst());
        }
    }

    /// <summary>Opening a notification marks it read. Navigation to its source comes with the target screens.</summary>
    private void OnNotificationTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string id })
        {
            Notifications.MarkReadCommand.Execute(id);
        }
    }

    private void OnClearNotificationClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string id })
        {
            Notifications.ClearCommand.Execute(id);
        }
    }

    private void OnAccountPointerEntered(object sender, PointerRoutedEventArgs e) => AccountHoverGear.Opacity = 1;

    private void OnAccountPointerExited(object sender, PointerRoutedEventArgs e) => AccountHoverGear.Opacity = 0;

    private void OnPopupCloseRequested(object? sender, EventArgs e) => PopupState.CloseCommand.Execute(null);

    private void OnScrimTapped(object sender, TappedRoutedEventArgs e) => PopupState.CloseCommand.Execute(null);

    /// <summary>Clicks inside the pop-up must not reach the scrim, which would close it.</summary>
    private void OnPopupTapped(object sender, TappedRoutedEventArgs e) => e.Handled = true;
}
