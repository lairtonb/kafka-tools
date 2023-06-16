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

            // I still need to think better about this.
            DataGridLogs.DataContext = _lazyLogEntries;
        }

        #region Environment Selection

        private EnvironmentInfo _selectedEnvironment;

        public EnvironmentInfo SelectedEnvironment
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

        #endregion

        #region Topic Filtering UI

        private string _topicNameFilter;

        public string TopicNameFilter
        {
            get => _topicNameFilter;
            set
            {
                if (_topicNameFilter != value)
                {
                    _topicNameFilter = value;
                    RaisePropertyChanged(nameof(TopicNameFilter));
                    ApplyFilter();
                }
            }
        }

        private void ApplyFilter()
        {
            if (EnvironmentGrid.FindResource("TopicsCollectionViewSource") is CollectionViewSource topicsCollectionViewSource)
            {
                topicsCollectionViewSource.Filter += (object _, FilterEventArgs e) =>
                {
                    if (e.Item is not TopicInfo item)
                    {
                        e.Accepted = false;
                        return;
                    }

                    e.Accepted = item.TopicName.Contains(TopicNameFilter);
                };
                topicsCollectionViewSource.View.Refresh();
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Logging

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

        public IEnumerable<LogEntry> LogEntries => _logBufferSink?.LogEntries ?? _lazyLogEntries.Value;

        #endregion

        #region Copied

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

        #endregion

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown(0);
        }
    }


}
