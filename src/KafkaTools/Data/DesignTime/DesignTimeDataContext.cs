using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using KafkaTools.Logging;
using KafkaTools.Models;

namespace KafkaTools.Data.DesignTime
{
    public class DesignTimeDataContext
    {
        public ObservableCollection<JsonMessage> DesignTimeMessages { get; set; } =
            new()
            {
                new JsonMessage
                {
                    Key = "Sample Key",
                    Timestamp = new Timestamp(DateTime.Now),
                }
            };

        public ObservableCollection<LogEntry> ApplicationLogs { get; set; } =
            new()
            {
                new LogEntry()
                {
                    Message = "Sample Message 1",
                    Timestamp = DateTime.Now,
                    LogLevel = "Information"
                },
                new LogEntry()
                {
                    Message = "Sample Message 2",
                    Timestamp = DateTime.Now,
                    LogLevel = "Information"
                },
                new LogEntry()
                {
                    Message = "Sample Message 3",
                    Timestamp = DateTime.Now,
                    LogLevel = "Information"
                }
            };
    }
}
