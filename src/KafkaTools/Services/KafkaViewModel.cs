using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using KafkaTools.Abstractions;
using System.Windows.Threading;
using KafkaTools.Common;
using KafkaTools.Configuration;
using KafkaTools.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Wpf.Core;
using System.Windows.Input;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Windows;

namespace KafkaTools.Services
{
    public class KafkaViewModel : ObservableObject
    {
        private readonly ILogger<KafkaViewModel> _logger;
        private readonly AppSettings _appSettings;
        private readonly INotificationManager _notificationManager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly KafkaServices _kafkaServices;

        public KafkaViewModel(ILogger<KafkaViewModel> logger,
            IOptions<AppSettings> options,
            INotificationManager notificationManager,
            ILoggerFactory loggerFactory,
            KafkaServices kafkaServices)
        {
            _logger = logger;
            _appSettings = options.Value;
            _notificationManager = notificationManager;
            _loggerFactory = loggerFactory;
            _kafkaServices = kafkaServices;

            _environments = new ObservableCollection<EnvironmentInfo>();

            this.LoadEnvironments();
        }

        #region Environments

        public void LoadEnvironments()
        {
            _environments = new ObservableCollection<EnvironmentInfo>();
            foreach (var kvp in _appSettings.Environments)
            {
                string environmentName = kvp.Key;
                EnvironmentSettings environmentSettings = kvp.Value;

                EnvironmentInfo environmentInfo = environmentSettings switch
                {
                    UserSecretsEnvironmentSettings userSecretsSettings => new EnvironmentInfo(
                        environmentName,
                        _notificationManager,
                        _loggerFactory.CreateLogger(environmentName),
                        userSecretsSettings),
                    KeyVaultEnvironmentSettings keyVaultSettings => new EnvironmentInfo(
                        environmentName,
                        _notificationManager,
                        _loggerFactory.CreateLogger(environmentName),
                        keyVaultSettings),
                    EnvironmentSettings noAuthSettings => new EnvironmentInfo(
                        environmentName,
                        _notificationManager,
                        _loggerFactory.CreateLogger(environmentName),
                        noAuthSettings),
                    _ => throw new NotSupportedException(
                        $"Unsupported environment settings type: {environmentSettings.GetType().Name}")
                };

                _environments.Add(environmentInfo);

                _logger.LogInformation($"Loaded environment: {environmentName}");
            }
        }

        public void ConnectToSelectedEnvironment()
        {
            if (_selectedEnvironment == null)
            {
                return;
            }
            Task.Run(() => Connect());
        }

        public ICommand ConnectToSelectedEnvironmentCommand
        {
            get
            {
                return new AsyncDelegateCommand(ConnectCommandHandler, canExecute: () =>
                {
                    return _selectedEnvironment != null && (
                            _selectedEnvironment.Status == ConnectionStatus.Connected ||
                            _selectedEnvironment.Status == ConnectionStatus.Disconnected
                        );
                });
            }
        }

        private async Task ConnectCommandHandler()
        {
            // TODO Show some progress indicator
            // TODO Unsubscribe from all messagens from previous environment
            // TODO Maybe use another visual representation in the future,
            //      like Guilherme did in Aptakube

            switch (this.SelectedEnvironment.Status)
            {
                case ConnectionStatus.Disconnected:
                    await Connect();
                    break;
                case ConnectionStatus.Connecting:
                    break;
                case ConnectionStatus.Connected:
                    await Disconnect();
                    break;
                default:
                    break;
            }
        }

        private async Task Disconnect()
        {
            // TODO implement disconnect logic
            await Task.CompletedTask;
        }

        private async Task Connect()
        {
            try
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    _selectedEnvironment.Status = ConnectionStatus.Connecting;
                });

                var topics = await _kafkaServices.GetTopicsAsync(_selectedEnvironment.EnvironmentName);

                App.Current.Dispatcher.Invoke(() =>
                {
                    Topics.Clear();

                    _selectedEnvironment.Status = ConnectionStatus.Connected;
                    foreach (var topic in topics)
                    {
                        var topicLogger = _loggerFactory.CreateLogger(topic);
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Topics.Add(new TopicInfo(topic, _notificationManager, topicLogger));
                        });
                    }

                    // TODO check this code is still needed
                    // TopicsCollectionView = CollectionViewSource.GetDefaultView(Topics);
                    // TopicsCollectionView.Refresh();
                });
            }
            catch (KafkaException ex) when (ex.Error.IsLocalError)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    _logger.LogError(ex, ex.Message);
                    _notificationManager.CloseAllAsync();
                    _notificationManager.ShowAsync(new NotificationContent
                    {
                        Title = "Error",
                        Message = ex.Message,
                        Type = NotificationType.Error
                    }, "WindowArea", TimeSpan.FromSeconds(10));
                    Topics.Clear();
                });
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    _logger.LogError(ex, ex.Message);
                    _notificationManager.CloseAllAsync();
                    _notificationManager.ShowAsync(new NotificationContent
                    {
                        Title = "Error",
                        Message = ex.Message,
                        Type = NotificationType.Error
                    }, "WindowArea", TimeSpan.FromSeconds(10));
                    Topics.Clear();
                });
            }

        }

        private ObservableCollection<EnvironmentInfo> _environments;

        public virtual ObservableCollection<EnvironmentInfo> Environments
        {
            get
            {
                return _environments;
            }
        }

        private EnvironmentInfo _selectedEnvironment;

        public EnvironmentInfo SelectedEnvironment
        {
            get { return _selectedEnvironment; }
            set
            {
                if (value == _selectedEnvironment)
                    return;

                _selectedEnvironment = value;

                RaisePropertyChanged(nameof(SelectedEnvironment));
            }
        }

        #endregion

        #region Topics

        public ICommand SubscribeToSelectedTopicCommand
        {
            get
            {
                return new AsyncDelegateCommand(SubscribeToTopic, canExecute: () =>
                {
                    return _selectedEnvironment != null &&
                        _selectedEnvironment.SelectedTopic != null;
                });
            }
        }

        private async Task SubscribeToTopic()
        {
            if (!_selectedEnvironment.SelectedTopic.Subscribed)
            {
                _kafkaServices.Subscribe(_selectedEnvironment.SelectedTopic.MessagePublished);
                _selectedEnvironment.SelectedTopic.Subscribed = true;
                await _kafkaServices.StartConsumingAsync(SelectedEnvironment, _selectedEnvironment.SelectedTopic.TopicName);
            }
            else
            {
                _kafkaServices.Unsubscribe(_selectedEnvironment.SelectedTopic.MessagePublished);
                _selectedEnvironment.SelectedTopic.Subscribed = false;

                // TODO implement stop consuming if it is last topic?
            }
        }

        private ObservableCollection<TopicInfo> _topics = new();

        public ObservableCollection<TopicInfo> Topics
        {
            get { return _topics; }
            set
            {
                if (_topics != value)
                {
                    _topics = value;
                    RaisePropertyChanged(nameof(Topics));
                }
            }
        }

        private ICollectionView topicsCollectionView;

        public ICollectionView TopicsCollectionView
        {
            get { return topicsCollectionView; }
            set
            {
                if (topicsCollectionView != value)
                {
                    topicsCollectionView = value;
                    RaisePropertyChanged(nameof(TopicsCollectionView));
                }
            }
        }

        private string topicNameFilter;

        public string TopicNameFilter
        {
            get { return topicNameFilter; }
            set
            {
                if (topicNameFilter != value)
                {
                    topicNameFilter = value;
                    RaisePropertyChanged(nameof(TopicNameFilter));
                    ApplyFilter();
                }
            }
        }

        private void ApplyFilter()
        {
            TopicsCollectionView.Filter = item => string.IsNullOrEmpty(TopicNameFilter) || ((TopicInfo)item).TopicName.Contains(TopicNameFilter);
            TopicsCollectionView.Refresh();
        }

        private TopicInfo _selectedTopic;

        public TopicInfo SelectedTopic
        {
            get { return _selectedTopic; }
            set
            {
                _selectedTopic = value;
                RaisePropertyChanged(nameof(SelectedTopic));

                // TODO investigate if this is the better option
                Messages = _selectedEnvironment.SelectedTopic?.Messages
                    ?? new ObservableCollection<JsonMessage>();
            }
        }

        #endregion

        #region Messages

        private ObservableCollection<JsonMessage> _messagesOfSelectedTopic = new();

        public ObservableCollection<JsonMessage> Messages
        {
            get => _messagesOfSelectedTopic;
            set
            {
                _messagesOfSelectedTopic = value;
                RaisePropertyChanged(nameof(Messages));
            }
        }

        private JsonMessage _selectedMessage;

        public JsonMessage SelectedMessage
        {
            get { return _selectedMessage; }
            set
            {
                if (_selectedMessage == value)
                {
                    return;
                }

                _selectedMessage = value;
                RaisePropertyChanged(nameof(SelectedMessage));

                // Reformat the Json message for better visualization
                using var parsedMessage = JsonDocument.Parse(_selectedMessage.Value);
                // TODO try passing the message directly, see if it works
                // SelectedMessageText = JsonSerializer.Serialize(_selectedMessage.Value,
                SelectedMessageText = JsonSerializer.Serialize(parsedMessage,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
            }
        }

        private string _selectedMessageText;

        public string SelectedMessageText
        {
            get { return _selectedMessageText; }
            set
            {
                if (_selectedMessageText == value)
                {
                    return;
                }
                _selectedMessageText = value;

                _selectedMessage.Value = _selectedMessageText;

                RaisePropertyChanged(nameof(SelectedMessageText));
            }
        }

        #endregion
    }
}
