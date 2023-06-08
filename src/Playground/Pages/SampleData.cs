using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Playground.Pages
{
    public class SampleData
    {
        public SampleData() { }
        public Dictionary<string, byte[]> Headers { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime Timestamp { get; set; }
        public long Offset { get; set; }

        public bool Loaded { get; set; } = false;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
