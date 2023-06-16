using System;
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
using System.Windows.Controls;
using static KafkaTools.ViewModels.MyCommand;

namespace KafkaTools.ViewModels
{
    public delegate bool CanExecute(TextBox textBox);
    public delegate Task Execute(TextBox textBox);

    public class MyCommand : ICommand
    {
        private readonly CanExecute _canExecute;
        private readonly Execute _execute;

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public MyCommand(CanExecute canExecute, Execute execute)
        {
            _canExecute = canExecute;
            _execute = execute;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter is not TextBox textBox)
            {
                return false;
            }

            // Implement your logic to determine if the command can execute
            return _canExecute(textBox);
        }

        public void Execute(object? parameter)
        {
            if (parameter is TextBox textBox)
            {
                _execute(textBox);
            }
        }
    }

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

        public void LoadEnvironments()
        {
            _environments = new ObservableCollection<EnvironmentInfo>();
            foreach (var kvp in _appSettings.Environments)
            {
                var environmentName = kvp.Key;
                var environmentSettings = kvp.Value;

                var environmentInfo = environmentSettings switch
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

        public virtual ObservableCollection<EnvironmentInfo> Environments => _environments;

        private EnvironmentInfo? _selectedEnvironment;

        public EnvironmentInfo? SelectedEnvironment
        {
            get => _selectedEnvironment;
            set
            {
                if (value == _selectedEnvironment)
                    return;

                _selectedEnvironment = value;

                RaisePropertyChanged(nameof(SelectedEnvironment));
            }
        }
    }
}
