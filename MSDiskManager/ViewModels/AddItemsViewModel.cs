using MSDiskManager.Helpers;
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
        private DirectoryViewModel parent;
        private DirectoryViewModel currentDirectory;
        private List<FileViewModel> baseFiles = new List<FileViewModel>();
        private List<DirectoryViewModel> baseDirectories = new List<DirectoryViewModel>();
        private List<FileViewModel> filesList = new List<FileViewModel>();
        private List<DirectoryViewModel> directoriesList = new List<DirectoryViewModel>();
        private string nameFilter = "";

        public ObservableCollection<FileViewModel> Files { get; set; } = new ObservableCollection<FileViewModel>();
        //public ObservableCollection<BaseEntityViewModel> ItemsToMove  { get; set; } = new ObservableCollection<FileViewModel>();
        public ObservableCollection<DirectoryViewModel> Directories { get; set; } = new ObservableCollection<DirectoryViewModel>();
        public DirectoryViewModel CurrentDirectory { get => currentDirectory; set { currentDirectory = value; NotifyPropertyChanged("CurrentDirectory"); } }
        public DirectoryViewModel Parent { get => parent; set { parent = value; NotifyPropertyChanged("Parent"); } }
        public List<TypeModel> Types { get; } = TypeModel.OnlyFileFilterTypes;

        public string NameFilter
        {
            get => nameFilter; set
            {
                if(value.Length > nameFilter.Length && value.Contains(nameFilter))
                {
                    FilesList = FilesList.Where(f => f.Name.Contains(value)).ToList();
                    DirectoriesList = DirectoriesList.Where(f => f.Name.Contains(value)).ToList();
                } else if (nameFilter.Length > value.Length && nameFilter.Contains(value))
                {
                    var fl = BaseFiles.Where(f => f.Name.Contains(value) && !f.Name.Contains(nameFilter)).ToList();
                    fl.AddRange(FilesList);
                    FilesList = fl;
                    var dl = BaseDirectories.Where(f => f.Name.Contains(value) && !f.Name.Contains(nameFilter)).ToList();
                    dl.AddRange(DirectoriesList);
                    DirectoriesList = dl;
                } else
                {
                    FilesList = BaseFiles.Where(f => f.Name.Contains(value)).ToList();
                    DirectoriesList = BaseDirectories.Where(f => f.Name.Contains(value)).ToList();
                }
                nameFilter = value;
            }
        }
        public List<FileViewModel> BaseFiles
        {
            get => baseFiles; set
            {
                baseFiles = value;
                FilesList = value;
            }
        }
        public List<DirectoryViewModel> BaseDirectories
        {
            get => baseDirectories; set
            {
                baseDirectories = value;
                DirectoriesList = value;
            }
        }
        public List<FileViewModel> FilesList
        {
            get => filesList; set
            {
                filesList = value;
                var ToRemove = Files.Where(f => value.Contains(f));
                var ToAdd = value.Where(f => !Files.Contains(f));
                foreach (var item in ToRemove) Files.Remove(item);
                foreach (var item in ToAdd) Files.InsertSorted(item, (e) => e.Name);
            }
        }
        public List<DirectoryViewModel> DirectoriesList
        {
            get => directoriesList; set
            {
                directoriesList = value;
                var ToRemove = Directories.Where(f => value.Contains(f));
                var ToAdd = value.Where(f => !Directories.Contains(f));
                foreach (var item in ToRemove) Directories.Remove(item);
                foreach (var item in ToAdd) Directories.InsertSorted(item, (e) => e.Name);
            }
        }


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
