using Estoq.App.Services;
using Microsoft.UI.Xaml;

namespace Estoq.App.ViewModels;

/// <summary>One row of the notifications panel.</summary>
public sealed class NotificationItemViewModel
{
    private readonly ILocalizer _localizer;

    public NotificationItemViewModel(AppNotification notification, ILocalizer localizer)
    {
        Notification = notification;
        _localizer = localizer;
    }

    public AppNotification Notification { get; }

    public string NotificationId => Notification.NotificationId;

    public string Title => Text("Title", Notification.TitleArguments);

    public string Detail => Text("Detail", Notification.DetailArguments);

    public bool IsRead => Notification.IsRead;

    public Visibility UnreadVisibility => IsRead ? Visibility.Collapsed : Visibility.Visible;

    public Visibility ReadVisibility => IsRead ? Visibility.Visible : Visibility.Collapsed;

    public double ContentOpacity => IsRead ? 0.65 : 1;

    /// <summary>Material glyph (warning, error, info). The text carries the meaning; icon is a cue.</summary>
    public string Glyph => Notification.Kind switch
    {
        NotificationKind.LowStock or NotificationKind.LowStockItem => "",
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
                return age.TotalMinutes < 60
                    ? _localizer.Format("Notif_Minutes", Math.Max(1, (int)age.TotalMinutes))
                    : _localizer.Format("Notif_Hours", (int)age.TotalHours);
            }

            return Notification.CreatedAt.Date == DateTimeOffset.Now.Date.AddDays(-1)
                ? _localizer["Notif_Yesterday"]
                : Notification.CreatedAt.ToString(_localizer["Notif_DateFormat"], _localizer.Culture);
        }
    }

    private string Text(string part, IReadOnlyList<string> arguments) =>
        _localizer.Format($"Notif_{Notification.Kind}_{part}", [.. arguments]);
}
