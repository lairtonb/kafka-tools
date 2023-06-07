using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Confluent.Kafka.Admin;
using KafkaTools.Services;
using System.Windows.Threading;
using Notifications.Wpf.Core;
using KafkaTools.Models;
using KafkaTools.Logging;
using Microsoft.Extensions.Logging;
using KafkaTools.Data.DesignTime;
using Microsoft.Extensions.Hosting;
using KafkaTools.Configuration;
using Microsoft.Extensions.Options;
using Serilog.Core;
using KafkaTools.Common;

namespace KafkaTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public string? User { get; private set; } = "G6VYX76LZVCUSRXB";

        public string? Password { get; private set; } = "Yx1DXhdNOZIx/nNTJZ5aF9wQTb/cbcYW43FaVixr/gGd0Ci+AN/y/DYoOecFBlCS";

        private readonly KafkaService _kafkaServices;
        private readonly INotificationManager _notificationManager;
        private readonly ILogger<MainWindow> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ObservableCollection<EnvironmentInfo> _environments;
        private readonly KafkaViewModel _kafkaViewModel;
        private readonly CircularBufferSink _logBufferSink;

        /// <summary>
        /// Used by XAML to bind to the view model.
        /// </summary>
        public MainWindow()
        {
        }

        public MainWindow(KafkaService kafkaServices,
            INotificationManager notificationManager,
            CircularBufferSink logBufferSink,
            ILoggerFactory loggerFactory,
            KafkaViewModel kafkaViewModel)
        {
            InitializeComponent();

            _logBufferSink = logBufferSink;
            _logBufferSink.PropertyChanged += LogBufferSink_PropertyChanged;

            _kafkaServices = kafkaServices;
            _notificationManager = notificationManager;
            _loggerFactory = loggerFactory;
            _kafkaViewModel = kafkaViewModel;

            _logger = _loggerFactory.CreateLogger<MainWindow>();

            // Need to think a little better how to handle this.
            _environments = kafkaViewModel.Environments;
            _selectedEnvironment = kafkaViewModel.Environments[0];

            DataContext = this;
        }

        private async Task Connect()
        {
            try
            {
                Dispatcher.Invoke(() => _selectedEnvironment.Status = ConnectionStatus.Connecting);

                var topics = await _kafkaServices.GetTopicsAsync(_selectedEnvironment.EnvironmentName);

                Dispatcher.Invoke(() =>
                {
                    Topics.Clear();

                    _selectedEnvironment.Status = ConnectionStatus.Connected;
                    foreach (var topic in topics)
                    {
                        var topicLogger = _loggerFactory.CreateLogger(topic);
                        Dispatcher.Invoke(() =>
                        {
                            Topics.Add(new TopicInfo(topic, _notificationManager, topicLogger));
                        });
                    }

                    TopicsCollectionView = CollectionViewSource.GetDefaultView(Topics);
                    TopicsCollectionView.Refresh();
                });
            }
            catch (KafkaException ex) when (ex.Error.IsLocalError)
            {
                Dispatcher.Invoke(() =>
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
                Dispatcher.Invoke(() =>
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

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        public ICommand CopyMessageCommand
        {
            get
            {
                return new AsyncDelegateCommand(CopyMessage, canExecute: () =>
                {
                    return SelectedMessage != null;
                });
            }
        }

        private async Task CopyMessage()
        {
            var selectionStart = textBoxMessage.SelectionStart;
            var selectionLength = textBoxMessage.SelectionLength;

            textBoxMessage.SelectAll();
            textBoxMessage.Copy();

            await _notificationManager.ShowAsync(new NotificationContent
            {
                Title = "Information",
                Message = "Copied",
                Type = NotificationType.Information
            }, "WindowArea", expirationTime: TimeSpan.FromMilliseconds(1200));

            textBoxMessage.SelectionStart = selectionStart;
            textBoxMessage.SelectionLength = selectionLength;
        }

        private async void CopyMessage_Click(object sender, RoutedEventArgs e)
        {
            await Task.CompletedTask;
        }

        private void LogBufferSink_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CircularBufferSink.LogEntries))
            {
                RaisePropertyChanged(nameof(LogEntries));
            }
        }

        //  TODO Hmmm, this needs improvements. A better IoC will help.

        private readonly Lazy<ObservableCollection<LogEntry>> _lazyLogEntries =
            new(() => new ObservableCollection<LogEntry>());

        public IEnumerable<LogEntry> LogEntries
        {
            get
            {
                return _logBufferSink?.LogEntries ?? _lazyLogEntries.Value;
            }
        }

        #region Copied

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

        /****
        private void InitializeEnvironments(INotificationManager notificationManager, ILoggerFactory loggerFactory)
        {
            foreach (var kvp in _appSettings.Environments)
            {
                string environmentName = kvp.Key;
                EnvironmentSettings environmentSettings = kvp.Value;

                EnvironmentInfo environmentInfo = environmentSettings switch
                {
                    UserSecretsEnvironmentSettings userSecretsSettings => new EnvironmentInfo(
                        environmentName,
                        notificationManager,
                        loggerFactory.CreateLogger(environmentName),
                        userSecretsSettings),
                    KeyVaultEnvironmentSettings keyVaultSettings => new EnvironmentInfo(
                        environmentName,
                        notificationManager,
                        loggerFactory.CreateLogger(environmentName),
                        keyVaultSettings),
                    EnvironmentSettings noAuthSettings => new EnvironmentInfo(
                        environmentName,
                        notificationManager,
                        loggerFactory.CreateLogger(environmentName),
                        noAuthSettings),
                    _ => throw new NotSupportedException(
                        $"Unsupported environment settings type: {environmentSettings.GetType().Name}")
                };

                _environments.Add(environmentInfo);
            }

            // Need to think a little better how to handle this.
            _selectedEnvironment = _environments[0];
        }
        */

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

        private ObservableCollection<JsonMessage> _selectedMessages = new();

        public ObservableCollection<JsonMessage> Messages
        {
            get => _selectedMessages;
            set
            {
                _selectedMessages = value;
                RaisePropertyChanged(nameof(Messages));
            }
        }

        private JsonMessage _selectedMessage;

        readonly Regex messageMatcherRegex = new("^[^{]*?(?={)", RegexOptions.Compiled
            | RegexOptions.Singleline);

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

                if (messageMatcherRegex.IsMatch(_selectedMessage?.Value ?? string.Empty))
                {
                    var message = messageMatcherRegex.Replace(_selectedMessage?.Value ?? string.Empty, string.Empty);
                    using var temp = JsonDocument.Parse(message);
                    SelectedMessageText = JsonSerializer.Serialize(temp, new JsonSerializerOptions { WriteIndented = true });
                }

                RaisePropertyChanged(nameof(SelectedMessage));
                RaisePropertyChanged(nameof(CopyMessageCommand));
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

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEnvironment == null)
            {
                e.Handled = true;
                return;
            }

            if (this.SelectedEnvironment.Status != ConnectionStatus.Connected)
            {
                _ = Connect();
            }
            else
            {
                _ = Disconnect();
            }
        }

        private async Task Disconnect()
        {

            await Task.CompletedTask;
        }

        private void SubscribeToTopic_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
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
                    // TODO implement stop consumign if it is last topic?
                }
            });
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
    }


}
