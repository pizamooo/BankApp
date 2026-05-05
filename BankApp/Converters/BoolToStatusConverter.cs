using System;
using System.Globalization;
using System.Windows.Data;

namespace BankApp.Converters
{
    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isClosed = (bool)value;
            return isClosed ? "Закрыт" : "Активен";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}