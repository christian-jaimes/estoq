using System.Globalization;
using Estoq.App.Services;
using Estoq.Core.Sales;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.System;

namespace Estoq.App.Controls;

/// <summary>
/// Date range picker for a filter flyout (docs/technical/ui-implementation.md, "Filter bar"): quick-select shortcuts on the left;
/// From and To fields and two month calendars on the right; Cancel and Apply below. A shortcut applies at once; a range typed or picked on the calendars is only a draft until Apply.
/// The month grids are built here (WinUI's CalendarView is single-month and cannot draw this range band). Weeks start on Monday.
/// </summary>
public sealed partial class DateRangePicker : UserControl
{
    private const double DayWidth = 40;
    private const double DayHeight = 38;

    private readonly CultureInfo _culture;
    private readonly DateOnly _today;
    private readonly Action<DateRange?> _apply;
    private readonly Action _close;
    private readonly bool _compact;

    private DateOnly? _from;
    private DateOnly? _to;
    private bool _picking; // true after the first click: the next click sets To
    private DateOnly _leftMonth;

    /// <param name="current">The range now applied (null = all dates).</param>
    /// <param name="apply">Called with the chosen range when Apply is pressed (null = all dates).</param>
    /// <param name="compact">True when the window is too narrow for two months: one month with both arrows, and a narrower flyout.</param>
    /// <param name="close">Closes the flyout; called after Apply and on Cancel.</param>
    public DateRangePicker(DateRange? current, DateOnly today, CultureInfo culture, Action<DateRange?> apply, bool compact, Action close)
    {
        InitializeComponent();
        _compact = compact;
        Root.Width = compact ? 580 : 840;
        RightMonth.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        _culture = culture;
        _today = today;
        _apply = apply;
        _close = close;
        _from = current?.From;
        _to = current?.To;
        _leftMonth = FirstOfMonth(current?.From ?? today);
        Render();
    }

    private static DateOnly FirstOfMonth(DateOnly day) => new(day.Year, day.Month, 1);

    private DateRange? Draft => _from is { } from ? DateRange.Create(from, _to ?? from) : null;

    private void Render()
    {
        FromInput.Text = _from?.ToString("d", _culture) ?? string.Empty;
        ToInput.Text = _to?.ToString("d", _culture) ?? string.Empty;
        BuildQuickList();
        BuildMonth(LeftMonth, _leftMonth, showPrevious: true, showNext: _compact);
        if (!_compact)
        {
            BuildMonth(RightMonth, _leftMonth.AddMonths(1), showPrevious: false, showNext: true);
        }
    }

    private Brush Brush(string key) => ThemeBrushes.Get(this, key);

    private static CornerRadius Radius(string key) => (CornerRadius)Application.Current.Resources[key];

    private Button NewCell() => new()
    {
        Style = (Style)Resources["PickerCellStyle"],
        Foreground = Brush("EqTextBrush"),
    };

    private void BuildQuickList()
    {
        QuickList.Children.Clear();
        var active = DateRange.Match(Draft, _today);
        foreach (var (quick, key) in new[]
        {
            (QuickRange.Today, "Sales_QuickToday"),
            (QuickRange.ThisWeek, "Sales_QuickWeek"),
            (QuickRange.ThisMonth, "Sales_QuickMonth"),
            (QuickRange.ThisYear, "Sales_QuickYear"),
        })
        {
            var content = new Grid { ColumnSpacing = 12, Padding = new Thickness(12, 0, 8, 0) };
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.ColumnDefinitions.Add(new ColumnDefinition());
            content.Children.Add(new FontIcon
            {
                Glyph = "",
                FontFamily = (FontFamily)Application.Current.Resources["EqIconFont"],
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Center,
            });
            var label = new TextBlock { Text = Loc.Get(key), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(label, 1);
            content.Children.Add(label);

            var button = NewCell();
            button.Content = content;
            button.Height = 40;
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.HorizontalContentAlignment = HorizontalAlignment.Left;
            button.CornerRadius = Radius("EqRadiusMd");
            button.Background = active == quick ? Brush("EqGoldTintBrush") : Brush("EqSurfaceElevatedBrush");
            var captured = quick;
            button.Click += (_, _) => ChooseQuick(captured);
            QuickList.Children.Add(button);
        }
    }

    private void ChooseQuick(QuickRange quick)
    {
        var range = DateRange.Of(quick, _today);

        // A shortcut applies at once and closes the picker.
        _apply(range);
        _close();
    }

    private void BuildMonth(StackPanel host, DateOnly month, bool showPrevious, bool showNext)
    {
        host.Children.Clear();

        // Header: arrow at the outer corner, month title centred.
        var header = new Grid { Width = DayWidth * 7, Height = 36 };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
        var title = new TextBlock
        {
            Text = month.ToString("MMMM yyyy", _culture),
            FontFamily = (FontFamily)Application.Current.Resources["EqUiFontSemiBold"],
            FontSize = (double)Application.Current.Resources["EqFontLg"],
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(title, 1);
        header.Children.Add(title);
        if (showPrevious)
        {
            header.Children.Add(ArrowButton("", 0, -1, "Sales_PreviousMonth"));
        }

        if (showNext)
        {
            header.Children.Add(ArrowButton("", 2, 1, "Sales_NextMonth"));
        }

        host.Children.Add(header);

        var grid = new Grid { Width = DayWidth * 7 };
        for (var column = 0; column < 7; column++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(DayWidth) });
        }

        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
        for (var row = 0; row < 6; row++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(DayHeight) });
        }

        // Monday-first weekday names.
        for (var column = 0; column < 7; column++)
        {
            var name = new TextBlock
            {
                Text = _culture.DateTimeFormat.GetAbbreviatedDayName((DayOfWeek)((column + 1) % 7)),
                Style = (Style)Application.Current.Resources["EqSecondaryStyle"],
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(name, column);
            grid.Children.Add(name);
        }

        // Always six week rows so the two months line up and the flyout does not change height.
        var first = month.AddDays(-(((int)month.DayOfWeek + 6) % 7));
        var range = Draft;
        for (var index = 0; index < 42; index++)
        {
            var day = first.AddDays(index);
            var inMonth = day.Month == month.Month;
            var cell = NewCell();
            cell.Width = DayWidth;
            cell.Height = DayHeight;
            cell.Content = day.Day.ToString(_culture);

            if (!inMonth)
            {
                cell.Foreground = Brush("EqTextSecondaryBrush");
                cell.Opacity = 0.5;
                cell.IsHitTestVisible = false;
            }
            else if (range is { } r && (day == r.From || day == r.To))
            {
                cell.Background = Brush("EqGoldBrush");
                cell.Foreground = Brush("EqOnGoldBrush");
                cell.CornerRadius = Radius("EqRadiusMd");
                cell.FontFamily = (FontFamily)Application.Current.Resources["EqUiFontSemiBold"];
            }
            else if (range is { } inRange && inRange.Contains(day))
            {
                cell.Background = Brush("EqGoldTintBrush");
            }

            if (inMonth)
            {
                var captured = day;
                cell.Click += (_, _) => PickDay(captured);
            }

            Grid.SetRow(cell, 1 + index / 7);
            Grid.SetColumn(cell, index % 7);
            grid.Children.Add(cell);
        }

        host.Children.Add(grid);
    }

    private Button ArrowButton(string glyph, int column, int months, string nameKey)
    {
        var button = NewCell();
        button.Width = 36;
        button.Height = 36;
        button.CornerRadius = Radius("EqRadiusMd");
        button.Content = new FontIcon
        {
            Glyph = glyph,
            FontFamily = (FontFamily)Application.Current.Resources["EqIconFont"],
            FontSize = 20,
        };
        AutomationProperties.SetName(button, Loc.Get(nameKey));
        Grid.SetColumn(button, column);
        button.Click += (_, _) =>
        {
            _leftMonth = _leftMonth.AddMonths(months);
            Render();
        };
        return button;
    }

    /// <summary>First click sets From, the second sets To; a click before From starts the range again.</summary>
    private void PickDay(DateOnly day)
    {
        if (!_picking || _from is null || day < _from)
        {
            _from = day;
            _to = null;
            _picking = true;
        }
        else
        {
            _to = day;
            _picking = false;
        }

        Render();
    }

    private void OnFieldKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            Commit((TextBox)sender);
            e.Handled = true;
        }
    }

    private void OnFieldCommit(object sender, RoutedEventArgs e) => Commit((TextBox)sender);

    /// <summary>A typed date replaces From or To when it is valid. Otherwise the last valid value comes back and the field shakes (shared invalid-input feedback).</summary>
    private void Commit(TextBox input)
    {
        var isFrom = input == FromInput;
        var current = isFrom ? _from : _to;
        var text = input.Text.Trim();
        if (text.Length == 0 && current is null)
        {
            return;
        }

        if (!DateOnly.TryParse(text, _culture, DateTimeStyles.None, out var parsed))
        {
            input.Text = current?.ToString("d", _culture) ?? string.Empty;
            Shake(isFrom ? FromBox : ToBox);
            return;
        }

        if (current == parsed)
        {
            return;
        }

        if (isFrom)
        {
            _from = parsed;
            if (_to is { } to && to < parsed)
            {
                _to = parsed;
            }
        }
        else
        {
            _to = parsed;
            _from ??= parsed;
            if (_from is { } from && parsed < from)
            {
                _from = parsed;
            }
        }

        _picking = false;
        _leftMonth = FirstOfMonth(_from ?? parsed);
        Render();
    }

    private static void Shake(UIElement box)
    {
        var transform = ShakeMotion.Prepare(box);
        var storyboard = new Storyboard();
        ShakeMotion.Add(storyboard, transform, 35, -4, 4, -2, 0);
        storyboard.Completed += (_, _) => transform.TranslateX = 0;
        storyboard.Begin();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => _close();

    private void OnApplyClick(object sender, RoutedEventArgs e)
    {
        _apply(Draft);
        _close();
    }
}
