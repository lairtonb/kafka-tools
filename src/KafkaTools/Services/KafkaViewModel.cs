using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KafkaTools.Configuration;
using KafkaTools.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Wpf.Core;

namespace KafkaTools.Services
{
    public class KafkaViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<KafkaViewModel> _logger;
        private readonly AppSettings _appSettings;
        private readonly INotificationManager _notificationManager;
        private readonly ILoggerFactory _loggerFactory;

        public KafkaViewModel(ILogger<KafkaViewModel> logger,
            IOptions<AppSettings> options,
            INotificationManager notificationManager,
            ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _appSettings = options.Value;
            _notificationManager = notificationManager;
            _loggerFactory = loggerFactory;

            _environments = new ObservableCollection<EnvironmentInfo>();

            this.LoadEnvironments();
        }

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

        public event PropertyChangedEventHandler? PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
