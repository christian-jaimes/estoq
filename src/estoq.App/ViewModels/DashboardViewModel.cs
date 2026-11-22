using CommunityToolkit.Mvvm.ComponentModel;
using Estoq.App.Services;

namespace Estoq.App.ViewModels;

/// <summary>Placeholder until the dashboard is built per docs/roadmap.md.</summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly ILocalizer _localizer;

    public DashboardViewModel(ILocalizer localizer) => _localizer = localizer;

    public string Title => _localizer["Nav_Dashboard"];

    public string Message => _localizer["Page_Dashboard_Message"];
}
