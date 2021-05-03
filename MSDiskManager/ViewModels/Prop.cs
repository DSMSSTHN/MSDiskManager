using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManager.ViewModels
{
    public class Prop<T> : INotifyPropertyChanged
    {
        public Prop(T value)
        {
            this.Value = value;
        }
        private T _value;
        public T Value{get => _value;set { this._value = value;OnPropertyChanged("Value"); } }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
