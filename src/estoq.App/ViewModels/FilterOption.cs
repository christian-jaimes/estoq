namespace Estoq.App.ViewModels;

/// <summary>One entry of a filter or sort menu. A null value means "all".</summary>
public sealed record FilterOption(string? Value, string Label);
