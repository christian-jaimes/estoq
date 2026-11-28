namespace Estoq.App.ViewModels;

/// <summary>One KPI card. <paramref name="Trend"/> is the arrow (1 up, -1 down, 0 none); <paramref name="Tone"/> its colour (1 good, -1 bad, 0 neutral): more refunds point up and are bad.</summary>
public sealed record DashboardKpi(string Glyph, string Label, string Value, string Change, string ChangeGlyph, int Tone);

/// <summary>A row of the payment legend. <paramref name="ColorIndex"/> is the chart colour slot shared with the donut.</summary>
public sealed record PaymentRow(string Label, string Share, string Amount, int ColorIndex);

public sealed record CategoryRow(string Label, string Share, string Amount, double Fraction, int ColorIndex);

/// <summary><paramref name="IsAlternateRow"/> is true for every second row, which gets the table's banding.</summary>
public sealed record TopProductRow(string Rank, string Name, string Sku, string Units, string Sales, bool IsAlternateRow);

/// <summary><paramref name="IsCritical"/> marks the scarcest items (two or fewer left), shown in the danger colour instead of the warning one; the words say it too.</summary>
public sealed record LowStockRow(string Name, string Sku, string Left, bool IsCritical, bool IsAlternateRow);
