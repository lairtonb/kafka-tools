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
using System.Windows.Media.Animation;
using KafkaTools.ViewModels;
using Serilog;

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
        private readonly ObservableCollection<EnvironmentInfo> _environments;
        private readonly KafkaViewModel _kafkaViewModel;
        private readonly ILogger<MainWindow> _logger;
        private readonly CircularBufferSink _logBufferSink;

        public MainWindow(KafkaService kafkaServices,
            INotificationManager notificationManager,
            CircularBufferSink logBufferSink,
            KafkaViewModel kafkaViewModel,
            ILogger<MainWindow> logger)
        {
            InitializeComponent();

            _logBufferSink = logBufferSink;
            _logBufferSink.PropertyChanged += LogBufferSink_PropertyChanged;

            _kafkaServices = kafkaServices;
            _notificationManager = notificationManager;
            _kafkaViewModel = kafkaViewModel;

            _logger = logger;

            // Need to think a little better how to handle this.
            _environments = kafkaViewModel.Environments;
            _selectedEnvironment = kafkaViewModel.Environments[0];

            DataContext = kafkaViewModel;
        }

        public KafkaViewModel KafkaViewModel
        {
            get
            {
                return _kafkaViewModel;
            }
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

        private void SubscribeToTopic_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                if (!_selectedEnvironment.SelectedTopic.Subscribed)
                {
                    _kafkaServices.Subscribe(_selectedEnvironment.SelectedTopic.MessagePublished);
                    _selectedEnvironment.SelectedTopic.Subscribed = true;
                    await _kafkaServices.StartConsumingAsync(_selectedEnvironment, _selectedEnvironment.SelectedTopic.TopicName);
                }
                else
                {
                    _kafkaServices.Unsubscribe(_selectedEnvironment.SelectedTopic.MessagePublished);
                    _selectedEnvironment.SelectedTopic.Subscribed = false;
                    // TODO implement stop consumign if it is last topic?
                }
            });
        }

        #endregion


        private void AutoScrollCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_selectedEnvironment.SelectedTopic != null)
            {
                _selectedEnvironment.SelectedTopic.Messages.CollectionChanged += DataGrid_ScrollChanged;
            }
        }

        private void AutoScrollCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_selectedEnvironment.SelectedTopic != null)
            {
                _selectedEnvironment.SelectedTopic.Messages.CollectionChanged -= DataGrid_ScrollChanged;
            }
        }

        private void DataGrid_ScrollChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (autoScrollCheckBox.IsChecked == true && e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                dataGridMessages.ScrollIntoView(SelectedEnvironment?.SelectedTopic?.Messages?.Last());
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown(0);
        }
    }


}
