using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MVVMTest01
{
    internal class ShoppingItemViewmodel : INotifyPropertyChanged
    {
        private readonly ShoppingItem _shoppingItem;
        private readonly AddItemCommand _addIemCommand;

        public ShoppingItemViewmodel()
        {
            _addIemCommand = new AddItemCommand();
        }

        public int Id
        {
            get { return _shoppingItem.Id; }
            set
            {
                _shoppingItem.Id = value;
                OnPropertyChanged("Id");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand AddItemCommand
        {
            get
            {
                return _addIemCommand;
            }
        }
    }
}
