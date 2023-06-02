using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaTools
{
    public class JsonMessage
    {
        public JsonMessage(Message<string, string> message)
        {
            Key = message.Key;
            Value = message.Value;
            Timestamp = message.Timestamp;
            Headers = new Dictionary<string, byte[]>();
            foreach (var item in message.Headers)
            {
                Headers.Add(item.Key, item.GetValueBytes());
            }
        }
        public JsonMessage() { }
        public Dictionary<string, byte[]> Headers { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public Timestamp Timestamp { get; set; }

        public Message<string, string> ToMessage()
        {
            var message = new Message<string, string>()
            {
                Key = this.Key,
                Value = this.Value,
                Timestamp = this.Timestamp,
                Headers = new Headers()
            };

            foreach (var key in this.Headers.Keys)
            {
                message.Headers.Add(key, this.Headers[key]);
            }

            return message;
        }
    }
}
