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




        private string name = "";
        private string tagName = "";
        private bool filterFileName = false;
        private bool filterDescription = false;
        private bool currentFolderOnly = true;
        private bool currentFolderRecursive = false;
        private bool allFolders = false;
        private bool showHidden = false;
        private DirectoryEntity parent = null;

        public string Name { get => name; set { name = value; NotifyPropertyChanged("Name");} }
        public string TagName { get => tagName; set { tagName = value; NotifyPropertyChanged("TagName");} }
        public bool FilterFileName { get => filterFileName; set { filterFileName = value; NotifyPropertyChanged("FilterFileName");} }
        public bool FilterDescription { get => filterDescription; set { filterDescription = value; NotifyPropertyChanged("FilterDescription");} }
        public bool CurrentFolderOnly { get => currentFolderOnly; set { currentFolderOnly = value; NotifyPropertyChanged("CurrentFolderOnly");} }
        public bool CurrentFolderRecursive { get => currentFolderRecursive; set { currentFolderRecursive = value; NotifyPropertyChanged("CurrentFolderRecursive");} }
        public bool AllFolders { get => allFolders; set { allFolders = value; NotifyPropertyChanged("AllFolders");} }
        public bool ShowHidden { get => showHidden; set { showHidden = value; NotifyPropertyChanged("ShowHidden");} }
        public DirectoryEntity Parent { get => parent; set { parent = value; NotifyPropertyChanged("Parent");} }
        public List<TypeModel> FilterTypes { get; set; } = TypeModel.AllFilterTypes;
        public List<OrderModel> Orders { get; set; } = OrderModel.AllOrdersModels.ToList();
        public ObservableCollection<Tag> Tags { get; set; } = new ObservableCollection<Tag>();

        

        public bool OnlyFolders => FilterTypes.All(ft => ft.Type == MSItemType.Directory || !ft.IsChecked);
        public bool IncludeFolders => FilterTypes.First(ft => ft.Type == MSItemType.Directory).IsChecked;
        public List<FileType> FileTypes => FilterTypes.Where(ft => ft.Type != MSItemType.All && ft.Type != MSItemType.Directory && ft.IsChecked).Select(ft => ft.FileType).ToList();


        private OrderModel selectedOrder => Orders.FirstOrDefault(o => o.IsChecked) ?? Orders[0];
        private List<long> ancestorIds { get { var result = new List<long>(); result.Add((long)Parent.Id); return result; } }
        public FileFilter FileFilter => new FileFilter
        {
            DirectoryId = CurrentFolderOnly ? (Parent?.Id ?? -1) : null,
            AncestorIds = CurrentFolderRecursive ? (Parent == null ? null : ancestorIds) : null,
            IncludeDescriptionInSearch = filterDescription,
            IncludeFileNameInSearch = filterFileName,
            IncludeHidden = showHidden,
            Order = selectedOrder.Order,
            Name = Name,
            tagIds = Tags.Select(t => (long)t.Id).ToList(),
            TagName = TagName,
            Types = FileTypes
        };
        public DirectoryFilter DirectoryFilter => new DirectoryFilter
        {
            DirectoryId = CurrentFolderOnly ? (Parent?.Id ?? -1) : null,
            AncestorIds = CurrentFolderRecursive ? (Parent == null ? null : ancestorIds) : null,
            IncludeDescriptionInSearch = filterDescription,
            IncludeDirectoryNameInSearch = filterFileName,
            IncludeHidden = showHidden,
            Order = selectedOrder.DirectoryOrder,
            Name = Name,
            tagIds = Tags.Select(t => (long)t.Id).ToList(),
            TagName = TagName,

        };

        public FilterModel()
        {

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
