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

    public NotificationsViewModel(INotificationService service)
    {
        _service = service;
        _service.Changed += (_, _) => Refresh();
        Refresh();
    }

    public ObservableCollection<NotificationItemViewModel> Items { get; } = [];

    public int UnreadCount { get; private set; }

    public bool HasItems => Items.Count > 0;

    public bool HasUnread => UnreadCount > 0;

    public bool HasRead => Items.Any(i => i.IsRead);

    public Visibility EmptyVisibility => HasItems ? Visibility.Collapsed : Visibility.Visible;

    public Visibility UnreadDotVisibility => HasUnread ? Visibility.Visible : Visibility.Collapsed;

    public string Summary => HasUnread ? $"{UnreadCount} unread" : "All caught up";

    public string BellLabel => HasUnread ? $"Notifications, {UnreadCount} unread" : "Notifications";

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
            Items.Add(new NotificationItemViewModel(n));
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
