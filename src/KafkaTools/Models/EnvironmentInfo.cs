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
using KafkaTools.ViewModels;

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
            KafkaService kafkaService,
            ILoggerFactory loggerFactory)
        {
            // TODO Guard constructor params: https://docs.microsoft.com/en-us/dotnet/api/system.argumentnullexception?view=net-5.0
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
            get => this.status;
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
                    this.Status is ConnectionStatus.Connected or ConnectionStatus.Disconnected);
            }
        }

        private async Task ConnectCommandHandler()
        {
            // TODO Show some progress indicator
            // TODO Unsubscribe from all messages from previous environment
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
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.Status = ConnectionStatus.Connecting;
                });

                var topics = await _kafkaService.GetTopicsAsync(this.EnvironmentName);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Topics.Clear();

                    this.Status = ConnectionStatus.Connected;
                    foreach (var topic in topics)
                    {
                        var topicLogger = _loggerFactory.CreateLogger(topic);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Topics.Add(new TopicInfo(topic, _notificationManager, topicLogger));
                        });
                    }
                });
            }
            catch (KafkaException ex) when (ex.Error.IsLocalError)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.Status = ConnectionStatus.Disconnected;
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
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.Status = ConnectionStatus.Disconnected;
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
            get => _topics;
            set
            {
                if (_topics != value)
                {
                    _topics = value;
                    RaisePropertyChanged(nameof(Topics));
                }
            }
        }

        private TopicInfo? _selectedTopic = default;

        public TopicInfo? SelectedTopic
        {
            get => _selectedTopic;
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

                // Forcing the CommandManager to raise the RequerySuggested event
                // so that the SubscribeCommand can be reevaluated.
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand SubscribeCommand => new AsyncDelegateCommand(Subscribe, canExecute: () => SelectedTopic != null);

        private Task Subscribe()
        {
            if (SelectedTopic != null)
            {
                if (!SelectedTopic.Subscribed)
                {
                    _kafkaService.Subscribe(SelectedTopic.MessagePublished);
                    SelectedTopic.Subscribed = true;
                    Task.Run(() => _kafkaService.StartConsumingAsync(this, SelectedTopic.TopicName));
                }
                else
                {
                    _kafkaService.Unsubscribe(SelectedTopic.MessagePublished);
                    SelectedTopic.Subscribed = false;
                    Task.Run(() => _kafkaService.StopConsumingAsync(this, SelectedTopic.TopicName));
                }
            }

            return Task.CompletedTask;
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
