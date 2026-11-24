namespace Estoq.App.Services;

/// <summary>In-memory sample notifications until the back end exists.</summary>
public sealed class MockNotificationService : INotificationService
{
    private List<AppNotification> _items;

    public MockNotificationService()
    {
        var now = DateTimeOffset.Now;
        _items =
        [
            new("n1", NotificationKind.SoldWithoutStock, ["Linen shirt, M"], ["000124"], now.AddMinutes(-10), false),
            new("n2", NotificationKind.LowStock, ["5"], [], now.AddHours(-3), false),
            new("n3", NotificationKind.BackupStale, ["9"], [], now.AddDays(-1).AddHours(-2), false),
            new("n4", NotificationKind.LowStockItem, ["Denim jacket, L", "2"], ["1"], now.AddDays(-3), true),
            new("n5", NotificationKind.BackupFailed, [], [], now.AddDays(-6), true),
        ];
    }

    public event EventHandler? Changed;

    public IReadOnlyList<AppNotification> GetAll() => _items.OrderByDescending(n => n.CreatedAt).ToList();

    public void Add(AppNotification notification) => Update(_items.Append(notification));

    public void MarkRead(string notificationId) =>
        Update(_items.Select(n => n.NotificationId == notificationId ? n with { IsRead = true } : n));

    public void MarkAllRead() => Update(_items.Select(n => n with { IsRead = true }));

    public void Clear(string notificationId) =>
        Update(_items.Where(n => !(n.NotificationId == notificationId && n.IsRead)));

    public void ClearRead() => Update(_items.Where(n => !n.IsRead));

    private void Update(IEnumerable<AppNotification> items)
    {
        _items = items.ToList();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
