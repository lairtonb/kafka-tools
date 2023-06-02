using System;
using System.Globalization;
using System.Windows.Data;
using Confluent.Kafka;

namespace KafkaTools
{
    public class TimestampConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Timestamp timestamp)
            {
                return timestamp.UtcDateTime;
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}