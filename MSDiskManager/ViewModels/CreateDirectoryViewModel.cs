using MSDiskManagerData.Data.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManager.ViewModels
{
    public class CreateDirectoryViewModel : INotifyPropertyChanged
    {
        private string name = "new_file";
        private string description = "new_file";
        private ObservableCollection<Tag> tags = new ObservableCollection<Tag>();
        private bool isHidden;

        public string Name { get => name; set { name = value; NotifyPropertyChanged("Name"); } }
        public string Description { get => description; set { description = value; NotifyPropertyChanged("Description"); } }
        public ObservableCollection<Tag> Tags { get => tags; set { tags = value; NotifyPropertyChanged("Tags"); } }

        public virtual bool IsHidden { get => isHidden; set { isHidden = value; NotifyPropertyChanged("IsHidden"); } }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
