using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DS4WinWPF.DS4Forms.Converters
{
    public class MissingToGrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isMissing = value is bool b && b;
            return isMissing ? Brushes.Gray : Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
