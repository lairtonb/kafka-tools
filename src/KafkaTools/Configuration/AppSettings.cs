using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace KafkaTools.Configuration
{
    public class AppSettings
    {
        public Dictionary<string, EnvironmentSettings> Environments { get; set; }
    }

    public class EnvironmentSettings
    {
        public string EnvironmentName { get; set; }
        public string BrokerUrl { get; set; }
        public string AuthenticationType { get; set; }
        public SaslMechanism? SaslMechanism { get; set; }
        public SecurityProtocol? SecurityProtocol { get; set; }
    }

    public class UserSecretsEnvironmentSettings : EnvironmentSettings
    {
        public string UserSecretsId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

    public class KeyVaultEnvironmentSettings : EnvironmentSettings
    {
        public string KeyVaultUrl { get; set; }
        public string ClientIdKey { get; set; }
        public string ClientSecretKey { get; set; }
    }

}
