#nullable enable
using MSDiskManager.Helpers;
using MSDiskManagerData.Data;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Entities.Relations;
using MSDiskManagerData.Data.Repositories;
using MSDiskManagerData.Helpers;
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
        protected string originalName = "";
        public void RestoreName()
        {
            Name = originalName;
            IsRenaming = false;
        }
        public void CommitName()
        {
            originalName = Name;
            IsRenaming = false;
        }
        public bool NameChanged => Name != originalName;
        protected object? content;
        private Brush originalBrush => Application.Current.Resources["Primary"] as SolidColorBrush ?? new SolidColorBrush(Colors.DarkGray);
        private Brush ignoredBrush => new SolidColorBrush(Colors.Black);

        private long? _id;
        private string name = "New Item";
        private string onDeskName = "New_Item";
        private string description = "";
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
        private int numberOfItemsRec = 0;
        private ObservableCollection<Tag> tags = new ObservableCollection<Tag>();
        private Brush background = new SolidColorBrush(Colors.Transparent);
        private ImageSource? image;
        private bool isRenaming = false;
        private string errorMessage = "";
        private string onDeskSize = "";
        private string iconPath = "";
        private double iconHeight = 40;

        public string IconPath
        {
            get => iconPath; protected set { iconPath = value; Changed(); }
        }
        public double IconHeight { get => iconHeight; set { iconHeight = value; Changed(); } }
        public bool ShouldRemove { get; set; } = false;

        public long? Id { get => _id; set { _id = value; } }
        public string Name { get => name; set { name = value; if (Globals.IsNullOrEmpty(originalName)) originalName = value; Changed("Name"); } }
        public string OnDeskName
        {
            get => onDeskName; set
            {
                var v = value.Replace("\\", "").Replace("/", "").Replace(";", "").Trim();
                this.Path = this.Path.Replace(onDeskName, v);
                onDeskName = v;
                Changed("onDeskName");
            }
        }
        public int NumberOfItems { get => numberOfItemsRec; set { numberOfItemsRec = value; Changed("NumberOfItems"); } }

        public string ErrorMessage { get => errorMessage; set { errorMessage = value; Changed("ErrorMessage"); } }
        public string Description { get => description; set { description = value; Changed("Description"); } }
        public ObservableCollection<Tag> Tags { get => tags; set { tags = value; Changed("Tags"); } }
        public string OriginalPath { get => originalPath; set => originalPath = value; }
        public String Path
        {
            get => path; set
            {
                var v = value.Trim().Replace("/", "\\").Replace(";", "\\");
                if (this is DirectoryViewModel)
                {
                    var d = this as DirectoryViewModel;
                    d.Children.ForEach(c => c.Path = c.Path.Replace(path, v));
                    d.Files.ForEach(f => f.Path = f.Path.Replace(path, v));
                }
                path = v; Changed("Path");


            }
        }
        public long? ParentId
        {
            get => parentId; set
            {
                if (parentId != null && ancestorIds.Contains(parentId.Value)) AncestorIds.Remove(parentId.Value);
                parentId = value; Changed("ParentId");
                if (value != null && !ancestorIds.Contains(value.Value)) AncestorIds.Add(value.Value);
            }
        }
        public DirectoryViewModel? Parent
        {
            get => parent; set { parent = value; Changed("Parent"); }
        }
        public Brush Background { get => background; set { background = value; Changed("Background"); } }
        public List<long> AncestorIds
        {
            get => ancestorIds; set
            {
                ancestorIds = value; Changed("AncestorIds");
                if (this is DirectoryViewModel && Id != null)
                {
                    var d = this as DirectoryViewModel;
                    d.Children.ForEach(c => c.AncestorIds = value.With(this.Id.Value));
                    d.Files.ForEach(c => c.AncestorIds = value.With(this.Id.Value));
                }
            }
        }
        public Instant AddingDate { get => addingDate; set { addingDate = value; Changed("AddingDate"); } }
        public Instant MovingDate { get => movingDate; set { movingDate = value; Changed("MovingDate"); } }
        public virtual bool IsHidden
        {
            get => isHidden; set
            {
                isHidden = value; Changed("IsHidden");
            }
        }
        public virtual bool IgnoreAdd
        {
            get => ignoreAdd; set
            {
                ignoreAdd = value;
                Background = value ? ignoredBrush : originalBrush;
                Changed("IgnoreAdd");
            }
        }
        public bool IsSelected
        {
            get => isSelected; set
            {
                isSelected = value; Changed("IsSelected");
                Background = !value ? Brushes.Transparent : (Application.Current.Resources["Primary"] as SolidColorBrush)!;
            }
        }
        public bool IsRenaming { get => isRenaming; set { isRenaming = value; Changed("IsRenaming"); } }
        public string FullPath { get => MSDM_DBContext.DriverName[0] + ":\\" + Path; }

        public virtual long Size { get; }

        public virtual IconType IconType { get; }
        public virtual ImageSource? Image { get => image; set { image = value; Changed("Image"); } }
        public virtual double ImageWidth { get; }
        public virtual string OnDeskSize { get => onDeskSize; set { onDeskSize = value; Changed("OnDeskSize"); } }
        public virtual object? TooltipContent { get; }


        public async Task<BaseEntity> Refresh()
        {
            if (Id == null) return null;
            var id = (long)Id;
            BaseEntity be = (this is FileViewModel) ? (await new FileRepository().GetFile(id)) : (await new DirectoryRepository().GetDirectory(id));
            if (be == null) return null;
            this.Name = be.Name;
            this.Path = be.Path;
            this.onDeskName = be.OnDeskName;
            this.MovingDate = be.MovingDate;
            this.AncestorIds = be.AncestorIds;
            this.Description = be.Description;
            this.Tags.Clear();

            if (be is MSFile)
            {
                (this as FileViewModel)!.Extension = (be as MSFile)!.Extension;
                this.Tags.AddMany((be as MSFile)!.FileTags.Select(ft => ft.Tag).ToList());
            }
            else
            {
                this.Tags.AddMany((be as MSDirecotry)!.DirectoryTags.Select(ft => ft.Tag).ToList());
            }
            this.ParentId = be.ParentId;
            this.IsHidden = be.IsHidden;


            return be;
        }



        public event PropertyChangedEventHandler? PropertyChanged;
        protected void Changed([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
