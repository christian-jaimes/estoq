namespace Estoq.App.Services;

/// <summary>The kinds of notification the app raises (see docs/features/app-shell.md). Each kind has its own localized text.</summary>
public enum NotificationKind
{
    LowStock,
    LowStockItem,
    SoldWithoutStock,
    BackupStale,
    BackupFailed,
}

/// <summary>
/// One notification. Stored as kind plus arguments, never as text, so it reads in the current app language.
/// The arguments fill the kind's strings (Notif_{Kind}_Title and _Detail in Strings.resx).
/// </summary>
public sealed record AppNotification(
    string NotificationId,
    NotificationKind Kind,
    IReadOnlyList<string> TitleArguments,
    IReadOnlyList<string> DetailArguments,
    DateTimeOffset CreatedAt,
    bool IsRead);

/// <summary>Source of notifications. Replaced by the stored implementation when the back end exists.</summary>
public interface INotificationService
{
    /// <summary>All notifications, newest first.</summary>
    IReadOnlyList<AppNotification> GetAll();

    event EventHandler? Changed;

    /// <summary>Raises a notification (for example a sale of an item without stock).</summary>
    void Add(AppNotification notification);

    void MarkRead(string notificationId);

    void MarkAllRead();

    /// <summary>Removes one notification. Only read notifications can be cleared.</summary>
    void Clear(string notificationId);

    /// <summary>Removes every read notification.</summary>
    void ClearRead();
}
