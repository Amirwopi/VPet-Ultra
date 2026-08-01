using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace VPet.Cognition.Wpf.Converters;

/// <summary>True (user) → Right, False (assistant) → Left.</summary>
public sealed class BoolToAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b && b ? HorizontalAlignment.Right : HorizontalAlignment.Left;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>True (user) → user bubble color, False → assistant bubble color.</summary>
public sealed class BoolToBubbleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b && b ? new SolidColorBrush(Color.FromRgb(0x7B, 0x68, 0xEE))
                              : new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x5C));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}