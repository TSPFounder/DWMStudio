// DwmConverters.cs
// IValueConverter implementations used by DWM Studio XAML views.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DWMStudio.Converters
{
    /// <summary>bool → 1.0 (true) or 0.18 (false) — pipeline stage opacity.</summary>
    public sealed class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value is true ? 1.0 : 0.18;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            throw new NotSupportedException();
    }

    /// <summary>string → bool: true when non-null and non-empty.</summary>
    public sealed class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            !string.IsNullOrWhiteSpace(value as string);
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            throw new NotSupportedException();
    }

    /// <summary>int → Visible when > 0, else Collapsed.</summary>
    public sealed class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value is int n && n > 0 ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            throw new NotSupportedException();
    }

    /// <summary>
    /// int currentStep + parameter stepNumber → Visible when equal, else Collapsed.
    /// </summary>
    public sealed class StepVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            if (value is int cur && p is string ps && int.TryParse(ps, out int step))
                return cur == step ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            throw new NotSupportedException();
    }

    /// <summary>bool -> Collapsed (true) / Visible (false).</summary>
    public sealed class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value is true ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            throw new NotSupportedException();
    }
}
