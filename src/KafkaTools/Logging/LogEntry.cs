using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaTools.Logging
{
    public class LogEntry
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string LogLevel { get; set; }
    }
}
