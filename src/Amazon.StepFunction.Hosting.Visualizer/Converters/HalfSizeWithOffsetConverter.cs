using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Amazon.StepFunction.Hosting.Visualizer.Converters
{
  internal class HalfSizeWithOffsetConverter : IValueConverter
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