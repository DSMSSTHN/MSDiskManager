using MSDiskManager.Dialogs;
using MSDiskManager.Helpers;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Helpers;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MSDiskManager.ViewModels
{
   
    public class AddItemsViewModel : INotifyPropertyChanged
    {
        private readonly static List<string> typeNames = Enum.GetValues(typeof(FileType)).Cast<FileType>().Select(ft => ft.ToString().ToLower()).ToList();
        private string filter = "";
        private bool filesOnly = false;
        private DirectoryViewModel parent;
        private bool canScroll;
        private CancellationTokenSource cancels;
        public List<FileViewModel> Files { get; set; } = new List<FileViewModel>();
        public List<DirectoryViewModel> Dirs { get; set; } = new List<DirectoryViewModel>();
        public DirectoryViewModel CurrentDirectory { get; set; }
        public DirectoryViewModel Distanation { get => parent; set => parent = value; }

        private List<BaseEntityViewModel> removedItems { get; set; } = new List<BaseEntityViewModel>();
        public ObservableCollection<BaseEntityViewModel> Items { get; } = new ObservableCollection<BaseEntityViewModel>();
        public bool CanScroll { get => canScroll; set { canScroll = value; NotifyPropertyChanged("CanScroll"); } }
        public bool FilesOnly { get => filesOnly; set { filesOnly = value; NotifyPropertyChanged("FilesOnly"); } }
        public string Filter
        {
            get => filter; set
            {
                var result = removedItems.Where(i => checkEntity(i, value)).ToList();
                removedItems.AddRange(Items.RemoveWhere(i => !checkEntity(i, value)).Where(i => !removedItems.Contains(i)));
                Items.InsertSorted(result.Where(i => !Items.Contains(i)).ToList(), (e) => ((e is DirectoryViewModel) ? "0" : "1") + e.Name);
                removedItems.RemoveAll(i => Items.Contains(i));
                filter = value;
                NotifyPropertyChanged("Filter");
            }
        }
        private HashSet<Key> pressedKeys = new HashSet<Key>();
        public void KeyDown(Key key)
        {
            //if (key == Key.A && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))) Items.ToList().ForEach(i =>
            //{ if (!i.IsSelected) i.IsSelected = true; });
            //if (key == Key.F2 && lastClickedItem != null) BeginRenaming();
            if (key == Key.Escape) SelectedItems.ToList().ForEach(i => i.IsSelected = false);
            if (key == Key.Delete) SelectedItems.ToList().ForEach(i => HandleDelete(i));
            //if (key == Key.Enter)
            //{
            //    if (isRenaming) StoppedRenaming(lastClickedItem);
            //    else
            //    {
            //        if (entity is FileViewModel)
            //        {
            //            try
            //            {
            //                var pi = new ProcessStartInfo { UseShellExecute = true, FileName = (entity as FileViewModel)!.FullPath, Verb = "Open" };
            //                System.Diagnostics.Process.Start(pi);
            //            }
            //            catch (Exception e)
            //            {
            //                MessageBox.Show($"Error while tying to open file [{entity.FullPath}].\n{e.Message}");
            //            }
            //        }
            //        else if (entity is DirectoryViewModel)
            //        {
            //            Parent = entity as DirectoryViewModel;
            //        }
            //    }
            //}

            if (key == Key.F2)
            {
                Edit(null);
            }
        }
        private BaseEntityViewModel lastSelectedEntity;
        private long isWorking = 0;
        public void SelectEntity(BaseEntityViewModel entity)
        {
            if (entity == null) return;
            if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) && lastSelectedEntity != null && Items.Contains(lastSelectedEntity) && Items.Contains(lastSelectedEntity))
            {
                var index1 = Items.IndexOf(entity);
                var index2 = Items.IndexOf(lastSelectedEntity);
                var min = Math.Min(index1, index2);
                var max = Math.Max(index1, index2);
                for (int i = 0; i < Items.Count; i++)
                {
                    if ((i < min || i > max) && Items[i].IsSelected) Items[i].IsSelected = false;
                    else if (i >= min && i <= max && !Items[i].IsSelected) Items[i].IsSelected = true;
                }
                return;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                entity.IsSelected = !entity.IsSelected;
                if (!entity.IsSelected) return;
            }
            else
            {
                var its = Items.ToList();
                try
                {
                    foreach (var item in its) if (item != entity && item.IsSelected) item.IsSelected = false;
                    if (entity != null) entity.IsSelected = !entity.IsSelected;
                }
                catch { }
            }
            lastSelectedEntity = entity;
        }
        public IEnumerable<BaseEntityViewModel> CurrentItems => (CurrentDirectory == null ?
            Files.Cast<BaseEntityViewModel>().Union(Dirs) :
            CurrentDirectory.Files.Cast<BaseEntityViewModel>().Union(CurrentDirectory.Children));
        public IEnumerable<BaseEntityViewModel> SelectedItems => CurrentItems.Where(i => i.IsSelected);
        public void Edit(BaseEntityViewModel? entity)
        {
            var selected = SelectedItems.ToList();
            var e = entity ?? selected.FirstOrDefault();
            if (e == null) return;
            selected.Remove(e);
            var diag = new EditEntitiesDialog(e, selected, true);
            diag.ShowDialog();
        }

        public List<BaseEntityViewModel> HandleDelete(BaseEntityViewModel? entity)
        {

            if (parent != null)
            {

                parent.Files = parent.Files.Where(f => !f.IsSelected && f != entity).ToList();


            }
            var fs = CurrentDirectory == null ? Files : CurrentDirectory.Files;
            var ds = CurrentDirectory == null ? Dirs : CurrentDirectory.Children;
            var iss = fs.RemoveWhere(i => i.IsSelected || i == entity).Cast<BaseEntityViewModel>().ToList();//Items.RemoveWhere(i => i.IsSelected || i == entity);
            iss.AddRange(ds.RemoveWhere(d => d.IsSelected || d == entity));
            Items.RemoveWhere(i => iss.Contains(i));
            iss.ForEach(i =>
        {
            if (i.Parent != null)
            {
                i.Parent.Files = i.Parent.Files.Where(f => !f.IsSelected && f != entity).ToList();
                i.Parent.Children = i.Parent.Children.Where(c => !c.IsSelected && c != entity).ToList();
            }
        });
            return iss;
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
            foreach (var s in segments)
            {
                if (typeNames.Any(t => s.Contains(t))) { if (!checkFileType(file, s)) return false; }
                else if (!file.Name.ToLower().Contains(s) && !file.Description.ToLower().Contains(s) && !file.OnDeskName.ToLower().Contains(s)) return false;
            }
            return true;

        }
        private bool checkFileType(FileViewModel file, string segment)
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
            entity.IsSelected = false;
            Items.Remove(entity);
            removedItems.Remove(entity);
        }
        public void AddEntity(BaseEntityViewModel entity)
        {
            Items.InsertSorted(entity, (i) => ((i is DirectoryViewModel) ? "0" : "1") + i.Name);
        }
        public async Task AddEntities<T>(IEnumerable<T> entities, bool sort, PauseTokenSource pauses)
            where T : BaseEntityViewModel, new()
        {
            pauses.IsPaused = false;
            cancels?.Cancel();
            cancels = new CancellationTokenSource();
            var token = cancels.Token;
            var delay = 1;
            if (sort) await Items.InsertSortedWithDelay(entities, (i) => ((i is DirectoryViewModel) ? "0" : "1") + i.Name, delay);
            else
            {
                var num = 0;
                foreach (var e in entities)
                {
                    if (token.IsCancellationRequested) return;
                    Items.Add(e);
                    await Task.Delay(delay);
                    if (++num % 100 == 0)
                    {
                        pauses.IsPaused = true;
                        await pauses.Token.WaitWhilePausedAsync();
                    }
                }
            }
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
