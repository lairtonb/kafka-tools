using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Printing;
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
    public class Topic : INotifyPropertyChanged

    {
        public string TopicName { get; set; }

        public bool Subscribed { get; set; } = true;

        private bool _updated = false;

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

                // Set IsNewItemAdded back to false after a brief delay
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

    /// <summary>
    /// Interaction logic for ListBoxRowAnimation.xaml
    /// </summary>
    public partial class ListBoxRowAnimation : Page
    {
        public ListBoxRowAnimation()
        {
            InitializeComponent();

            // Example event or method where you add an item to the collection
            AddNewItemToCollection(new Topic
            {
                TopicName = $"Topic {NextTopicNumber}",
                Updated = true
            });
        }

        private int _topicNumber = 0;

        private int NextTopicNumber => ++_topicNumber;

        private void AddNewItemToCollection(Topic newItem)
        {
            var lastTopic = Topics.LastOrDefault();

            if (lastTopic != null)
            {
                lastTopic.Updated = false;
            }

            // Add the new item to the collection
            Topics.Add(newItem);
        }

        private void Button_AddNewItem_Click(object sender, RoutedEventArgs e)
        {
            AddNewItemToCollection(new Topic
            {
                TopicName = $"Topic {NextTopicNumber}",
                Updated = true
            });
        }
        private void ButtonAnimateThirdItem_Click(object sender, RoutedEventArgs e)
        {
            var thirdTopic = Topics.SingleOrDefault(t => t.TopicName == "Topic 3");

            if (thirdTopic != null)
            {
                // Stop ongoing animation and start over
                thirdTopic.Updated = false;
                thirdTopic.Updated = true;
            }
        }

        public ObservableCollection<Topic> Topics { get; set; } = new ObservableCollection<Topic>();
    }
}
