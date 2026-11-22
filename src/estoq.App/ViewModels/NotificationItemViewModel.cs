using Estoq.App.Services;
using Microsoft.UI.Xaml;

namespace Estoq.App.ViewModels;

/// <summary>One row of the notifications panel.</summary>
public sealed class NotificationItemViewModel
{
    public NotificationItemViewModel(AppNotification notification)
    {
        Notification = notification;
    }

    public AppNotification Notification { get; }

    public string NotificationId => Notification.NotificationId;

    public string Title => Notification.Title;

    public string Detail => Notification.Detail;

    public bool IsRead => Notification.IsRead;

    public Visibility UnreadVisibility => IsRead ? Visibility.Collapsed : Visibility.Visible;

    public Visibility ReadVisibility => IsRead ? Visibility.Visible : Visibility.Collapsed;

    public double ContentOpacity => IsRead ? 0.65 : 1;

    /// <summary>Material glyph (warning, error, info). The text carries the meaning; icon is a cue.</summary>
    public string Glyph => Notification.Kind switch
    {
        NotificationKind.LowStock => "",
        NotificationKind.SoldWithoutStock => "",
        _ => "",
    };

    public string TimeText
    {
        get
        {
            var age = DateTimeOffset.Now - Notification.CreatedAt;
            if (Notification.CreatedAt.Date == DateTimeOffset.Now.Date)
            {
                return age.TotalMinutes < 60 ? $"{Math.Max(1, (int)age.TotalMinutes)} min" : $"{(int)age.TotalHours} h";
            }

            return Notification.CreatedAt.Date == DateTimeOffset.Now.Date.AddDays(-1)
                ? "Yesterday"
                : Notification.CreatedAt.ToString("d MMM");
        }
    }
}
