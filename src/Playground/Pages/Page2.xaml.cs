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
    public class Fruit
    {
        public string Name { get; set; }
        public string Color { get; set; }
    }

    /// <summary>
    /// Interaction logic for Page2.xaml
    /// </summary>
    public partial class Page2 : Page
    {
        public Page2()
        {
            InitializeComponent();

            // Add some initial items to the collection
            MyItems.Add(new Fruit { Name = "Apple", Color = "Red" });
            MyItems.Add(new Fruit { Name = "Banana", Color = "Yellow" });
            MyItems.Add(new Fruit { Name = "Cherry", Color = "Red" });
            MyItems.Add(new Fruit { Name = "Grape", Color = "Red" });
            MyItems.Add(new Fruit { Name = "Lychee", Color = "Pink" });



            FilteredItemsView = CollectionViewSource.GetDefaultView(MyItems);

            DataContext = this;
        }

        private ObservableCollection<Fruit> _myItems = new();
        public ObservableCollection<Fruit> MyItems
        {
            get => _myItems;
            set
            {
                if (_myItems != value)
                {
                    _myItems = value;
                    OnPropertyChanged(nameof(MyItems));
                    OnPropertyChanged(nameof(FilteredItemsView));
                }
            }
        }

        private ICollectionView _myItemsView;
        public ICollectionView FilteredItemsView
        {
            get => _myItemsView;
            set
            {
                if (_myItemsView != value)
                {
                    _myItemsView = value;
                    OnPropertyChanged(nameof(FilteredItemsView));
                }
            }
        }

        private string _filterText;
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (_filterText != value)
                {
                    _filterText = value;
                    OnPropertyChanged(nameof(FilterText));
                    ApplyFilter();
                }
            }
        }

        private void ApplyFilter()
        {
            var myCollectionViewSource = (CollectionViewSource)FindResource("MyCollectionViewSource");
            myCollectionViewSource.Filter += (s, e) =>
            {
                if (e.Item is not Fruit item)
                {
                    e.Accepted = false;
                    return;
                }
                e.Accepted = item.Name.Contains(FilterText);
            };
            myCollectionViewSource.View.Refresh();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
