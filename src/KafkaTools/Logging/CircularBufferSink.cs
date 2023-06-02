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

            /* foreach (var entry in buffer.GetItems())
            {
                _logEntries.Add(entry);
            } */

            OnPropertyChanged(nameof(LogEntries));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, message));
        }

        // readonly ObservableCollection<LogEntry> _logEntries = new ObservableCollection<LogEntry>();

        public IEnumerable<LogEntry> LogEntries => buffer.GetItems();

        /*
        public ObservableCollection<LogEntry> LogEntries
        {
            get
            {
                return _logEntries;
            }
        }
        */

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
    }

}
