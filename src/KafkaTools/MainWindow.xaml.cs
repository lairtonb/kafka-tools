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

namespace KafkaTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public string? User { get; private set; } = "G6VYX76LZVCUSRXB";

        public string? Password { get; private set; } = "Yx1DXhdNOZIx/nNTJZ5aF9wQTb/cbcYW43FaVixr/gGd0Ci+AN/y/DYoOecFBlCS";

        private string _selectedBatchExportMessageText = "";

        private readonly IConfiguration _config;
        private readonly KafkaServices _kafkaServices;
        private readonly INotificationManager _notificationManager;

        /// <summary>
        /// Used by XAML to bind to the view model.
        /// </summary>
        public MainWindow()
        {
        }

        public MainWindow(IConfiguration config, KafkaServices kafkaServices, INotificationManager notificationManager)
        {
            InitializeComponent();

            _config = config;
            _kafkaServices = kafkaServices;
            _notificationManager = notificationManager;

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

        private readonly ObservableCollection<string> _environments = new(
                new string[] {
                    "(Not Specified)",
                    "Development",
                    "CI",
                    "Test",
                    "Prep",
                    "Sand",
                    "Production"
                }
            );

        public virtual ObservableCollection<string> Environments
        {
            get
            {
                return _environments;
            }
        }

        private string _selectedEnvironment = string.Empty;

        public string SelectedEnvironment
        {
            get { return _selectedEnvironment; }
            set
            {
                if (value == _selectedEnvironment
                    || string.IsNullOrEmpty(value)) return;

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
            Messages = SelectedTopic.Messages;
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

        private void EnvironmentsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedEnvironment == "(Not Specified)" || string.IsNullOrEmpty(_selectedEnvironment))
            {
                e.Handled = true;
                return;
            }

            // TODO Show some progress indicator
            // TODO Unsubscribe from all messagens from previous environment
            // TODO Maybe use another visual representation in the future,
            //      like Guilherme did in Aptakube

            var identifier = Guid.NewGuid();

            Task.Run(async () =>
            {
                try
                {
                    var topics = await _kafkaServices.GetTopicsAsync(_selectedEnvironment);
                    Dispatcher.Invoke(() =>
                    {
                        Topics.Clear();
                        foreach (var topic in topics)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                Topics.Add(new TopicInfo(topic));
                            });
                        }
                    });
                }
                catch (KafkaException ex) when (ex.Error.IsLocalError)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _notificationManager.CloseAsync(identifier);
                        _notificationManager.ShowAsync(new NotificationContent
                        {
                            Title = "Error",
                            Message = ex.Message,
                            Type = NotificationType.Error
                        }, "WindowArea");
                        Topics.Clear();
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _notificationManager.CloseAsync(identifier);
                        _notificationManager.ShowAsync(new NotificationContent
                        {
                            Title = "Error",
                            Message = ex.Message,
                            Type = NotificationType.Error
                        }, "WindowArea");
                        Topics.Clear();
                    });
                }
            });
        }

        private void DataGridMessages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Method intentionally left empty.
        }

        private void SubscribeToTopic_Click(object sender, RoutedEventArgs e)
        {
            var identifier = Guid.NewGuid();

            _notificationManager.ShowAsync(identifier, new NotificationContent
            {
                Title = "Information",
                Message = "Waiting for messages...",
                Type = NotificationType.Information
            }, "WindowArea", expirationTime: TimeSpan.MaxValue);

            Task.Run(async () =>
            {
                /***
                bool isFirstItem = true;
                
                _kafkaServices.MessagePublished += (object? sender, MessageEventArgs e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (isFirstItem)
                        {
                            Messages.Clear();
                            _notificationManager.CloseAsync(identifier);
                            _notificationManager.ShowAsync(new NotificationContent
                            {
                                Title = "Succes",
                                Message = "Started receiving messages from Kafka",
                                Type = NotificationType.Success
                            }, "WindowArea");
                            isFirstItem = false;
                        }
                        Messages.Add(e.Message);
                    });
                };
                */

                _kafkaServices.Subscribe(SelectedTopic.MessagePublished);

                await _kafkaServices.StartConsumingAsync(SelectedEnvironment, SelectedTopic.TopicName);
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
    }

    public class DesignTimeDataContext
    {
        public ObservableCollection<JsonMessage> DesignTimeMessages { get; set; } =
            new ObservableCollection<JsonMessage>()
            {
                new JsonMessage()
                {
                    Key = "Sample Key",
                    Timestamp = new Timestamp(DateTime.Now),
                }
            };
    }
}
