using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using KafkaTools.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Configuration
{
    public static class AppSettingsConfigurationExtensions
    {
        public static Dictionary<string, EnvironmentSettings> GetEnvironments(this IConfiguration configuration,
            string key)
        {
            var dictionary = new Dictionary<string, EnvironmentSettings>();
            var section = configuration.GetSection(key);
            foreach (var childSection in section.GetChildren().OrderBy(c =>
            {
                // Get Children seem to returns ordered alphabetically
                // We provide a way for the appsettings file author to enforce a specific order.
                var order = c.GetSection("Order")?.Get<int>() ?? 0;
                return order;
            }))
            {
                var itemKey = childSection.Key;
                var childrenSection = childSection.GetChildren();
                if (childrenSection.Any())
                {
                    var environmentSettings = childSection.Get<EnvironmentSettings>()
                        ?? throw new AppSettingsException($"An environment setting session must have values: {itemKey}");
                    switch (environmentSettings.AuthenticationType)
                    {
                        case "None":
                            environmentSettings.EnvironmentName = itemKey;
                            dictionary.Add(itemKey, environmentSettings);
                            break;

                        case "KeyVault":
                            var keyVaultEnvironmentSettings = childSection.Get<KeyVaultEnvironmentSettings>()!;
                            keyVaultEnvironmentSettings.EnvironmentName = itemKey;
                            dictionary.Add(itemKey, keyVaultEnvironmentSettings);
                            break;

                        case "UserSecrets":
                            var userSecretsEnvironmentSettings = childSection.Get<UserSecretsEnvironmentSettings>()!;
                            userSecretsEnvironmentSettings.EnvironmentName = itemKey;
                            dictionary.Add(itemKey, userSecretsEnvironmentSettings);
                            break;

                        default:
                            throw new AppSettingsException(
                                    $"Unsupported AuthenticationType for environment ({itemKey}): {environmentSettings.AuthenticationType ?? "<null>"}",
                                    "AuthenticationType"
                                );
                    }
                }
            }

            // Validate either System.ComponentModel.DataAnnotations or System.Options.Configuration.DataAnnotations
            return dictionary;
        }
    }
}
