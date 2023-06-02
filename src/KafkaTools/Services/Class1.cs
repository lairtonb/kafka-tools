using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaTools.Services
{
    public class MessageEventArgs : EventArgs
    {
        public JsonMessage Message { get; }

        public string TopicName { get; }

        public long Offset { get; private set; }

        public MessageEventArgs(string topic, long offset, JsonMessage message)
        {
            TopicName = topic;
            Offset = offset;
            Message = message;
        }
    }
}