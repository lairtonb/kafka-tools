using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using KafkaTools.Common;
using System.Windows.Input;
using KafkaTools.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notifications.Wpf.Core;
using Confluent.Kafka;
using KafkaTools.Services;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace KafkaTools.Models
{
    public class EnvironmentInfo : INotifyPropertyChanged
    {
        private readonly INotificationManager _notificationManager;
        private readonly ILogger _logger;
        private readonly KafkaService _kafkaService;
        private readonly ILoggerFactory _loggerFactory;

        public EnvironmentInfo(string environmentName,
            INotificationManager notificationManager,
            ILogger logger,
            EnvironmentSettings environmentSettings,
            KafkaService? kafkaService,
            ILoggerFactory loggerFactory)
        {
            EnvironmentName = environmentName;
            _notificationManager = notificationManager;
            _logger = logger;
            EnvironmentSettings = environmentSettings;
            _kafkaService = kafkaService;
            _loggerFactory = loggerFactory;
        }

        public EnvironmentSettings EnvironmentSettings { get; private set; }


        public string EnvironmentName { get; private set; }

        private ConnectionStatus status = ConnectionStatus.Disconnected;

        public ConnectionStatus Status
        {
            get
            {
                return this.status;
            }
            internal set
            {
                if (this.status != value)
                {
                    this.status = value;
                    RaisePropertyChanged(nameof(Status));
                }
            }
        }

        public ICommand ConnectToSelectedEnvironmentCommand
        {
            get
            {
                return new AsyncDelegateCommand(ConnectCommandHandler, canExecute: () =>
                {
                    return this.Status == ConnectionStatus.Connected
                        || this.Status == ConnectionStatus.Disconnected;
                });
            }
        }

        private async Task ConnectCommandHandler()
        {
            // TODO Show some progress indicator
            // TODO Unsubscribe from all messagens from previous environment
            // TODO Maybe use another visual representation in the future,
            //      like Guilherme did in Aptakube

            switch (this.Status)
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

        private async Task Connect()
        {
            try
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    this.Status = ConnectionStatus.Connecting;
                });

                await Task.CompletedTask;

                var topics = await _kafkaService.GetTopicsAsync(this.EnvironmentName);

                App.Current.Dispatcher.Invoke(() =>
                {
                    Topics.Clear();

                    this.Status = ConnectionStatus.Connected;
                    foreach (var topic in topics)
                    {
                        var topicLogger = _loggerFactory.CreateLogger(topic);
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Topics.Add(new TopicInfo(topic, _notificationManager, topicLogger));
                        });
                    }

                    // Retrieves the default view associated with the Topics collection.
                    // The default view is an object that provides functionalities for sorting,
                    // filtering, and grouping data in a collection.
                    // We currenty use this to filter the topics by name.
                    TopicsCollectionView = CollectionViewSource.GetDefaultView(Topics);

                    // TODO is this required here?
                    // Updates the view based on any changes that might have
                    // occurred in the underlying Topics collection.
                    TopicsCollectionView.Refresh();
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

        /*
        public void AddTopic(string topicName)
        {
            Topics.Add(new TopicInfo(topicName, _notificationManager, _logger));

            // Is this required?
            // RaisePropertyChanged(nameof(Topics));
        }
        */

        private async Task Disconnect()
        {
            // TODO implement disconnect logic
            await Task.CompletedTask;
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
            TopicsCollectionView.Filter = (item) =>
            {
                return string.IsNullOrEmpty(TopicNameFilter)
                    || ((TopicInfo)item).TopicName.Contains(TopicNameFilter);
            };
            TopicsCollectionView.Refresh();
        }

        private TopicInfo? _selectedTopic = default;

        public TopicInfo SelectedTopic
        {
            get { return _selectedTopic; }
            set
            {
                if (_selectedTopic == value)
                {
                    return;
                }
                _selectedTopic = value;

                RaisePropertyChanged(nameof(SelectedTopic));

                // TODO investigate if this is the better option
                // Messages = _selectedEnvironment.SelectedTopic?.Messages
                //    ?? new ObservableCollection<JsonMessage>();
            }
        }

        /*
        public void SubscribeToSelectedTopic()
        {
            Task.Run(async () =>
            {
                if (!_selectedTopic.Subscribed)
                {
                    _kafkaService.Subscribe(_selectedTopic.MessagePublished);
                    _selectedTopic.Subscribed = true;
                    await _kafkaService.StartConsumingAsync(this, _selectedTopic.TopicName);
                }
                else
                {
                    _kafkaService.Unsubscribe(_selectedTopic.MessagePublished);
                    _selectedTopic.Subscribed = false;
                    // TODO implement stop consumign if it is last topic?
                }
            });
        }
        */

        public event PropertyChangedEventHandler? PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
