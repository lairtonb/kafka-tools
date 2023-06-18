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

        private ConnectionStatus _status = ConnectionStatus.Disconnected;

        public ConnectionStatus Status
        {
            get => this._status;
            internal set
            {
                if (this._status != value)
                {
                    this._status = value;
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
                            Topics.Add(new TopicInfo(
                                this.EnvironmentName,
                                topic,
                                _notificationManager,
                                topicLogger,
                                _kafkaService)
                            );
                        });
                    }
                });
            }
            catch (KafkaException ex) when (ex.Error.IsLocalError)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.Status = ConnectionStatus.Disconnected;
                    _logger.LogError(ex, "An error occurred connecting to Kafka.");
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
                    _logger.LogError(ex, "An error occurred connecting to Kafka.");
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

                // Forcing the CommandManager to raise the RequerySuggested event
                // so that the SubscribeCommand can be reevaluated.
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
