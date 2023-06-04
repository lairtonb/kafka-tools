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
    /// <summary>
    /// Interaction logic for Page2.xaml
    /// </summary>
    public partial class Page2 : Page
    {
        public Page2()
        {
            InitializeComponent();

            // Add some initial items to the collection
            MyItems.Add("Apple");
            MyItems.Add("Banana");
            MyItems.Add("Cherry");
            MyItems.Add("Grape");

            FilteredItemsView = CollectionViewSource.GetDefaultView(MyItems);

            DataContext = this;
        }

        private ObservableCollection<string> myItems = new();
        public ObservableCollection<string> MyItems
        {
            get { return myItems; }
            set
            {
                if (myItems != value)
                {
                    myItems = value;
                    OnPropertyChanged(nameof(MyItems));
                    OnPropertyChanged(nameof(FilteredItemsView));
                }
            }
        }

        private ICollectionView myItemsView;
        public ICollectionView FilteredItemsView
        {
            get { return myItemsView; }
            set
            {
                if (myItemsView != value)
                {
                    myItemsView = value;
                    OnPropertyChanged(nameof(FilteredItemsView));
                }
            }
        }

        private string filterText;
        public string FilterText
        {
            get { return filterText; }
            set
            {
                if (filterText != value)
                {
                    filterText = value;
                    OnPropertyChanged(nameof(FilterText));
                    ApplyFilter();
                }
            }
        }

        private void ApplyFilter()
        {
            FilteredItemsView.Filter = item => string.IsNullOrEmpty(FilterText) || ((string)item).Contains(FilterText);
            FilteredItemsView.Refresh();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
