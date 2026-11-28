namespace Estoq.Core.Settings;

/// <summary>One audited change: what changed, from what to what, who and when. Settings are never changed silently.</summary>
public sealed record SettingChange(
    SettingsSection Section,
    string Field,
    string? OldValue,
    string? NewValue,
    string User,
    DateTimeOffset At);

/// <summary>Compares the saved and the edited fields of a section and returns one entry per difference.</summary>
public static class SettingsDiff
{
    public static IReadOnlyList<SettingChange> Compare(
        SettingsSection section,
        IReadOnlyDictionary<string, string> before,
        IReadOnlyDictionary<string, string> after,
        string user,
        DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        var changes = new List<SettingChange>();
        foreach (var (field, newValue) in after)
        {
            before.TryGetValue(field, out var oldValue);
            if (!string.Equals(oldValue ?? string.Empty, newValue ?? string.Empty, StringComparison.Ordinal))
            {
                changes.Add(new SettingChange(section, field, oldValue, newValue, user, at));
            }
        }

        return changes;
    }
}
