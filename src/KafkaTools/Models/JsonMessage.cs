using Confluent.Kafka;
using KafkaTools.ViewModels;
using Notifications.Wpf.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace KafkaTools
{
    public class JsonMessage : INotifyPropertyChanged
    {
        public JsonMessage(Message<string, string> message)
        {
            Key = message.Key;
            Value = message.Value;
            Timestamp = message.Timestamp;
            Headers = new Dictionary<string, byte[]>();
            foreach (var item in message.Headers)
            {
                Headers.Add(item.Key, item.GetValueBytes());
            }
        }
        public JsonMessage() { }
        public Dictionary<string, byte[]> Headers { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public Timestamp Timestamp { get; set; }
        public long Offset { get; set; }

        private bool _isNew = false;

        public bool IsNew
        {
            get { return _isNew; }
            set
            {
                _isNew = value;
                RaisePropertyChanged(nameof(IsNew));

                // Set IsNew back to false after a brief delay
                if (_isNew)
                {
                    Task.Delay(1500).ContinueWith(t =>
                    {
                        _isNew = false;
                        RaisePropertyChanged(nameof(IsNew));
                    });
                }
            }
        }

        public Message<string, string> ToMessage()
        {
            var message = new Message<string, string>()
            {
                Key = this.Key,
                Value = this.Value,
                Timestamp = this.Timestamp,
                Headers = new Headers()
            };

            foreach (var key in this.Headers.Keys)
            {
                message.Headers.Add(key, this.Headers[key]);
            }

            return message;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MyCommand CopyMessageCommand
        {
            get
            {
                return new MyCommand((parameter) =>
                {
                    if (!string.IsNullOrEmpty(parameter.Text))
                    {
                        return true;
                    }
                    return false;
                }, CopyMessage);
            }
        }

        private static async Task CopyMessage(TextBox textBox)
        {
            var selectionStart = textBox.SelectionStart;
            var selectionLength = textBox.SelectionLength;

            textBox.SelectAll();
            textBox.Copy();

            await Task.CompletedTask;
            /****
            await _notificationManager.ShowAsync(new NotificationContent
            {
                Title = "Information",
                Message = "Copied",
                Type = NotificationType.Information
            }, "WindowArea", expirationTime: TimeSpan.FromMilliseconds(1200)); */

            textBox.SelectionStart = selectionStart;
            textBox.SelectionLength = selectionLength;
        }
    }
}
