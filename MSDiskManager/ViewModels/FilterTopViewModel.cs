using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManager.ViewModels
{
    public class FilterTopViewModel : INotifyPropertyChanged
    {
        private FilterModel filterModel = new FilterModel();

        public FilterModel FilterModel { get => filterModel; set { filterModel = value; NotifyPropertyChanged("FilterModel"); } }






        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
