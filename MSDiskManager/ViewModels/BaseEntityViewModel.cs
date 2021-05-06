#nullable enable
using MSDiskManagerData.Data;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Entities.Relations;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace MSDiskManager.ViewModels
{
    public abstract class BaseEntityViewModel : INotifyPropertyChanged
    {
        protected object? content;
        private Brush originalBrush => Application.Current.Resources["primary"] as SolidColorBrush ?? new SolidColorBrush(Colors.DarkGray);
        private Brush ignoredBrush => new SolidColorBrush(Colors.Black);

        private long? _id;
        private string name = "new_file";
        private string onDeskName = "new_file";
        private string description = "new_file";
        private string path = "";
        private long? parentId;
        private DirectoryViewModel? parent;
        private List<long> ancestorIds = new List<long>();
        private Instant addingDate;
        private Instant movingDate;
        private bool isHidden;
        private string originalPath = "";
        private bool ignoreAdd = false;
        private bool isSelected = false;
        private ObservableCollection<Tag> tags = new ObservableCollection<Tag>();
        private Brush background = Application.Current.Resources["primaryLight"] as SolidColorBrush ?? new SolidColorBrush(Colors.DarkGray);
        private ImageSource? image;

        public bool ShouldRemove { get; set; } = false;

        public long? Id { get => _id; set { _id = value; } }
        public string Name { get => name; set { name = value; NotifyPropertyChanged("Name"); } }
        public string OnDeskName
        {
            get => onDeskName; set { onDeskName = value; NotifyPropertyChanged("onDeskName"); }
        }
        public string Description { get => description; set { description = value; NotifyPropertyChanged("Description"); } }
        public ObservableCollection<Tag> Tags { get => tags; set { tags = value; NotifyPropertyChanged("Tags"); } }
        public string OriginalPath { get => originalPath; set => originalPath = value; }
        public String Path { get => path; set { path = value; NotifyPropertyChanged("Path"); } }
        public long? ParentId { get => parentId; set { parentId = value; NotifyPropertyChanged("ParentId"); } }
        public DirectoryViewModel? Parent
        {
            get => parent; set { parent = value; NotifyPropertyChanged("Parent"); }
        }
        public Brush Background { get => background; set { background = value; NotifyPropertyChanged("Background"); } }
        public List<long> AncestorIds { get => ancestorIds; set { ancestorIds = value; NotifyPropertyChanged("AncestorIds"); } }
        public Instant AddingDate { get => addingDate; set { addingDate = value; NotifyPropertyChanged("AddingDate"); } }
        public Instant MovingDate { get => movingDate; set { movingDate = value; NotifyPropertyChanged("MovingDate"); } }
        public virtual bool IsHidden
        {
            get => isHidden; set
            {
                isHidden = value; NotifyPropertyChanged("IsHidden");
            }
        }
        public virtual bool IgnoreAdd
        {
            get => ignoreAdd; set
            {
                ignoreAdd = value;
                Background = value ? ignoredBrush : originalBrush;
                NotifyPropertyChanged("IgnoreAdd");
            }
        }
        public bool IsSelected
        {
            get => isSelected; set
            {
                isSelected = value;

                NotifyPropertyChanged("IsSelected");
            }
        }
        public string FullPath { get => MSDM_DBContext.DriverName + Path; }

        public virtual IconType IconType { get; }
        public virtual ImageSource? Image { get => image; set { image = value; NotifyPropertyChanged("Image"); } }
        public virtual double ImageWidth { get; }

        public virtual object? TooltipContent { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
