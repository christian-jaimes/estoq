using Estoq.Core.Settings;

namespace Estoq.App.Services;

/// <summary>
/// Development stand-in: returns the default threshold. On feat/cpt13-settings the owner sets it in Store settings
/// (<c>ISettingsService.LowStockThreshold</c>, key <see cref="LowStockPolicy.SettingKey"/>); when the branches merge this service reads that value.
/// </summary>
public sealed class SampleStockSettings : IStockSettings
{
    public int LowStockThreshold => LowStockPolicy.DefaultThreshold;
}
