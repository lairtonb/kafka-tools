using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using KafkaTools.Services;
using Notifications.Wpf.Core;

namespace KafkaTools.Models
{
    public class TopicInfo
    {
        public TopicInfo(string topicName)
        {
            TopicName = topicName;
        }

        public string TopicName { get; private set; }

        public long Offset { get; private set; }

        public ObservableCollection<JsonMessage> Messages { get; } =
            new ObservableCollection<JsonMessage>();

        public EventHandler<MessageEventArgs> MessagePublished
        {
            get
            {
                return OnMessagePublished;
            }
        }

        private void OnMessagePublished(object? sender, MessageEventArgs e)
        {
            if (e.TopicName == TopicName && e.Offset > Offset)
            {
                Application.Current?.Dispatcher.Invoke(
                    () =>
                    {
                        Offset = e.Offset;
                        Messages.Add(e.Message);
                    });
            }
        }
    }
}
