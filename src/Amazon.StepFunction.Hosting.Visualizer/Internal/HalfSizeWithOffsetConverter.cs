using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Amazon.StepFunction.Hosting.Visualizer.Internal
{
  /// <summary>Converts to <see cref="Size"/> plus a fixed offset for use in node positioning</summary>
  internal sealed class HalfSizeWithOffsetConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var offset = System.Convert.ToDouble(parameter);

      if (value is Size size)
      {
        return new Size((size.Width + offset) / 2, (size.Height + offset) / 2);
      }

      return new Size(offset / 2, offset / 2);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var offset = System.Convert.ToDouble(parameter);

      if (value is Size size)
      {
        return new Size((size.Width + offset) / 2, (size.Height + offset) / 2);
      }

      return new Size(offset / 2, offset / 2);
    }
  }
}