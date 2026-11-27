using System.Globalization;
using Estoq.Core.Returns;

namespace Estoq.App.Services;

/// <summary>
/// Saves returns on the sample data. Returned units are appended to <c>dummy_stock_movements.csv</c> as "return" rows
/// and exchange items as "sale" rows; stock on hand is still the sum of an article's movements.
/// </summary>
public sealed class SampleReturnsService : IReturnsService
{

    private readonly ISaleHistory _history;
    private readonly IPointOfSaleService _pointOfSale;
    private readonly INotificationService _notifications;
    private int _lastReturnNumber;

    public SampleReturnsService(ISaleHistory history, IPointOfSaleService pointOfSale, INotificationService notifications)
    {
        _history = history;
        _pointOfSale = pointOfSale;
        _notifications = notifications;
    }

    /// <summary>Sample store setting: a 30-day window, so the seeded 45-day-old sale shows the "outside the window" message.</summary>
    public int? ReturnWindowDays => 30;

    public SettledSale? FindSale(string receiptNumber) => _history.Find(receiptNumber);

    public ReturnReceipt Save(ReturnRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        _lastReturnNumber++;
        var number = ReturnRules.FormatReturnNumber(_lastReturnNumber);

        WriteMovements(request);
        foreach (var line in request.Returned)
        {
            _pointOfSale.ApplyStockChange(line.VariantId, line.Quantity);
        }

        foreach (var line in request.NewItems)
        {
            _pointOfSale.ApplyStockChange(line.VariantId, -line.Quantity);
        }

        if (request.SaleNumber is { } saleNumber)
        {
            _history.ApplyReturn(saleNumber, request.Returned, request.RefundBase);
            _history.AddItems(saleNumber, request.NewItems);
        }
        else
        {
            // A return without a receipt is allowed at the current price and flagged for the owner.
            _notifications.Add(new AppNotification(
                $"return-{number}",
                NotificationKind.ReturnWithoutReceipt,
                [number],
                [number],
                DateTimeOffset.Now,
                false));
        }

        return new ReturnReceipt(number);
    }

    /// <summary>The return's reason and note as "&lt;reason&gt; - &lt;note&gt;"; just one of them when the other is empty. The reason is stored in English.</summary>
    internal static string Describe(ReturnReason? reason, string note)
    {
        var text = reason switch
        {
            ReturnReason.ChangeOfMind => "Change of mind",
            ReturnReason.WrongSize => "Wrong size",
            ReturnReason.Defective => "Defective or damaged",
            ReturnReason.WrongItem => "Wrong item",
            ReturnReason.Other => "Other",
            _ => string.Empty,
        };
        return StockMovementLog.Describe(text, note);
    }

    /// <summary>Logs the returned units as "return" rows carrying the original receipt number, and exchange items as "sale" rows with the same receipt number.</summary>
    private static void WriteMovements(ReturnRequest request)
    {
        var receipt = request.SaleNumber ?? string.Empty;
        var description = Describe(request.Reason, request.Note);
        var entries = request.Returned.Select(line => new MovementEntry(line.VariantId, "return", line.UnitPrice, line.Quantity, receipt, description)).ToList();
        // Exchange items inherit the receipt number, so they can be returned or exchanged later like any item of that sale.
        entries.AddRange(request.NewItems.Select(line => new MovementEntry(line.VariantId, "sale", line.UnitPrice, -line.Quantity, receipt)));
        StockMovementLog.Append(entries);
    }
}
