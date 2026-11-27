using Estoq.App.Services;
using Estoq.App.ViewModels;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.UI.ViewManagement;

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
        ApplySidebarWidth(ViewModel.SidebarWidth);
        PopupFrame.RenderTransform = new CompositeTransform();
        PosView.RenderTransform = new CompositeTransform();
        if (PopupState.IsOpen)
        {
            PopupOverlay.Visibility = Visibility.Visible;
            PopupOverlay.Opacity = 1;
        }

        navigation.Initialize(ContentFrame);
        Loaded += (_, _) => navigation.NavigateTo(AppRoute.Dashboard);
        PopupState.PropertyChanged += OnPopupPropertyChanged;
        _localizer = App.Current.Services.GetRequiredService<ILocalizer>();
        Loaded += (_, _) => _localizer.LanguageChanged += OnLanguageChanged;
        Unloaded += (_, _) => _localizer.LanguageChanged -= OnLanguageChanged;
    }

    private readonly ILocalizer _localizer;
    private Storyboard? _sidebarStoryboard;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _sidebarFlipTimer;

    private void ApplySidebarWidth(double panelWidth)
    {
        SidebarPanel.Width = panelWidth;
        SidebarContainer.Width = panelWidth + ShellViewModel.SidebarButtonOverhang;
    }

    /// <summary>
    /// Slides the sidebar between rail and full width. The labels, logo and alignment change with the state, so the
    /// content fades out first, the state flips while it is invisible, and it fades back in as the panel settles:
    /// nothing visibly pops. The content is clipped to the panel meanwhile. With system animations off the width just changes.
    /// </summary>
    private void OnSidebarToggleClick(object sender, RoutedEventArgs e)
    {
        if (_sidebarStoryboard is not null)
        {
            return;
        }

        // Set the target state explicitly (never "toggle") so a late or repeated callback can't leave the width and labels out of step.
        var expand = !ViewModel.IsSidebarExpanded;
        var to = expand ? ShellViewModel.ExpandedWidth : ShellViewModel.CollapsedWidth;
        if (!new UISettings().AnimationsEnabled)
        {
            ApplySidebarWidth(to);
            ViewModel.IsSidebarExpanded = expand;
            return;
        }

        const int totalMilliseconds = 240;
        var from = SidebarPanel.Width;
        var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };
        var storyboard = new Storyboard();
        var total = TimeSpan.FromMilliseconds(totalMilliseconds);
        AddWidthAnimation(storyboard, SidebarPanel, from, to, total, easing);
        AddWidthAnimation(storyboard, SidebarContainer, from + ShellViewModel.SidebarButtonOverhang, to + ShellViewModel.SidebarButtonOverhang, total, easing);

        // Content opacity: gone by 80 ms, hidden until 140 ms, fully back at the end. The state flips at 110 ms.
        var fade = new DoubleAnimationUsingKeyFrames();
        fade.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 1 });
        fade.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(80)), Value = 0 });
        fade.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(140)), Value = 0 });
        fade.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(total), Value = 1 });
        Storyboard.SetTarget(fade, SidebarContent);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        // Kept in a field: a timer that only a local variable references can be collected before it ticks.
        _sidebarFlipTimer = DispatcherQueue.CreateTimer();
        _sidebarFlipTimer.Interval = TimeSpan.FromMilliseconds(110);
        _sidebarFlipTimer.IsRepeating = false;
        _sidebarFlipTimer.Tick += (_, _) => ViewModel.IsSidebarExpanded = expand;

        storyboard.Completed += (_, _) =>
        {
            storyboard.Stop();
            _sidebarStoryboard = null;
            _sidebarFlipTimer?.Stop();
            ViewModel.IsSidebarExpanded = expand;
            ApplySidebarWidth(to);
            SidebarContent.Opacity = 1;
        };
        _sidebarStoryboard = storyboard;
        storyboard.Begin();
        _sidebarFlipTimer.Start();
    }

    private static void AddWidthAnimation(Storyboard storyboard, FrameworkElement target, double from, double to, TimeSpan duration, EasingFunctionBase easing)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true, // Width drives layout, so the animation must opt in.
        };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, "Width");
        storyboard.Children.Add(animation);
    }

    private void OnSidebarContentSizeChanged(object sender, SizeChangedEventArgs e) =>
        SidebarContent.Clip = new RectangleGeometry { Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height) };

    /// <summary>Static labels are read when a page is built, so a language change builds the shell again.</summary>
    private void OnLanguageChanged(object? sender, EventArgs e) =>
        DispatcherQueue.TryEnqueue(() => App.Current.MainWindow?.ShowShell(crossFade: false));

    public ShellViewModel ViewModel { get; }

    public PopupViewModel PopupState { get; }

    public NotificationsViewModel Notifications { get; }

    /// <summary>The window uses this area as its title bar so the app can be dragged from it.</summary>
    public UIElement TitleBarDragRegion => DragRegion;

    private void OnLogOutAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.LogOutCommand.Execute(null);
        args.Handled = true;
    }

    private void OnPointOfSaleAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.OpenPointOfSaleCommand.Execute(null);
        args.Handled = true;
    }

    private void OnAddItemsAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.OpenAddItemsCommand.Execute(null);
        args.Handled = true;
    }

    private void OnStockAdjustmentsAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.OpenStockAdjustmentsCommand.Execute(null);
        args.Handled = true;
    }

    private void OnReturnsAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.OpenReturnsCommand.Execute(null);
        args.Handled = true;
    }

    private void OnFocusSearch(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        SearchBox.Focus(FocusState.Keyboard);
        SearchBox.SelectAll();
        args.Handled = true;
    }

    private void OnPopupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PopupViewModel.IsOpen))
        {
            return;
        }

        AnimatePopup(PopupState.IsOpen);
        if (PopupState.IsOpen)
        {
            // Wait for the overlay to become visible, then move focus inside so Esc and Tab act on the pop-up.
            DispatcherQueue.TryEnqueue(() => _ = PopupState.IsPointOfSale ? PosView.FocusStart() : PopupFrame.FocusFirst());
        }
    }

    /// <summary>Tells the theme service where the button is, so the new theme grows out of it. Runs before the command.</summary>
    private void OnThemeToggleClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement button)
        {
            var centre = button.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(button.ActualWidth / 2, button.ActualHeight / 2));
            App.Current.Services.GetRequiredService<IThemeService>().RevealOrigin = centre;
        }
    }

    private Storyboard? _popupStoryboard;

    /// <summary>
    /// Opening fades the scrim in while the pop-up rises 16 px into place; closing fades it out with a smaller drop, then
    /// collapses the overlay so it no longer takes input. With system animations off it just shows or hides.
    /// </summary>
    private void AnimatePopup(bool open)
    {
        _popupStoryboard?.Stop();
        _popupStoryboard = null;
        var frame = (CompositeTransform)PopupFrame.RenderTransform;
        var pos = (CompositeTransform)PosView.RenderTransform;

        void Settle()
        {
            PopupOverlay.Opacity = open ? 1 : 0;
            PopupOverlay.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
            frame.TranslateY = 0;
            pos.TranslateY = 0;
        }

        if (!new UISettings().AnimationsEnabled)
        {
            Settle();
            return;
        }

        if (open)
        {
            PopupOverlay.Visibility = Visibility.Visible;
        }

        var duration = TimeSpan.FromMilliseconds(open ? 200 : 140);
        var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };
        var storyboard = new Storyboard();
        AddAnimation(storyboard, PopupOverlay, "Opacity", PopupOverlay.Opacity, open ? 1 : 0, duration, easing);
        var startY = open ? 16 : frame.TranslateY;
        var endY = open ? 0 : 8;
        AddAnimation(storyboard, frame, "TranslateY", startY, endY, duration, easing);
        AddAnimation(storyboard, pos, "TranslateY", open ? 16 : pos.TranslateY, endY, duration, easing);
        storyboard.Completed += (_, _) =>
        {
            storyboard.Stop();
            _popupStoryboard = null;
            Settle();
        };
        _popupStoryboard = storyboard;
        storyboard.Begin();
    }

    private static void AddAnimation(Storyboard storyboard, DependencyObject target, string property, double from, double to, TimeSpan duration, EasingFunctionBase easing)
    {
        var animation = new DoubleAnimation { From = from, To = to, Duration = duration, EasingFunction = easing };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);
        storyboard.Children.Add(animation);
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

    private void OnPopupPrimaryInvoked(object? sender, EventArgs e) => PopupState.PrimaryCommand.Execute(null);

    private void OnScrimTapped(object sender, TappedRoutedEventArgs e) => PopupState.CloseCommand.Execute(null);

    /// <summary>Clicks inside the pop-up must not reach the scrim, which would close it.</summary>
    private void OnPopupTapped(object sender, TappedRoutedEventArgs e) => e.Handled = true;
}
