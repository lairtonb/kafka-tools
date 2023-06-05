using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KafkaTools.Configuration;

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
                // We provide a way for the appsettingsFile author to enforce a specific order.
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
                        case "KeyVault":
                            var keyVaultEnvironmentSettings = childSection.Get<KeyVaultEnvironmentSettings>();
                            dictionary.Add(itemKey, keyVaultEnvironmentSettings ?? new KeyVaultEnvironmentSettings { AuthenticationType = "KeyVault" });
                            break;
                        case "UserSecrets":
                            var userSecretsEnvironmentSettings = childSection.Get<UserSecretsEnvironmentSettings>();
                            dictionary.Add(itemKey, userSecretsEnvironmentSettings ?? new UserSecretsEnvironmentSettings { AuthenticationType = "UserSecrets" });
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
