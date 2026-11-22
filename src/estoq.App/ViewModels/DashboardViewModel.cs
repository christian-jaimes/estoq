using CommunityToolkit.Mvvm.ComponentModel;

namespace Estoq.App.ViewModels;

/// <summary>Placeholder until the dashboard is built per docs/roadmap.md.</summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    public string Title => "Dashboard";

    public string Message => "The dashboard is built after the pop-ups and the Inventory and Sales pages (see roadmap).";
}
