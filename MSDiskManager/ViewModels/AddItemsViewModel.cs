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
        private DirectoryEntity parent;
        private DirectoryViewModel currentDirectory;
        private List<FileViewModel> baseFiles = new List<FileViewModel>();
        private List<DirectoryViewModel> baseDirectories = new List<DirectoryViewModel>();
        private List<FileViewModel> filesList = new List<FileViewModel>();
        private List<DirectoryViewModel> directoriesList = new List<DirectoryViewModel>();
        private string fileNameFilter = "";
        private string dirNameFilter = "";
        private DirectoryEntity distanation;
        private bool filesOnly = false;

        private object filesLock;
        private object foldersLock;
        public AddItemsViewModel()
        {
        }


        public ObservableCollection<FileViewModel> Files { get; set; } = new ObservableCollection<FileViewModel>();
        //public ObservableCollection<BaseEntityViewModel> ItemsToMove  { get; set; } = new ObservableCollection<FileViewModel>();
        public ObservableCollection<DirectoryViewModel> Directories { get; set; } = new ObservableCollection<DirectoryViewModel>();
        public DirectoryViewModel CurrentDirectory { get => currentDirectory; set { currentDirectory = value; NotifyPropertyChanged("CurrentDirectory"); } }
        public DirectoryEntity Parent { get => parent; set { parent = value; NotifyPropertyChanged("Parent"); } }
        public List<TypeModel> Types { get; } = TypeModel.OnlyFileFilterTypes;
        public DirectoryEntity Distanation { get => distanation; set { distanation = value; NotifyPropertyChanged("Distanation"); } }
        public bool FilesOnly { get => filesOnly; set { filesOnly = value; NotifyPropertyChanged("FilesOnly"); } }
        public string FileNameFilter
        {
            get => fileNameFilter; set
            {
                if (value.Length > fileNameFilter.Length && value.Contains(fileNameFilter))
                {
                    FilesList = FilesList.Where(f => f.Name.Contains(value)).ToList();
                }
                else if (fileNameFilter.Length > value.Length && fileNameFilter.Contains(value))
                {
                    var fl = BaseFiles.Where(f => f.Name.Contains(value) && !f.Name.Contains(fileNameFilter)).ToList();
                    fl.AddRange(FilesList);
                    FilesList = fl;
                }
                else
                {
                    FilesList = BaseFiles.Where(f => f.Name.Contains(value)).ToList();
                }
                fileNameFilter = value;
            }
        }
        public string DirectoryNameFilter
        {
            get => dirNameFilter; set
            {
                if (value.Length > dirNameFilter.Length && value.Contains(dirNameFilter))
                {
                    DirectoriesList = DirectoriesList.Where(f => f.Name.Contains(value)).ToList();
                }
                else if (dirNameFilter.Length > value.Length && dirNameFilter.Contains(value))
                {
                    var dl = BaseDirectories.Where(f => f.Name.Contains(value) && !f.Name.Contains(dirNameFilter)).ToList();
                    dl.AddRange(DirectoriesList);
                    DirectoriesList = dl;
                }
                else
                {
                    DirectoriesList = BaseDirectories.Where(f => f.Name.Contains(value)).ToList();
                }
                dirNameFilter = value;
            }
        }
        public List<FileViewModel> BaseFiles
        {
            get => baseFiles; set
            {
                baseFiles = value;
                FilesList = Globals.IsNullOrEmpty(FileNameFilter) ? value : value.Where(v => v.Name.Contains(FileNameFilter)).ToList();
            }
        }
        public List<DirectoryViewModel> BaseDirectories
        {
            get => baseDirectories; set
            {
                baseDirectories = value;
                DirectoriesList = Globals.IsNullOrEmpty(DirectoryNameFilter) ? value : value.Where(v => v.Name.Contains(DirectoryNameFilter)).ToList();
            }
        }
        public List<FileViewModel> FilesList
        {
            get => filesList; set
            {
                filesList = value;
                var ToRemove = Files.Where(f => !value.Contains(f)).ToList();
                var ToAdd = value.Where(f => !Files.Contains(f)).ToList();
                try
                {
                    foreach (var item in ToRemove) Files.Remove(item);
                    foreach (var item in ToAdd) Files.InsertSorted(item, (e) => e.Name);
                }
                catch (Exception)
                {

                    return;
                }
            }
        }
        public List<DirectoryViewModel> DirectoriesList
        {
            get => directoriesList; set
            {
                directoriesList = value;
                var ToRemove = Directories.Where(f => !value.Contains(f)).ToList();
                var ToAdd = value.Where(f => !Directories.Contains(f)).ToList();
                try
                {
                    foreach (var item in ToRemove) Directories.Remove(item);
                    foreach (var item in ToAdd) Directories.InsertSorted(item, (e) => e.Name);
                }
                catch (Exception)
                {

                    return;
                }
            }
        }
        public void Reset(List<FileViewModel> Files = null,List<DirectoryViewModel> Directories = null)
        {
            this.baseDirectories.Clear();
            this.directoriesList.Clear();
            this.Directories.Clear();
            this.baseFiles.Clear();
            this.filesList.Clear();
            this.Files.Clear();
            this.DirectoryNameFilter = "";
            this.FileNameFilter = "";
            this.Files.AddRange(Files ?? new List<FileViewModel>());
            this.Directories.AddRange(Directories ?? new List<DirectoryViewModel>());
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
