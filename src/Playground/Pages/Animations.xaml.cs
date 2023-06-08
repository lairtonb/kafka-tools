using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Playground.Pages
{
    public class AnimatedRow
    {
        public string Text { get; set; }
        public Brush BackgroundBrush { get; set; }
        public Brush ForegroundBrush { get; set; }
    }

    public class InsertAtTopObservableCollection<T> : ObservableCollection<T>
    {
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(0, item);
        }
    }

    /// <summary>
    /// Interaction logic for Animations.xaml
    /// </summary>
    public partial class Animations : Page
    {
        private long _startOffset = 0;

        public Animations()
        {
            InitializeComponent();

            ItemsCollection = new ObservableCollection<SampleData>();
            sampleDataGrid.ItemsSource = ItemsCollection;
        }

        public ObservableCollection<SampleData> ItemsCollection { get; set; }

        private async void ButtonAddMany_Click(object sender, RoutedEventArgs e)
        {
            Random random = new Random();
            for (var i = 0; i < 150; i++)
            {
                var jsonMessage = new SampleData
                {
                    Offset = _startOffset++,
                    Key = "key",
                    Value = "value",
                    Timestamp = DateTime.Now
                };
                ItemsCollection.Add(jsonMessage);
                sampleDataGrid.ScrollIntoView(jsonMessage);
                int jitterMilliseconds = random.Next(1, 200);
                await Task.Delay(jitterMilliseconds);
            }
        }

        private void ButtonAddOne_Click(object sender, RoutedEventArgs e)
        {
            var jsonMessage = new SampleData
            {
                Offset = _startOffset++,
                Key = "key",
                Value = "value",
                Timestamp = DateTime.Now
            };
            ItemsCollection.Add(jsonMessage);
            sampleDataGrid.ScrollIntoView(jsonMessage);
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var row = e.Row;

            var jsonMessage = row.DataContext as SampleData;

            if (jsonMessage == null)
            {
                return;
            }

            if (jsonMessage.Loaded)
            {
                return;
            }

            jsonMessage.Loaded = true;

            var animation = FindResource("RowAnimationStoryboard") as Storyboard;

            row.Tag = animation;

            // Set the desired duration for the animation
            var animationDuration = TimeSpan.FromSeconds(5); // Adjust the duration as needed
            animation.Duration = new Duration(animationDuration);

            // Create a new SolidColorBrush to animate
            var brush = new SolidColorBrush(Colors.White);
            row.Background = brush;

            // Set the SolidColorBrush as the target for the animation
            // Set the SolidColorBrush as the target for the animation
            animation?.SetValue(Storyboard.TargetPropertyProperty, new PropertyPath("(DataGridRow.Background).(SolidColorBrush.Color)"));
            animation?.SetValue(Storyboard.TargetPropertyProperty, new PropertyPath("(DataGridRow.Foreground).(SolidColorBrush.Color)"));

            // Begin the animation
            // animation?.Begin(row);

            // Begin the animation
            row.BeginStoryboard(animation);

            /****
            var row = e.Row;
            var animation = FindResource("RowAnimationStoryboard") as Storyboard;
            animation?.Begin(row);
            */
        }

        private void sampleDataGrid_UnloadingRow(object sender, DataGridRowEventArgs e)
        {
            var row = e.Row;
            if (row.Tag is Storyboard rowAnimation)
            {
                // Stop the storyboard and set the FillBehavior to Stop
                rowAnimation.Stop();
                //rowAnimation.FillBehavior = FillBehavior.Stop;

                // Apply the last frame of the animation to the row
                //rowAnimation.Seek(TimeSpan.FromSeconds(5));
            }

            // Stop the animation
            // row.BeginAnimation(DataGridRow.BackgroundProperty, null);
            // row.BeginAnimation(DataGridRow.ForegroundProperty, null);

            // Set the final desired style of the row
            row.Background = Brushes.White;
            row.Foreground = Brushes.Black;
        }
    }
}
