using MSDiskManager.Helpers;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Helpers;
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
    public class AddItemsViewModel : INotifyPropertyChanged
    {
        private readonly static List<string> typeNames = Enum.GetValues(typeof(FileType)).Cast<FileType>().Select(ft => ft.ToString().ToLower()).ToList();
        private string filter = "";
        private bool filesOnly = false;
        private DirectoryEntity parent;
        private bool canScroll;

        public DirectoryEntity Distanation { get => parent; set => parent = value; }

        private List<BaseEntityViewModel> removedItems { get; set; } = new List<BaseEntityViewModel>();
        public ObservableCollection<BaseEntityViewModel> Items { get; } = new ObservableCollection<BaseEntityViewModel>();
        public bool CanScroll { get => canScroll; set { canScroll = value; NotifyPropertyChanged("CanScroll"); } }
        public bool FilesOnly { get => filesOnly; set { filesOnly = value; NotifyPropertyChanged("FilesOnly"); } }
        public string Filter
        {
            get => filter; set
            {
                //if (filter.Length > value.Length && filter.Contains(value))
                //{
                //    Items.InsertSorted(removedItems.RemoveWhere(i => checkEntity(i, value)), (e) => ((e is DirectoryViewModel) ? "0" : "1") + e.Name);
                //}
                //else if (filter.Length < value.Length && value.Contains(filter))
                //{
                //    removedItems.AddRange(Items.RemoveWhere(i => !checkEntity(i, value)));
                //}
                //else
                //{
                    var result = removedItems.Where(i => checkEntity(i, value)).ToList();
                    removedItems.AddRange(Items.RemoveWhere(i => !checkEntity(i, value)).Where(i => !removedItems.Contains(i)));
                    Items.InsertSorted(result.Where( i => !Items.Contains(i)).ToList(), (e) => ((e is DirectoryViewModel) ? "0" : "1") + e.Name);
                removedItems.RemoveAll(i => Items.Contains(i));
                //}
                filter = value;
                NotifyPropertyChanged("Filter");
            }
        }
        private bool checkEntity(BaseEntityViewModel entity, string filter)
        {
            if (entity is DirectoryViewModel && filesOnly) return false;
            if (entity is FileViewModel) return checkFileEntity(entity as FileViewModel, filter);
            return entity.Name.ToLower().Contains(filter.ToLower())
                || entity.OnDeskName.ToLower().Contains(filter.ToLower());
        }
        private bool checkFileEntity(FileViewModel file, string filter)
        {
            var segments = filter.Trim().ToLower().Split(" ").Where(s => Globals.IsNotNullNorEmpty(s.Trim())).ToList();
            foreach(var s in segments)
            {
                if (typeNames.Any(t => s.Contains(t))) {if(!checkFileType(file, s)) return false; }
                else if (!file.Name.ToLower().Contains(s) && !file.Description.ToLower().Contains(s) && !file.OnDeskName.ToLower().Contains(s)) return false;
            }
            return true;
           
        }
        private bool checkFileType(FileViewModel file,string segment)
        {
            if (segment[0] == '!' && segment.Contains(file.FileType.ToString().ToLower())) return false; else if (segment[0] != '!') return segment.Contains(file.FileType.ToString().ToLower()); else return true;
        }
        public void Reset()
        {
            this.Items.Clear();
            this.Filter = "";
        }
        public void Reset<T>(List<T> items = null)
            where T : BaseEntityViewModel
        {
            this.Items.Clear();
            this.Filter = "";
            if (items != null) this.Items.InsertSorted(items, (i) => ((i is DirectoryViewModel) ? "0" : "1") + i.Name);
        }
        public void RemoveEntity(BaseEntityViewModel entity)
        {
            Items.Remove(entity);
            removedItems.Remove(entity);
        }
        public void AddEntity(BaseEntityViewModel entity)
        {
            Items.InsertSorted(entity, (i) => ((i is DirectoryViewModel) ? "0" : "1") + i.Name);
        }
        public void AddEntities<T>(ICollection<T> entities)
            where T : BaseEntityViewModel, new()
        {
            Items.InsertSorted(entities, (i) => ((i is DirectoryViewModel) ? "0" : "1") + i.Name);
        }
        //public void Reset(List<FileViewModel> Files = null,List<DirectoryViewModel> Directories = null)
        //{
        //    this.baseDirectories.Clear();
        //    this.directoriesList.Clear();
        //    this.Directories.Clear();
        //    this.baseFiles.Clear();
        //    this.filesList.Clear();
        //    this.Files.Clear();
        //    this.DirectoryNameFilter = "";
        //    this.FileNameFilter = "";
        //    this.Files.AddRange(Files ?? new List<FileViewModel>());
        //    this.Directories.AddRange(Directories ?? new List<DirectoryViewModel>());
        //}


        public void checkType(TypeModel type)
        {

        }
        public void uncheckType(TypeModel type)
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
