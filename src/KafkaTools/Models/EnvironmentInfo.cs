using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using KafkaTools.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notifications.Wpf.Core;

namespace KafkaTools.Models
{
    public class EnvironmentInfo : INotifyPropertyChanged
    {
        private readonly INotificationManager _notificationManager;
        private readonly ILogger _logger;

        public EnvironmentInfo(string environmentName,
            INotificationManager notificationManager,
            ILogger logger,
            EnvironmentSettings environmentSettings)
        {
            EnvironmentName = environmentName;
            _notificationManager = notificationManager;
            _logger = logger;
            EnvironmentSettings = environmentSettings;
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

        public ObservableCollection<TopicInfo> Topics { get; } =
            new ObservableCollection<TopicInfo>();

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
            }
        }

        public void AddTopic(string topicName)
        {
            Topics.Add(new TopicInfo(topicName, _notificationManager, _logger));

            // Is this required?
            // RaisePropertyChanged(nameof(Topics));
        }

        /*
        public void SubscribeToSelectedTopic()
        {
            Task.Run(async () =>
            {
                if (!_selectedTopic.Subscribed)
                {
                    _kafkaServices.Subscribe(_selectedTopic.MessagePublished);
                    _selectedTopic.Subscribed = true;
                    await _kafkaServices.StartConsumingAsync(this, _selectedTopic.TopicName);
                }
                else
                {
                    _kafkaServices.Unsubscribe(_selectedTopic.MessagePublished);
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
