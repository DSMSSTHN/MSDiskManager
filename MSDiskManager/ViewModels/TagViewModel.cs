#nullable enable
using MSDiskManagerData.Data.Entities;
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
        private bool isSelected = false;
        private bool isHidden = false;

        public long? Id { get => _id; set { _id = value; } }
        public string Name { get => name; set { name = value; NotifyPropertyChanged("Name"); } }
        public int Color { get => color; set { color = value; NotifyPropertyChanged("Color"); } }
        public bool IsSelected { get => isSelected; set { isSelected = value;NotifyPropertyChanged("IsSelected"); } }
        public bool IsHidden { get => isHidden; set { isHidden = value;NotifyPropertyChanged("IsSelected"); } }

        public TagViewModel() { }
        public TagViewModel(Tag tag)
        {
            this.Id = tag.Id;
            this.name = tag.Name;
            this.color = tag.Color;
            this.isSelected = false;
        }

        public Tag Tag => new Tag { Id = Id, Name = name, Color = color, IsHidden = IsHidden };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
