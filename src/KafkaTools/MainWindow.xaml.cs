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
using KafkaTools.Abstractions;
using System.Windows.Threading;
using Notifications.Wpf.Core;
using KafkaTools.Models;
using KafkaTools.Logging;
using Microsoft.Extensions.Logging;
using KafkaTools.Data.DesignTime;
using Microsoft.Extensions.Hosting;

namespace KafkaTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public string? User { get; private set; } = "G6VYX76LZVCUSRXB";

        public string? Password { get; private set; } = "Yx1DXhdNOZIx/nNTJZ5aF9wQTb/cbcYW43FaVixr/gGd0Ci+AN/y/DYoOecFBlCS";

        // private readonly IConfiguration _config;

        private readonly KafkaServices _kafkaServices;
        private readonly INotificationManager _notificationManager;
        private readonly ILogger<MainWindow> _logger;
        private readonly ILoggerFactory _loggerFactory;

        private static CircularBufferSink _logBufferSink;

        /// <summary>
        /// Used by XAML to bind to the view model.
        /// </summary>
        public MainWindow()
        {
        }

        public MainWindow(// IConfiguration config,
            KafkaServices kafkaServices,
            INotificationManager notificationManager,
            CircularBufferSink logBufferSink,
            ILoggerFactory loggerFactory
            )
        {
            _logBufferSink = logBufferSink;
            _logBufferSink.PropertyChanged += LogBufferSink_PropertyChanged; ;

            InitializeComponent();

            // _config = config;
            _kafkaServices = kafkaServices;
            _notificationManager = notificationManager;

            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<MainWindow>();

            DataContext = this;

            // Development
            /*
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddUserSecrets(userSecretsId: "301fdd9d-69f8-4441-90f8-7d83ddccf23d")
                .Build();
            */
        }

        /*
         * Data Binding / Bound Properties
         */

        private readonly ObservableCollection<EnvironmentInfo> _environments = new(
                new EnvironmentInfo[] {
                    new EnvironmentInfo { EnvironmentName = "Development" },
                    new EnvironmentInfo { EnvironmentName = "CI" },
                    new EnvironmentInfo { EnvironmentName = "Test" },
                    new EnvironmentInfo { EnvironmentName = "Prep" },
                    new EnvironmentInfo { EnvironmentName = "Sand" },
                    new EnvironmentInfo { EnvironmentName = "Production" }
                }
            );

        public virtual ObservableCollection<EnvironmentInfo> Environments
        {
            get
            {
                return _environments;
            }
        }

        private EnvironmentInfo? _selectedEnvironment = null;

        public EnvironmentInfo? SelectedEnvironment
        {
            get { return _selectedEnvironment; }
            set
            {
                if (value == _selectedEnvironment || _selectedEnvironment == null)
                    return;

                _selectedEnvironment = value;

                RaisePropertyChanged(nameof(SelectedEnvironment));
            }
        }

        public ObservableCollection<TopicInfo> Topics { get; set; } =
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

        private void TopicsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Messages = SelectedTopic?.Messages ?? new ObservableCollection<JsonMessage>();
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
            }
        }

        private string _selectedMessageText = "Teste";

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

        internal static class ConfluentConstants
        {
            public const byte MagicByte = 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEnvironment == null)
            {
                e.Handled = true;
                return;
            }

            // TODO Show some progress indicator
            // TODO Unsubscribe from all messagens from previous environment
            // TODO Maybe use another visual representation in the future,
            //      like Guilherme did in Aptakube
            await Task.Run(async () =>
            {
                try
                {
                    var topics = await _kafkaServices.GetTopicsAsync(_selectedEnvironment.EnvironmentName);
                    Dispatcher.Invoke(() =>
                    {
                        Topics.Clear();
                        foreach (var topic in topics)
                        {
                            var topicLogger = _loggerFactory.CreateLogger(topic);
                            Dispatcher.Invoke(() =>
                            {
                                Topics.Add(new TopicInfo(topic, _notificationManager, topicLogger));
                            });
                        }
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
            });

        }

        private async void EnvironmentsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void DataGridMessages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Method intentionally left empty.
        }

        private void SubscribeToTopic_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                if (!SelectedTopic.Subscribed)
                {
                    _kafkaServices.Subscribe(SelectedTopic.MessagePublished);
                    SelectedTopic.Subscribed = true;
                    await _kafkaServices.StartConsumingAsync(SelectedEnvironment, SelectedTopic.TopicName);
                }
                else
                {
                    _kafkaServices.Unsubscribe(SelectedTopic.MessagePublished);
                    SelectedTopic.Subscribed = false;
                    // TODO implement stop consumign if it is last topic?
                }
            });
        }

        private void PublishTo_Click(object sender, RoutedEventArgs e)
        {
            // Method intentionally left empty.
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private async void CopyMessage_Click(object sender, RoutedEventArgs e)
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

        private void LogBufferSink_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CircularBufferSink.LogEntries))
            {
                RaisePropertyChanged(nameof(LogEntries));
            }
        }

        public IEnumerable<LogEntry> LogEntries
        {
            get
            {
                return _logBufferSink.LogEntries;
            }
        }


    }


}
