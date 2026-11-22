using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Microsoft.UI.Xaml;

namespace Estoq.App.ViewModels;

/// <summary>State of the notifications pop-up opened from the top bar bell.</summary>
public sealed partial class NotificationsViewModel : ObservableObject
{
    private readonly INotificationService _service;
    private readonly ILocalizer _localizer;

    public NotificationsViewModel(INotificationService service, ILocalizer localizer)
    {
        _service = service;
        _localizer = localizer;
        _service.Changed += (_, _) => Refresh();
        _localizer.LanguageChanged += (_, _) => Refresh();
        Refresh();
    }

    public ObservableCollection<NotificationItemViewModel> Items { get; } = [];

    public int UnreadCount { get; private set; }

    public bool HasItems => Items.Count > 0;

    public bool HasUnread => UnreadCount > 0;

    public bool HasRead => Items.Any(i => i.IsRead);

    public Visibility EmptyVisibility => HasItems ? Visibility.Collapsed : Visibility.Visible;

    public Visibility UnreadDotVisibility => HasUnread ? Visibility.Visible : Visibility.Collapsed;

    public string Summary => HasUnread ? _localizer.Format("Notif_Unread", UnreadCount) : _localizer["Notif_AllCaughtUp"];

    public string BellLabel => HasUnread ? _localizer.Format("Notif_BellUnread", UnreadCount) : _localizer["Notif_Title"];

    [RelayCommand]
    private void MarkRead(string? notificationId)
    {
        if (notificationId is not null)
        {
            _service.MarkRead(notificationId);
        }
    }

    [RelayCommand]
    private void Clear(string? notificationId)
    {
        if (notificationId is not null)
        {
            _service.Clear(notificationId);
        }
    }

    [RelayCommand]
    private void MarkAllRead() => _service.MarkAllRead();

    [RelayCommand]
    private void ClearRead() => _service.ClearRead();

    private void Refresh()
    {
        Items.Clear();
        foreach (var n in _service.GetAll())
        {
            Items.Add(new NotificationItemViewModel(n, _localizer));
        }

        UnreadCount = Items.Count(i => !i.IsRead);
        OnPropertyChanged(nameof(UnreadCount));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(HasUnread));
        OnPropertyChanged(nameof(HasRead));
        OnPropertyChanged(nameof(EmptyVisibility));
        OnPropertyChanged(nameof(UnreadDotVisibility));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(BellLabel));
    }
}
