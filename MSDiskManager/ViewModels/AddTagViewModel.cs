using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManager.ViewModels
{
    public class AddTagViewModel : INotifyPropertyChanged
    {
        private string name = "";
        private int color = 0;

        public string Name { get => name; set { name = value; NotifyPropertyChanged("Name"); } }
        public int Color { get => color; set { color = value; NotifyPropertyChanged("Color"); } }
        public List<int> AllColors
        {
            get
            {
                var lst = new List<int>(); for (int i = 0; i < 11; i++)
                {
                    lst.Add(i);
                }
                return lst;
            }
        }



        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

}
