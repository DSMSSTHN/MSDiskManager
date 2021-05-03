#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManager.ViewModels
{
    public class TagViewModel : INotifyPropertyChanged
    {
        private long? _id;
        private string name = "new tag";
        private int color = 0;


        public long? Id { get => _id; set { _id = value; } }
        public string Name { get => name; set { name = value; NotifyPropertyChanged("Name"); } }
        public int Color { get => color; set { color = value; NotifyPropertyChanged("Color"); } }




        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
