#nullable enable
using MSDiskManager.Helpers;
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
    public class FilterModel : INotifyPropertyChanged
    {


        public void AddTag(Tag tag)
        {
            this.Tags.Add(tag);
            NotifyPropertyChanged("Tag");
        }
        public void RemoveTag(long id)
        {
            var tag = Tags.First(t => t.Id == id);
            Tags.Remove(tag);
            NotifyPropertyChanged("Tag");
        }



        private string name = "";
        private string tagName = "";
        private bool filterFileName = false;
        private bool filterDescription = false;
        private bool currentFolderOnly = true;
        private bool currentFolderRecursive = false;
        private bool allFolders = false;
        private bool showHidden = false;
        private DirectoryEntity? parent = null;
        private List<TypeModel> filterTypes = TypeModel.AllFilterTypes;
        private List<OrderModel> orders = OrderModel.AllOrdersModels.ToList();

        public string Name { get => name; set { name = value; NotifyPropertyChanged("Name"); } }
        public string TagName { get => tagName; set { tagName = value; NotifyPropertyChanged("TagName"); } }
        public bool FilterFileName { get => filterFileName; set { filterFileName = value; NotifyPropertyChanged("FilterFileName"); } }
        public bool FilterDescription { get => filterDescription; set { filterDescription = value; NotifyPropertyChanged("FilterDescription"); } }
        public bool CurrentFolderOnly { get => currentFolderOnly; set { currentFolderOnly = value; NotifyPropertyChanged("CurrentFolderOnly"); } }
        public bool CurrentFolderRecursive { get => currentFolderRecursive; set { currentFolderRecursive = value; NotifyPropertyChanged("CurrentFolderRecursive"); } }
        public bool AllFolders { get => allFolders; set { allFolders = value; NotifyPropertyChanged("AllFolders"); } }
        public bool ShowHidden { get => showHidden; set { showHidden = value; NotifyPropertyChanged("ShowHidden"); } }
        public DirectoryEntity Parent { get => parent; set { parent = value; NotifyPropertyChanged("Parent"); } }
        public List<TypeModel> FilterTypes { get => filterTypes; set => filterTypes = value; }
        public List<OrderModel> Orders { get => orders; set => orders = value; }
        public ObservableCollection<Tag> Tags { get; set; } = new ObservableCollection<Tag>();



        public bool OnlyFolders => FilterTypes.All(ft => ft.Type == MSItemType.Directory || !ft.IsChecked);
        public bool IncludeFolders => filterTypes.All(ft => !ft.IsChecked) || FilterTypes.First(ft => ft.Type == MSItemType.Directory).IsChecked;
        public List<FileType> FileTypes => FilterTypes.Where(ft => ft.Type != MSItemType.All && ft.Type != MSItemType.Directory && ft.IsChecked).Select(ft => ft.FileType).ToList();


        private OrderModel selectedOrder => Orders.FirstOrDefault(o => o.IsChecked) ?? Orders[0];
        private List<long> ancestorIds { get { var result = new List<long>(); if (Parent != null) result.Add((long)Parent.Id); return result; } }
        public FileFilter FileFilter => new FileFilter
        {
            DirectoryId = CurrentFolderOnly ? (Parent?.Id ?? -1) : null,
            AncestorIds = CurrentFolderRecursive ? (Parent == null ? null : ancestorIds) : null,
            IncludeDescriptionInSearch = filterDescription,
            IncludeFileNameInSearch = filterFileName,
            IncludeHidden = showHidden,
            Order = selectedOrder.Order,
            Name = Name,
            tagIds = Tags.Select(t => (long)t.Id!).ToList(),
            TagName = TagName,
            Types = FileTypes
        };
        public DirectoryFilter DirectoryFilter => new DirectoryFilter
        {
            ParentId = CurrentFolderOnly ? (Parent?.Id ?? -1) : null,
            AncestorIds = CurrentFolderRecursive ? (Parent == null ? new List<long>() : ancestorIds) : new List<long>(),
            IncludeDescriptionInSearch = filterDescription,
            IncludeDirectoryNameInSearch = filterFileName,
            IncludeHidden = showHidden,
            Order = selectedOrder.DirectoryOrder,
            Name = Name,
            tagIds = Tags.Select(t => (long)t.Id!).ToList(),
            TagName = TagName,

        };

        public FilterModel()
        {
            FilterTypes.ForEach(ft => { ft.PropertyChanged += filterTypesChanged!; });
            Orders.ForEach(o => { o.PropertyChanged += orderChanged!; });
        }
        private void filterTypesChanged(object f, PropertyChangedEventArgs args)
        {
            var tm = f as TypeModel;
            if (tm.Type == MSItemType.All)
            {
                foreach (var ft in filterTypes) ft.PropertyChanged -= filterTypesChanged!;
                foreach (var ft in filterTypes) ft.IsChecked = tm.IsChecked;
                foreach (var ft in filterTypes) ft.PropertyChanged += filterTypesChanged!;
            }
            else
            {
                foreach (var ft in filterTypes) ft.PropertyChanged -= filterTypesChanged!;
                filterTypes.First(ft => ft.Type == MSItemType.All).IsChecked = filterTypes.All(ft => ft.IsChecked || ft.Type == MSItemType.All);
                foreach (var ft in filterTypes) ft.PropertyChanged += filterTypesChanged!;
            }
            NotifyPropertyChanged("Types");
        }
        private void orderChanged(object f, PropertyChangedEventArgs args)
        {
            NotifyPropertyChanged("Order");
        }

        public FilterModel(string name, string tagName, bool filterFileName,
            bool filterDescription, bool currentFolderOnly,
            bool currentFolderRecursive, bool allFolders,
            bool showHidden, DirectoryEntity parent,
            List<TypeModel> filterTypes,
            List<OrderModel> orders, ObservableCollection<Tag> tags)
        {
            this.name = name;
            this.tagName = tagName;
            this.filterFileName = filterFileName;
            this.filterDescription = filterDescription;
            this.currentFolderOnly = currentFolderOnly;
            this.currentFolderRecursive = currentFolderRecursive;
            this.allFolders = allFolders;
            this.showHidden = showHidden;
            this.parent = parent;
            FilterTypes = filterTypes.Clone();
            Orders = orders.Clone();
            Tags = tags.Clone();
        }

        public FilterModel Clone => new FilterModel(
            Name, TagName, FilterFileName, FilterDescription, CurrentFolderOnly, CurrentFolderRecursive, AllFolders, ShowHidden, Parent, FilterTypes, Orders, Tags);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
