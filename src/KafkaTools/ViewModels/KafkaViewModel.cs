﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
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
using KafkaTools.Services;

namespace KafkaTools.ViewModels
{
    public class KafkaViewModel : ObservableObject
    {
        private readonly ILogger<KafkaViewModel> _logger;
        private readonly AppSettings _appSettings;
        private readonly INotificationManager _notificationManager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly KafkaService _kafkaService;

        public KafkaViewModel(ILogger<KafkaViewModel> logger,
            IOptions<AppSettings> options,
            INotificationManager notificationManager,
            ILoggerFactory loggerFactory,
            KafkaService kafkaServices)
        {
            _logger = logger;
            _appSettings = options.Value;
            _notificationManager = notificationManager;
            _loggerFactory = loggerFactory;
            _kafkaService = kafkaServices;

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
                        environmentName, _notificationManager, _loggerFactory.CreateLogger(environmentName),
                        userSecretsSettings, _kafkaService, _loggerFactory),
                    KeyVaultEnvironmentSettings keyVaultSettings => new EnvironmentInfo(
                        environmentName, _notificationManager, _loggerFactory.CreateLogger(environmentName),
                        keyVaultSettings, _kafkaService, _loggerFactory),
                    EnvironmentSettings noAuthSettings => new EnvironmentInfo(
                        environmentName, _notificationManager, _loggerFactory.CreateLogger(environmentName),
                        noAuthSettings, _kafkaService, _loggerFactory),
                    _ => throw new NotSupportedException(
                        $"Unsupported environment settings type: {environmentSettings.GetType().Name}")
                };

                _environments.Add(environmentInfo);

                _logger.LogInformation($"Loaded environment: {environmentName}");
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
                _kafkaService.Subscribe(_selectedEnvironment.SelectedTopic.MessagePublished);
                _selectedEnvironment.SelectedTopic.Subscribed = true;
                await _kafkaService.StartConsumingAsync(SelectedEnvironment, _selectedEnvironment.SelectedTopic.TopicName);
            }
            else
            {
                _kafkaService.Unsubscribe(_selectedEnvironment.SelectedTopic.MessagePublished);
                _selectedEnvironment.SelectedTopic.Subscribed = false;

                // TODO implement stop consuming if it is last topic?
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
