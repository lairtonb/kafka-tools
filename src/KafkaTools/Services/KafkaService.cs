using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Confluent.Kafka;
using KafkaTools.Configuration;
using KafkaTools.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Wpf.Core;
using static System.Net.WebRequestMethods;
using static Confluent.Kafka.ConfigPropertyNames;

namespace KafkaTools.Services
{
    internal static class ConfluentConstants
    {
        public const byte MagicByte = 0;
    }

    public class KafkaService
    {
        private readonly IMemoryCache _envCache;
        private readonly INotificationManager _notificationManager;

        private readonly Dispatcher _dispatcher;
        private readonly AppSettings _appSettings;
        private readonly ILogger<KafkaService> _logger;
        private CancellationTokenSource _cancellationTokenSource;

        public object JsonConvert { get; private set; }

        public KafkaService(IMemoryCache envCache, INotificationManager notificationManager,
            IOptions<AppSettings> options, ILogger<KafkaService> logger)
        {
            _envCache = envCache;
            _notificationManager = notificationManager;
            _appSettings = options.Value;
            _logger = logger;

            _dispatcher ??= Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            _cancellationTokenSource = new CancellationTokenSource();
        }

        private event EventHandler<MessageEventArgs>? MessagePublished;

        public void Subscribe(EventHandler<MessageEventArgs> messagePublishedEventHandler)
        {
            if (MessagePublished == null || !MessagePublished.GetInvocationList().Contains(messagePublishedEventHandler))
            {
                MessagePublished += messagePublishedEventHandler;
            }
        }

        public void Unsubscribe(EventHandler<MessageEventArgs> messagePublishedEventHandler)
        {
            if (MessagePublished != null && MessagePublished.GetInvocationList().Contains(messagePublishedEventHandler))
            {
                MessagePublished -= messagePublishedEventHandler;
            }
        }

        public async Task<List<string>> GetTopicsAsync(string selectedSourceEnvironment)
        {
            var envSettings = await GetEnvSettingsAsync(selectedSourceEnvironment!);

            var identifier = Guid.NewGuid();

            await _notificationManager.ShowAsync(identifier, new NotificationContent
            {
                Title = "Information",
                Message = "Getting topics from server.",
                Type = NotificationType.Information
            }, "WindowArea", TimeSpan.MaxValue);

            var adminConfig = new AdminClientConfig()
            {
                BootstrapServers = envSettings.BrokerUrl,
            };

            if (!string.IsNullOrEmpty(envSettings.Key) && !string.IsNullOrEmpty(envSettings.Secret))
            {
                adminConfig.SaslMechanism = envSettings.SaslMechanism;
                adminConfig.SecurityProtocol = envSettings.SecurityProtocol;
                adminConfig.SaslUsername = envSettings.Key;
                adminConfig.SaslPassword = envSettings.Secret;
            }

            var topics = new List<string>();

            await Task.Run(() =>
            {
                using (var adminClient = new AdminClientBuilder(adminConfig).Build())
                {
                    var metadata = adminClient.GetMetadata(timeout: TimeSpan.FromMinutes(60));
                    var topicNames = metadata.Topics
                        .Where(t => !t.Topic.StartsWith("_")
                            && !t.Topic.StartsWith("docker-")
                            && !t.Topic.StartsWith("default_ksql_processing_log"))
                        .OrderBy(t => t.Topic)
                        .Select(t => t.Topic)
                        .ToList();
                    topics.AddRange(topicNames);
                }
                _dispatcher.BeginInvoke(async () => await _notificationManager.CloseAsync(identifier));
            });

            return topics;
        }

        private readonly Dictionary<string, string> _currentTopics = new();

        public async Task StopConsumingAsync(string environmentName, string selectedTopic, AutoOffsetReset selectedAutoOffsetReset)
        {
            // Get consumer from cache or new consumer
            var consumer = await GetConsumerAsync(environmentName, selectedAutoOffsetReset);

            // Remove topic from current topics
            if (_currentTopics.ContainsKey(selectedTopic))
            {
                _currentTopics.Remove(selectedTopic);
            }

            // Update the topic subscription.
            // Any previous subscription will be unassigned and
            // the new subscription will replace it.
            // If the new subscription list is empty, it is
            // treated the same as Unsubscribe.
            consumer.Subscribe(_currentTopics.Select(t => t.Value));
        }

        public async Task StartConsumingAsync(string environmentName, string selectedTopic, AutoOffsetReset selectedAutoOffsetReset)
        {
            var consumer = await GetConsumerAsync(environmentName, selectedAutoOffsetReset);

            try
            {
                if (!_currentTopics.ContainsKey(selectedTopic))
                {
                    _currentTopics.Add(selectedTopic, selectedTopic);
                    consumer.Subscribe(_currentTopics.Select(t => t.Value));
                }

                while (true)
                {
                    var consumeResult = consumer.Consume(_cancellationTokenSource.Token);

                    // Check if a stop signal is received
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        // Reset the token, to allow the consumer to be restarted
                        _cancellationTokenSource = new CancellationTokenSource();

                        // Stop consuming and disconnect
                        consumer.Close();
                        break;
                    }

                    if (consumeResult == null)
                    {
                        throw new KafkaServiceException("Cabol!");
                    }

                    if (consumeResult.IsPartitionEOF)
                    {
                        // Should we notify that we consumed all messages available so far?
                        continue;
                    }

                    using var stream = new MemoryStream(consumeResult.Message.Value);
                    using var reader = new BinaryReader(stream);

                    // Just first 5 bytes for now (Confluent Schema Registry framing)                
                    var magicByte = reader.ReadByte();
                    if (magicByte != ConfluentConstants.MagicByte)
                    {
                        stream.Position = 0;
                    }
                    else
                    {
                        var nextByte = reader.ReadByte();
                        if (nextByte == 123) // 123 (7B HEX) = '{' in ASCII/UTF-8 (first byte of JSON) - not considering Json array (nextByte == 91)
                        {
                            // Customization made to work with messages that are not registered in Schema Registry
                            // but that have the magic byte and the first byte of the message is '{'.
                            // I don't know if this is the best approach, but it is working for now.
                            // TODO : Improve this code (will probably rewrite everything when implementing Avro)
                            stream.Position = 1;
                        }
                        else
                        {
                            // Discard the registryId, since we are not considering it at this moment
                            stream.Position -= 1;
                            _ = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                        }
                    }

                    var streamReader = new StreamReader(stream);
                    var valueString = await streamReader.ReadToEndAsync();

                    var message = new JsonMessage
                    {
                        Headers = new Dictionary<string, byte[]>()
                    };
                    foreach (var header in consumeResult.Message.Headers)
                    {
                        message.Headers.Add(header.Key, header.GetValueBytes());
                    }
                    message.Key = consumeResult.Message.Key;
                    message.Value = valueString;
                    message.Offset = consumeResult.Offset.Value;
                    message.Timestamp = consumeResult.Message.Timestamp;
                    message.IsNew = true;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessagePublished?.Invoke(this, new MessageEventArgs(consumeResult.Topic,
                            consumeResult.Offset, message));
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while consuming Kafka messages");
                await _dispatcher.Invoke(() => _notificationManager.CloseAllAsync());
                await _notificationManager.ShowAsync(new NotificationContent
                {
                    Title = "Error",
                    Message = ex.Message,
                    Type = NotificationType.Error
                }, "WindowArea");

                throw;
            }
        }

        private async Task<EnvSettings> GetEnvSettingsAsync(string environment)
        {
            var cachedEnvSettings = _envCache.Get(environment);
            if (cachedEnvSettings != null)
            {
                return (EnvSettings)cachedEnvSettings;
            }

            var brokerUrl = _appSettings.Environments[environment].BrokerUrl;
            var saslMechanism = _appSettings.Environments[environment].SaslMechanism;
            var securityProtocol = _appSettings.Environments[environment].SecurityProtocol;

            var (key, secret) = _appSettings.Environments[environment] switch
            {
                KeyVaultEnvironmentSettings environmentSettings => await GetKeyVaultEnvSettingsAsync(environmentSettings),
                UserSecretsEnvironmentSettings userSecretsEnvironmentSettings => await GetUserSecretsEnvironmentSettings(userSecretsEnvironmentSettings),
                EnvironmentSettings => (string.Empty, string.Empty),
                _ => throw new ArgumentException("Invalid environmentName", nameof(environment))
            };

            var envSettings = new EnvSettings()
            {
                BrokerUrl = brokerUrl,
                SaslMechanism = saslMechanism,
                SecurityProtocol = securityProtocol,
                Key = key,
                Secret = secret
            };

            _envCache.Set(environment, envSettings,
                new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) }
            );

            return envSettings;
        }

        /// <summary>
        /// Get settings from user secrets
        /// </summary>
        private static Task<(string key, string secret)> GetUserSecretsEnvironmentSettings(UserSecretsEnvironmentSettings userSecretsEnvironmentSettings)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddUserSecrets(userSecretsEnvironmentSettings.UserSecretsId);
            var config = configurationBuilder.Build();
            var key = config.GetSection($"AppSettings:Environments:{userSecretsEnvironmentSettings.EnvironmentName}:ClientId").Value;
            var secret = config.GetSection($"AppSettings:Environments:{userSecretsEnvironmentSettings.EnvironmentName}:ClientSecret").Value;
            return Task.FromResult((key!, secret!));
        }

        private async Task<(string key, string secret)> GetKeyVaultEnvSettingsAsync(KeyVaultEnvironmentSettings environment)
        {
            SecretClientOptions options = new()
            {
                Retry =
                {
                    Delay= TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                 }
            };

            var key = string.Empty;
            var secret = string.Empty;

            var identifier = Guid.NewGuid();

            await _notificationManager.ShowAsync(identifier, new NotificationContent
            {
                Title = "Information",
                Message = "Getting credentials from KeyVault...",
                Type = NotificationType.Information
            }, "WindowArea", expirationTime: TimeSpan.MaxValue);

            try
            {
                var kvClient = new SecretClient(new Uri(environment.KeyVaultUrl), new DefaultAzureCredential(), options);
                var kvSecret = (await kvClient.GetSecretAsync("cm--kafka-key--tooling")).Value;
                key = kvSecret.Value;

                kvSecret = (await kvClient.GetSecretAsync("cm--kafka-secret--tooling")).Value;
                secret = kvSecret.Value;
            }
            finally
            {
                await _dispatcher.BeginInvoke(async () =>
                {
                    await _notificationManager.CloseAsync(identifier);
                });
            }

            await _notificationManager.ShowAsync(new NotificationContent
            {
                Title = "Success",
                Message = "Done getting credentials from KeyVault.",
                Type = NotificationType.Success
            }, "WindowArea");

            return (key, secret);
        }

        private readonly Dictionary<string, IConsumer<string, byte[]>> _existingConsumers = new();

        private async Task<IConsumer<string, byte[]>> GetConsumerAsync(string selectedSourceEnvironmentName, AutoOffsetReset selectedAutoOffsetReset)
        {
            var environmentNamePlusAutoOffsetResetCompositeKey = $"{selectedSourceEnvironmentName}-{selectedAutoOffsetReset}";
            if (_existingConsumers.ContainsKey(environmentNamePlusAutoOffsetResetCompositeKey))
            {
                return _existingConsumers[selectedSourceEnvironmentName];
            }

            try
            {
                var envSettings = await GetEnvSettingsAsync(selectedSourceEnvironmentName!);

                var consumerConfig = new ConsumerConfig();

                if (!string.IsNullOrWhiteSpace(envSettings.Key) && !string.IsNullOrWhiteSpace(envSettings.Secret))
                {
                    consumerConfig.SaslMechanism = SaslMechanism.Plain;
                    consumerConfig.SecurityProtocol = SecurityProtocol.SaslSsl;
                    consumerConfig.SaslUsername = envSettings.Key;
                    consumerConfig.SaslPassword = envSettings.Secret;
                }

                // There are many, many more options to configure!!!!
                var config = new ConsumerConfig(consumerConfig)
                {
                    /*
                    Initial list of brokers as a CSV list of broker host or host:port. The application
                    may also use `rd_kafka_brokers_add()` to add brokers during runtime. default:
                    '' importance: high                     
                    */
                    BootstrapServers = envSettings.BrokerUrl,

                    /*
                    Client group id string. All clients sharing the same group.id belong to the same
                    group. default: '' importance: high                     
                    Notes:
                    -  For Confluent, the group.id may have some rules set up in the broker,
                       like starting with env name, like this example:
                       GroupId = $"ci-tooling-webhookeventadapter-worker-lairton-local",
                    - Not sure about if other brokers have similar rules.
                    */
                    GroupId = $"{selectedSourceEnvironmentName.ToLowerInvariant()}-tooling-messageviewer-worker-local",

                    /*
                    Automatically and periodically commit offsets in the background. Note: setting
                    this to false does not prevent the consumer from fetching previously committed
                    start offsets. To circumvent this behaviour set specific start offsets per partition
                    in the call to assign(). default: true importance: high
                    */
                    EnableAutoCommit = false,

                    /*
                    librdkafka statistics emit interval. The application also needs to register a
                    stats callback using `rd_kafka_conf_set_stats_cb()`. The granularity is 1000ms.
                    A value of 0 disables statistics. default: 0 importance: high                     
                    */
                    StatisticsIntervalMs = 5000,

                    /*
                    Client group session and failure detection timeout. The consumer sends periodic
                    heartbeats (heartbeat.interval.ms) to indicate its liveness to the broker. If
                    no hearts are received by the broker for a group member within the session timeout,
                    the broker will remove the consumer from the group and trigger a rebalance. The
                    allowed range is configured with the **broker** configuration properties `group.min.session.timeout.ms`
                    and `group.max.session.timeout.ms`. Also see `max.poll.interval.ms`. default:
                    45000 importance: high

                    Setting this to a low value ensures the consumer leaves the group quickly
                    when the consumer is closed.
                    */
                    SessionTimeoutMs = 10000,

                    /*
                    Action to take when there is no initial offset in offset store or the desired
                    offset is out of range: 'smallest','earliest' - automatically reset the offset
                    to the smallest offset, 'largest','latest' - automatically reset the offset to
                    the largest offset, 'error' - trigger an error (ERR__AUTO_OFFSET_RESET) which
                    is retrieved by consuming messages and checking 'message->err'. default: largest
                    importance: high  
                    */
                    AutoOffsetReset = selectedAutoOffsetReset,
                    //// AutoOffsetReset.Earliest,
                    //// AutoOffsetReset = AutoOffsetReset.Latest;

                    /*
                    Emit RD_KAFKA_RESP_ERR__PARTITION_EOF event whenever the consumer reaches the
                    end of a partition. default: false importance: low
                    */
                    EnablePartitionEof = true,

                    /// A good introduction to the CooperativeSticky assignor and incremental rebalancing:
                    /// https://www.confluent.io/blog/cooperative-rebalancing-in-kafka-streams-consumer-ksqldb/
                    /// PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
                    /// 
                    /*
                    Topic blacklist, a comma-separated list of regular expressions for matching topic
                    names that should be ignored in broker metadata information as if the topics
                    did not exist. default: '' importance: low 
                    */
                    // TopicBlacklist = 

                    /*
                    Automatically store offset of last message provided to application. The offset
                    store is an in-memory store of the next offset to (auto-)commit for each partition.
                    default: true importance: high 
                    */
                    EnableAutoOffsetStore = false
                };

                var consumer = new ConsumerBuilder<string, byte[]>(config)
                    .SetErrorHandler((consumer, error) =>
                    {
                        _notificationManager.ShowAsync(new NotificationContent
                        {
                            Title = "Error",
                            Message = $"Error occurred, reason: {error.Code.GetReason()}, {error.Reason}",
                            Type = NotificationType.Error
                        }, "WindowArea");
                    })
                    /****
                    .SetPartitionsAssignedHandler((c, partitions) =>
                    {
                        var offsets = partitions.Select(tp => new TopicPartitionOffset(tp, Offset.End));                    
                        return offsets;
                    })
                    */
                    .Build();

                _existingConsumers.Add(environmentNamePlusAutoOffsetResetCompositeKey, consumer);

                return consumer;
            }
            catch (Exception ex)
            {
                await _notificationManager.ShowAsync(new NotificationContent
                {
                    Title = "Error",
                    Message = ex.Message,
                    Type = NotificationType.Error
                }, "WindowArea");

                throw;
            }
        }
    }
}
