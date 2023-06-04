using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;

namespace KafkaTools.Logging
{
    public class CircularBufferSink : ILogEventSink, INotifyPropertyChanged, INotifyCollectionChanged
    {
        private readonly CircularBuffer<LogEntry> buffer;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public CircularBufferSink(int capacity)
        {
            buffer = new CircularBuffer<LogEntry>(capacity);
        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            var logEntry = new LogEntry()
            {
                Message = message,
                Timestamp = logEvent.Timestamp.DateTime,
                LogLevel = logEvent.Level.ToString()
            };
            buffer.Enqueue(logEntry);

            RaisePropertyChanged(nameof(LogEntries));
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, message));
        }

        public IEnumerable<LogEntry> LogEntries => buffer.GetItems();

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
    }

}
