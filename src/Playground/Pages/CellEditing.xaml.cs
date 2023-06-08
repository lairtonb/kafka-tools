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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Playground.Pages
{
    /// <summary>
    /// Interaction logic for CellEditing.xaml
    /// </summary>
    public partial class CellEditing : Page
    {
        public CellEditing()
        {
            InitializeComponent();

            DataContext = this;
        }

        public ObservableCollection<TestData> TestList { get; set; }
            = new ObservableCollection<TestData>();

    }

    public class TestData
    {
        DateTime start;
        public DateTime Start
        {
            get { return start; }
            set { start = value; }
        }
    }
}
