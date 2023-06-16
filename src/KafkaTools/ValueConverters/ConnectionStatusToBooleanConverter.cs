using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using KafkaTools.Models;

namespace KafkaTools.ValueConverters
{
    public class ConnectionStatusToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConnectionStatus status && Enum.TryParse(parameter as string, out ConnectionStatus targetStatus))
            {
                return status == targetStatus;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsOkToSubscribeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Access the values passed from the bindings
            var isConnected = values[0] is ConnectionStatus and ConnectionStatus.Connected;
            var isTopicSelected = values[1] is TopicInfo;

            // Perform the desired logic based on the values

            // Combine the isConnected value with the isTopicSelected
            // This is what determine if the "Play" button is enabled or not
            return isConnected && isTopicSelected;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
