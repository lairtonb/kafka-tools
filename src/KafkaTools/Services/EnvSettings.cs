using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace KafkaTools.Services
{
    public class EnvSettings
    {
        public string BrokerUrl { get; set; }
        public string Key { get; set; }
        public string Secret { get; set; }
        public SaslMechanism? SaslMechanism { get; internal set; }
        public SecurityProtocol? SecurityProtocol { get; internal set; }
    }
}
