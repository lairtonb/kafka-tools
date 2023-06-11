using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

namespace Playground.Pages
{
    public class JsonMessage : INotifyPropertyChanged
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime Timestamp { get; set; }
        public long Offset { get; set; }

        private bool _isNew = false;

        public bool IsNew
        {
            get { return _isNew; }
            set
            {
                _isNew = value;
                RaisePropertyChanged(nameof(IsNew));

                // Set IsNewItemAdded back to false after a brief delay
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

        public event PropertyChangedEventHandler? PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Interaction logic for DataGridRowAnimation.xaml
    /// </summary>
    public partial class DataGridRowAnimation : Page
    {
        public DataGridRowAnimation()
        {
            InitializeComponent();
            AddNewMessage(new JsonMessage
            {
                Key = Guid.NewGuid().ToString(),
                Value = "{}",
                Timestamp = DateTime.Now,
                Offset = NextOffset,
                IsNew = true
            });
            AddNewMessage(new JsonMessage
            {
                Key = Guid.NewGuid().ToString(),
                Value = "{}",
                Timestamp = DateTime.Now,
                Offset = NextOffset,
                IsNew = true
            });
            AddNewMessage(new JsonMessage
            {
                Key = Guid.NewGuid().ToString(),
                Value = "{}",
                Timestamp = DateTime.Now,
                Offset = NextOffset,
                IsNew = true
            });
        }

        public ObservableCollection<JsonMessage> Messages { get; set; } = new ObservableCollection<JsonMessage>();

        private int _currentOffset = 0;

        private int NextOffset => ++_currentOffset;

        private void AddNewMessage(JsonMessage newMessage)
        {
            var lastMessage = Messages.LastOrDefault();

            if (lastMessage != null)
            {
                lastMessage.IsNew = false;
            }

            // Add the new item to the collection
            Messages.Add(newMessage);
        }

        private void Button_AddNewMessage_Click(object sender, RoutedEventArgs e)
        {
            AddNewMessage(new JsonMessage
            {
                Key = Guid.NewGuid().ToString(),
                Value = "{}",
                Timestamp = DateTime.Now,
                Offset = NextOffset,
                IsNew = true
            });
        }
    }
}
