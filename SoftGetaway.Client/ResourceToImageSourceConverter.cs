using System;
using System.Windows.Data;
using System.Windows.Media;

namespace softGetawayClient
{
    public class ResourceToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (ImageSource)App.Current.FindResource(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException("The method or operation is not implemented.");
        }
    }
}
