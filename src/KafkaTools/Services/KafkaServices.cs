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
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.Memory;
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

    public class KafkaServices
    {
        private readonly IMemoryCache _envCache;
        private readonly INotificationManager _notificationManager;

        private readonly Dispatcher _dispatcher;

        public object JsonConvert { get; private set; }

        public KafkaServices(IMemoryCache envCache, INotificationManager notificationManager, System.Windows.Threading.Dispatcher? dispatcher = default)
        {
            _envCache = envCache;
            _notificationManager = notificationManager;

            dispatcher ??= Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            _dispatcher = dispatcher;
        }

        private event EventHandler<MessageEventArgs> MessagePublished;

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

            await _notificationManager.ShowAsync(new NotificationContent
            {
                Title = "Information",
                Message = "Getting topics from server.",
                Type = NotificationType.Information
            }, "WindowArea");

            var adminConfig = new AdminClientConfig()
            {
                BootstrapServers = envSettings.BrokerUrl,
            };

            if (!string.IsNullOrEmpty(envSettings.Key) && !string.IsNullOrEmpty(envSettings.Secret))
            {
                adminConfig.SaslMechanism = SaslMechanism.Plain;
                adminConfig.SecurityProtocol = SecurityProtocol.SaslSsl;
                adminConfig.SaslUsername = envSettings.Key;
                adminConfig.SaslPassword = envSettings.Secret;
            };

            var topics = new List<string>();

            await Task.Run(() =>
            {
                using (var adminClient = new AdminClientBuilder(adminConfig).Build())
                {
                    var metadata = adminClient.GetMetadata(timeout: TimeSpan.FromSeconds(60));
                    var topicNames = metadata.Topics
                        .Where(t => !t.Topic.StartsWith("_")
                            && !t.Topic.StartsWith("docker-")
                            && !t.Topic.StartsWith("default_ksql_processing_log"))
                        .OrderBy(t => t.Topic)
                        .Select(t => t.Topic)
                        .ToList();
                    topics.AddRange(topicNames);
                }
            });

            return topics;
        }

        public Dictionary<string, string> currentTopics = new();

        public async Task StartConsumingAsync(string selectedSourceEnvironment, string selectedTopic)
        {
            var consumer = await GetConsumerAsync(selectedSourceEnvironment);

            try
            {
                if (!currentTopics.ContainsKey(selectedTopic))
                {
                    currentTopics.Add(selectedTopic, selectedTopic);
                    consumer.Subscribe(currentTopics.Select(t => t.Value));
                }

                CancellationTokenSource cts = new CancellationTokenSource();

                // Why should we want this?
                // cts.CancelAfter(TimeSpan.FromSeconds(60));

                while (true)
                {
                    var consumeResult = consumer.Consume(cts.Token);

                    if (consumeResult == null)
                    {
                        throw new KafkaServicesException("Cabol!");
                    }

                    if (consumeResult.IsPartitionEOF)
                    {
                        // Notify loaded all messages
                        continue;
                    }

                    /***
                    var message = Regex.Replace(_selectedMessage.Value, @"^[^{]*?(?={)", string.Empty,
                        RegexOptions.Multiline | RegexOptions.CultureInvariant);

                    var temp = JsonConvert.DeserializeObject(consumeResult.Message.Value);
                    SelectedMessageText = JsonConvert.SerializeObject(temp, Formatting.Indented);
                    */

                    using var stream = new MemoryStream(consumeResult.Message.Value);
                    using var reader = new BinaryReader(stream);

                    /***
                    // Should just ignore first 5 bytes?                
                    var magicByte = reader.ReadByte();
                    if (magicByte != ConfluentConstants.MagicByte)
                    {
                        throw new InvalidDataException($"Expecting data with Confluent Schema Registry framing. Magic byte was {consumeResult.Message.Value[0]}, expecting {ConfluentConstants.MagicByte}");
                    }

                    var writerId = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    */

                    var streamReader = new StreamReader(stream);
                    var valueString = streamReader.ReadToEnd();

                    var message = new JsonMessage();
                    message.Headers = new Dictionary<string, byte[]>();
                    foreach (var header in consumeResult.Message.Headers)
                    {
                        message.Headers.Add(header.Key, header.GetValueBytes());
                    }
                    message.Key = consumeResult.Message.Key;
                    message.Value = valueString;
                    message.Timestamp = consumeResult.Message.Timestamp;

                    if (cts.Token.IsCancellationRequested)
                        break;

                    MessagePublished?.Invoke(this, new MessageEventArgs(consumeResult.Topic,
                        consumeResult.Offset, message));
                }
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

        private async Task<EnvSettings> GetEnvSettingsAsync(string environment)
        {
            var cachedEnvSettings = _envCache.Get(environment);
            if (cachedEnvSettings != null)
            {
                return (EnvSettings)cachedEnvSettings;
            }

            var brokerUrl = environment switch
            {
                "Development" => "localhost:9092",
                "CI" => "pkc-57q5g.westeurope.azure.confluent.cloud:9092",
                "Test" => "pkc-57q5g.westeurope.azure.confluent.cloud:9092",
                "Staging" => "pkc-57q5g.westeurope.azure.confluent.cloud:9092",
                "Production" => "pkc-57q5g.westeurope.azure.confluent.cloud:9092",
                _ => throw new ArgumentException("Invalid environment", nameof(environment))
            };

            var (key, secret) = await GetKeyAndSecretAsync(environment);

            var envSettings = new EnvSettings()
            {
                Key = key,
                Secret = secret,
                BrokerUrl = brokerUrl
            };

            _envCache.Set(environment, envSettings,
                new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) }
            );

            return envSettings;
        }

        private async Task<(string key, string secret)> GetKeyAndSecretAsync(string environment)
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

            var kvUri = environment switch
            {
                "Development" => null,
                "CI" => new Uri($"https://esw-tooling-ci-we.vault.azure.net"),
                "Test" => new Uri($"https://esw-tooling-test.vault.azure.net"),
                "Prep" => new Uri($"https://esw-tooling-sand.vault.azure.net"),
                "Sand" => new Uri($"https://esw-tooling-sand.vault.azure.net"),
                _ => throw new ArgumentException("Invalid environment", nameof(environment))
            };

            var key = string.Empty;
            var secret = string.Empty;

            if (environment != "Development")
            {
                var identifier = Guid.NewGuid();

                await _notificationManager.ShowAsync(identifier, new NotificationContent
                {
                    Title = "Information",
                    Message = "Getting credentials from KeyVault...",
                    Type = NotificationType.Information
                }, "WindowArea", expirationTime: TimeSpan.MaxValue);

                try
                {
                    var kvClient = new SecretClient(kvUri, new DefaultAzureCredential(), options);
                    var kvSecret = (await kvClient.GetSecretAsync("cm--kafka-key--tooling")).Value;
                    key = kvSecret.Value;

                    kvSecret = (await kvClient.GetSecretAsync("cm--kafka-secret--tooling")).Value;
                    secret = kvSecret.Value;
                }
                finally
                {
                    if (!_dispatcher.CheckAccess())
                    {
                        await _dispatcher.BeginInvoke((Action)async delegate
                        {
                            await _notificationManager.CloseAsync(identifier);
                        });
                    }
                }

                await _notificationManager.ShowAsync(new NotificationContent
                {
                    Title = "Success",
                    Message = "Done getting credentials from KeyVault.",
                    Type = NotificationType.Success
                }, "WindowArea");
            }
            return (key, secret);
        }

        public Dictionary<string, IConsumer<string, byte[]>> consumers = new();

        private async Task<IConsumer<string, byte[]>> GetConsumerAsync(string selectedSourceEnvironment)
        {
            if (consumers.ContainsKey(selectedSourceEnvironment))
            {
                return consumers[selectedSourceEnvironment];
            }

            try
            {
                var envSettings = await GetEnvSettingsAsync(selectedSourceEnvironment!);

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
                    -  For COnfluent, the group.id may have some rules set up in the broker,
                       like starting with env name, like this example:
                       GroupId = $"ci-tooling-webhookeventadapter-worker-lairton-local",
                    - Not sure about if other brokers have similar rules.
                    */
                    GroupId = $"{selectedSourceEnvironment.ToLowerInvariant()}-tooling-messageviewer-worker-local",

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
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    //// AutoOffsetReset = AutoOffsetReset.Latest;

                    // EnablePartitionEof = true,

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
                    }
                    )
                    /****
                    .SetPartitionsAssignedHandler((c, partitions) =>
                    {
                        var offsets = partitions.Select(tp => new TopicPartitionOffset(tp, Offset.End));                    
                        return offsets;
                    })
                    */
                    .Build();

                consumers.Add(selectedSourceEnvironment, consumer);

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
