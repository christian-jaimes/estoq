using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.Core.Roles;
using Estoq.Core.Settings;

namespace Estoq.App.ViewModels.Settings;

/// <summary>One staff row in the table (mockup 11).</summary>
public sealed partial class StaffRowViewModel : ObservableObject
{
    public StaffRowViewModel(StaffMember member, string roleText, string statusText, bool canManage, bool isAlternate)
    {
        Member = member;
        RoleText = roleText;
        StatusText = statusText;
        CanManage = canManage;
        IsAlternateRow = isAlternate;
        Initials = AccountSettingsViewModel.MakeInitials(member.Name);
    }

    public StaffMember Member { get; }

    public Guid Id => Member.Id;

    public string Name => Member.Name;

    /// <summary>The email, or the sign-in code hint when the person has none.</summary>
    public string Detail => Member.Email.Length > 0 ? Member.Email : string.Empty;

    public bool HasDetail => Member.Email.Length > 0;

    public string Initials { get; }

    public string RoleText { get; }

    public string StatusText { get; }

    public bool IsActive => Member.Status == AccountStatus.Active;

    public bool IsInactive => !IsActive;

    /// <summary>False when this person's role may not edit or deactivate this account (a manager looking at the owner).</summary>
    public bool CanManage { get; }

    public bool IsAlternateRow { get; }

    /// <summary>Menu text for the activate or deactivate action.</summary>
    public string ToggleStatusText { get; set; } = string.Empty;
}

/// <summary>
/// The Staff section: search, status filter, table, and an Add or edit form. Each change applies at once through
/// <see cref="ISettingsService"/> (which enforces the rules and writes the audit entry); it does not wait for Save changes.
/// </summary>
public sealed partial class StaffSettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ISessionService _session;
    private readonly ILocalizer _localizer;
    private Guid? _editingId;

    public StaffSettingsViewModel(ISettingsService settings, ISessionService session, ILocalizer localizer)
    {
        _settings = settings;
        _session = session;
        _localizer = localizer;
        _settings.Changed += (_, _) => Refresh();
    }

    public ObservableCollection<StaffRowViewModel> Rows { get; } = [];

    public ObservableCollection<RoleChoice> Roles { get; } = [];

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    /// <summary>0 all, 1 active, 2 inactive.</summary>
    [ObservableProperty]
    public partial int StatusFilterIndex { get; set; }

    public bool HasRows => Rows.Count > 0;

    public bool HasNoRows => Rows.Count == 0;

    // The Add or edit form.
    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string FormTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FormName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FormEmail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial RoleChoice? FormRole { get; set; }

    [ObservableProperty]
    public partial string FormError { get; set; } = string.Empty;

    /// <summary>False when editing the owner: their role cannot change.</summary>
    [ObservableProperty]
    public partial bool CanChangeRole { get; set; } = true;

    [ObservableProperty]
    public partial string FormCodeHint { get; set; } = string.Empty;

    /// <summary>A message after a change, for example the new sign-in code to hand to the person.</summary>
    [ObservableProperty]
    public partial string Notice { get; set; } = string.Empty;

    public bool HasNotice => Notice.Length > 0;

    public void Reset()
    {
        Search = string.Empty;
        StatusFilterIndex = 0;
        CloseForm();
        Notice = string.Empty;
        RefreshStrings();
    }

    public void RefreshStrings()
    {
        var keep = FormRole?.Role;
        Roles.Clear();
        foreach (var role in new[] { UserRole.Manager, UserRole.Cashier })
        {
            Roles.Add(new RoleChoice(role, _localizer[$"Role_{role}"]));
        }

        FormRole = Roles.FirstOrDefault(r => r.Role == keep) ?? Roles.LastOrDefault();
        Refresh();
    }

    [RelayCommand]
    private void Add()
    {
        _editingId = null;
        FormTitle = _localizer["Settings_StaffAddTitle"];
        FormName = FormEmail = FormError = string.Empty;
        FormRole = Roles.FirstOrDefault(r => r.Role == UserRole.Cashier);
        CanChangeRole = true;
        FormCodeHint = _localizer["Settings_StaffCodeHintNew"];
        Notice = string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit(StaffRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        _editingId = row.Id;
        FormTitle = _localizer.Format("Settings_StaffEditTitle", row.Name);
        FormName = row.Name;
        FormEmail = row.Member.Email;
        FormError = string.Empty;
        CanChangeRole = row.Member.Role != UserRole.Owner;
        FormRole = Roles.FirstOrDefault(r => r.Role == row.Member.Role)
            ?? new RoleChoice(UserRole.Owner, _localizer[$"Role_{UserRole.Owner}"]);
        FormCodeHint = string.Empty;
        Notice = string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void SaveForm()
    {
        var role = FormRole?.Role ?? UserRole.Cashier;
        StaffError error;
        if (_editingId is { } id)
        {
            error = _settings.EditStaff(id, FormName, FormEmail, role);
        }
        else
        {
            error = _settings.AddStaff(FormName, FormEmail, role, out var added);
            if (error == StaffError.None && added is not null)
            {
                Notice = _localizer.Format("Settings_StaffAddedNotice", added.Name, added.Code);
            }
        }

        if (error != StaffError.None)
        {
            FormError = _localizer[$"Settings_StaffError_{error}"];
            return;
        }

        CloseForm();
    }

    [RelayCommand]
    private void CancelForm() => CloseForm();

    [RelayCommand]
    private void ToggleStatus(StaffRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        var next = row.IsActive ? AccountStatus.Inactive : AccountStatus.Active;
        var error = _settings.SetStaffStatus(row.Id, next);
        Notice = error == StaffError.None ? string.Empty : _localizer[$"Settings_StaffError_{error}"];
    }

    [RelayCommand]
    private void RegenerateCode(StaffRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        var error = _settings.RegenerateStaffCode(row.Id);
        Notice = error == StaffError.None
            ? _localizer.Format("Settings_StaffCodeNotice", row.Name, _settings.Staff.First(m => m.Id == row.Id).Code)
            : _localizer[$"Settings_StaffError_{error}"];
    }

    [RelayCommand]
    private void DismissNotice() => Notice = string.Empty;

    private void CloseForm()
    {
        IsEditing = false;
        _editingId = null;
        FormError = string.Empty;
    }

    private void Refresh()
    {
        var actor = _session.Role;
        var query = Search.Trim();
        var rows = _settings.Staff
            .Where(m => StatusFilterIndex == 0 || (StatusFilterIndex == 1) == (m.Status == AccountStatus.Active))
            .Where(m => query.Length == 0
                || m.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || m.Email.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .Select((m, i) => new StaffRowViewModel(
                m,
                _localizer[$"Role_{m.Role}"],
                _localizer[$"Status_{m.Status}"],
                RolePermissions.CanManageAccount(actor, m.Role),
                i % 2 == 1)
            {
                ToggleStatusText = _localizer[m.Status == AccountStatus.Active ? "Settings_StaffDeactivate" : "Settings_StaffActivate"],
            })
            .ToList();

        Rows.Clear();
        foreach (var row in rows)
        {
            Rows.Add(row);
        }

        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(HasNoRows));
    }

    partial void OnSearchChanged(string value) => Refresh();

    partial void OnStatusFilterIndexChanged(int value) => Refresh();

    partial void OnNoticeChanged(string value) => OnPropertyChanged(nameof(HasNotice));
}
