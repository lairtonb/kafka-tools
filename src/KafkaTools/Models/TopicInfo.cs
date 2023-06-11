﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using KafkaTools.Services;
using Microsoft.Extensions.Logging;
using Notifications.Wpf.Core;

namespace KafkaTools.Models
{
    public class InsertAtTopObservableCollection<T> : ObservableCollection<T>
    {
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(0, item);
        }
    }

    public class TopicInfo : INotifyPropertyChanged
    {
        private readonly INotificationManager _notificationManager;
        private readonly ILogger _logger;

        public TopicInfo(string topicName,
            INotificationManager notificationManager,
            ILogger logger)
        {
            TopicName = topicName;
            _notificationManager = notificationManager;
            _logger = logger;
        }

        public string TopicName { get; private set; }

        public long Offset { get; private set; }

        public InsertAtTopObservableCollection<JsonMessage> Messages { get; } =
            new InsertAtTopObservableCollection<JsonMessage>();


        public EventHandler<MessageEventArgs> MessagePublished
        {
            get
            {
                return OnMessagePublished;
            }
        }

        private bool isFirstMessageFlag = true;

        private void OnMessagePublished(object? sender, MessageEventArgs e)
        {
            if (e.TopicName == TopicName && e.Offset > Offset)
            {
                Application.Current?.Dispatcher.Invoke(
                    () =>
                    {
                        if (isFirstMessageFlag)
                        {
                            isFirstMessageFlag = false;
                            _notificationManager.CloseAsync(identifier);
                            _notificationManager.ShowAsync(new NotificationContent
                            {
                                Title = "Information",
                                Message = $"Receiving messages from \"{TopicName}\"",
                                Type = NotificationType.Information
                            }, "WindowArea");
                            _logger.LogInformation("Receiving messages from \"{TopicName}\"", TopicName);
                        }
                        Offset = e.Offset;
                        Messages.Add(e.Message);

                        this.Updated = true;

                        // Change log level to Trace for the topic to see this message
                        _logger.LogTrace(
                            "Received message from \"{TopicName}\", with offset={Offset}, key=\"{Key}\" and timestamp=\"{Timestamp}\"",
                            TopicName, e.Offset, e.Message.Key, e.Message.Timestamp.UtcDateTime
                        );
                    });
            }
        }

        private bool subscribed = false;
        private readonly Guid identifier = Guid.NewGuid();

        public bool Subscribed
        {
            get { return this.subscribed; }
            internal set
            {
                if (this.subscribed != value)
                {
                    this.subscribed = value;

                    if (this.subscribed)
                    {
                        _notificationManager.ShowAsync(identifier, new NotificationContent
                        {
                            Title = "Information",
                            Message = $"Waiting messages from \"{TopicName}\"",
                            Type = NotificationType.Information
                        }, "WindowArea", TimeSpan.MaxValue);
                        _logger.LogInformation("Subscribed to \"{TopicName}\"", TopicName);
                    }
                    else
                    {
                        Dispatcher.CurrentDispatcher.Invoke(() => _notificationManager.CloseAsync(identifier));
                        _logger.LogInformation("Unsubscribed from \"{TopicName}\"", TopicName);
                    }
                    RaisePropertyChanged(nameof(Subscribed));
                }
            }
        }

        private bool _updated = false;

        /// <summary>
        /// <para>
        /// Set to true when a new item is added to the collection.
        /// </para>
        /// </summary>
        /// <remarks>
        /// It will automatically revert back to false after a brief delay
        /// </remarks>
        public bool Updated
        {
            get
            {
                return _updated;
            }
            set
            {
                _updated = value;
                RaisePropertyChanged(nameof(Updated));

                if (_updated)
                {
                    Task.Delay(1500).ContinueWith(t =>
                    {
                        _updated = false;
                        RaisePropertyChanged(nameof(Updated));
                    });
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
