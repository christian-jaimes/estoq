namespace Estoq.App.Services;

/// <summary>The kinds of notification the app raises (see docs/features/app-shell.md).</summary>
public enum NotificationKind
{
    LowStock,
    SoldWithoutStock,
    BackupStale,
}

/// <summary>One notification. The text carries the figure; the kind picks the icon.</summary>
public sealed record AppNotification(
    string NotificationId,
    NotificationKind Kind,
    string Title,
    string Detail,
    DateTimeOffset CreatedAt,
    bool IsRead);

/// <summary>Source of notifications. Replaced by the stored implementation when the back end exists.</summary>
public interface INotificationService
{
    /// <summary>All notifications, newest first.</summary>
    IReadOnlyList<AppNotification> GetAll();

    event EventHandler? Changed;

    void MarkRead(string notificationId);

    void MarkAllRead();

    /// <summary>Removes one notification. Only read notifications can be cleared.</summary>
    void Clear(string notificationId);

    /// <summary>Removes every read notification.</summary>
    void ClearRead();
}
