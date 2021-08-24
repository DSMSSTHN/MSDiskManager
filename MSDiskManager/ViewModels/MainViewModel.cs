#nullable enable
using MSDiskManager.Dialogs;
using MSDiskManager.Helpers;
using MSDiskManager.Pages.Main;
using MSDiskManagerData.Data;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using NodaTime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MSDiskManager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {


        public void AddTag(Tag tag)
        {
            this.Tags.Add(tag);
            NotifyPropertyChanged("Tag");
            _ = filterData();
        }
        public void RemoveTag(long id)
        {
            var tag = Tags.First(t => t.Id == id);
            Tags.Remove(tag);
            NotifyPropertyChanged("Tag");
            _ = filterData();
        }


        private long isWorking = 0;
        private string name = "";
        private string tagName = "";
        private bool filterFileName = false;
        private bool filterDescription = false;
        private bool currentFolderOnly = true;
        private bool currentFolderRecursive = false;
        private bool allFolders = false;
        private bool showHidden = false;
        private DirectoryViewModel? parent = null;
        private List<TypeModel> filterTypes = TypeModel.AllFilterTypes;
        private List<OrderModel> orders = OrderModel.AllOrdersModels.ToList();

        public string Name
        {
            get => name; set
            {
                name = value;
                NotifyPropertyChanged("Name");
                _ = filterData();
            }
        }
        public string TagName { get => tagName; set { tagName = value; NotifyPropertyChanged("TagName"); } }
        public bool FilterFileName { get => filterFileName; set { filterFileName = value; NotifyPropertyChanged("FilterFileName"); _ = filterData(); } }
        public bool FilterDescription { get => filterDescription; set { filterDescription = value; NotifyPropertyChanged("FilterDescription"); _ = filterData(); } }
        public Visibility LoadingVisibility { get => loadingVisibility; set { loadingVisibility = value; NotifyPropertyChanged("LoadingVisibility"); } }
        public ScrollBarVisibility VerticalScrollVisibility { get => verticalScrollVisibility; set { verticalScrollVisibility = value; NotifyPropertyChanged("VerticalScrollVisibility"); } }
        public bool CurrentFolderOnly
        {
            get => currentFolderOnly; set
            {
                currentFolderOnly = value;
                CanGoBack = value && parent != null;
                NotifyPropertyChanged("CurrentFolderOnly");
                _ = filterData();
            }
        }
        public bool CurrentFolderRecursive { get => currentFolderRecursive; set { currentFolderRecursive = value; NotifyPropertyChanged("CurrentFolderRecursive"); _ = filterData(); } }
        public bool AllFolders { get => allFolders; set { allFolders = value; NotifyPropertyChanged("AllFolders"); _ = filterData(); } }
        public bool ShowHidden { get => showHidden; set { showHidden = value; NotifyPropertyChanged("ShowHidden"); _ = filterData(); } }
        public string OnDeskSize { get => onDeskSize1; set { onDeskSize1 = value; NotifyPropertyChanged("OnDeskSize"); } }
        public int NumberOfItems
        {
            get => numberOfItems;
            set
            {
                numberOfItems = value;
                NotifyPropertyChanged("NumberOfItems");
            }
        }
        public DirectoryViewModel? Parent
        {
            get => parent; set
            {
                parent = value;

                if (value != null && (history.Count == 0 || history.Peek().Id != value.Id))
                {
                    history.Push(value);
                }
                CanGoBack = value != null && CurrentFolderOnly;
                NotifyPropertyChanged("Parent");
                _ = Task.Run(() => new DirectoryInfo((value == null ? (MSDM_DBContext.DriverName[0] + ":\\") : value.FullPath)).GetDirSize((s) => onDeskSize = s.ByteSizeToSizeString()));
                _ = filterData();
            }
        }
        public List<TypeModel> FilterTypes { get => filterTypes; set => filterTypes = value; }
        public List<OrderModel> Orders { get => orders; set => orders = value; }
        public ObservableCollection<Tag> Tags { get; set; } = new ObservableCollection<Tag>();



        public ObservableCollection<BaseEntityViewModel> Items { get; set; } = new ObservableCollection<BaseEntityViewModel>();
        private ConcurrentStack<CancellationTokenSource> _cancellationTokens = new ConcurrentStack<CancellationTokenSource>();
        private bool canGoBack = false;
        private int numberOfItems = 0;

        private Stack<DirectoryViewModel> history { get; set; } = new Stack<DirectoryViewModel>();
        public Boolean CanGoBack { get => canGoBack; set { canGoBack = value; NotifyPropertyChanged("CanGoBack"); } }
        public bool OnlyFolders => FilterTypes.All(ft => ft.Type == MSItemType.Directory || !ft.IsChecked);
        public bool IncludeFolders => filterTypes.All(ft => !ft.IsChecked) || FilterTypes.First(ft => ft.Type == MSItemType.Directory).IsChecked;
        public List<FileType> FileTypes => FilterTypes.Any(t => t.IsChecked) ? FilterTypes.Where(ft => ft.Type != MSItemType.All && ft.Type != MSItemType.Directory && ft.IsChecked).Select(ft => ft.FileType).ToList() : null;
        public bool CanPaste { get => canPaste; set { canPaste = value; NotifyPropertyChanged("CanPaste"); } }
        private OrderModel selectedOrder => Orders.FirstOrDefault(o => o.IsChecked) ?? Orders[0];
        private List<long> ancestorIds { get { var result = new List<long>(); if (Parent != null) result.Add((long)Parent.Id!); return result; } }
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
        private HashSet<Key> pressedKeys = new HashSet<Key>();
        private List<BaseEntityViewModel> selectedItems => Items.Where(i => i.IsSelected).ToList();
        private static HashSet<BaseEntityViewModel> itemsToCopyMove = new HashSet<BaseEntityViewModel>();
        private bool isCopying = false;
        private bool isMoving = false;
        public BaseEntityViewModel? LastSelectedItem => lastClickedItem;
        public void KeyDown(Key key, BaseEntityViewModel? entity = null)
        {
            pressedKeys.Add(key);
            if (key == Key.A && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))) SelectAll();
            //if (key == Key.F2 && lastClickedItem != null) BeginRenaming();
            else if (key == Key.Escape) cancelAll();
            else if (key == Key.Delete)
            {
                selectedItems.ForEach(i => { if (i is FileViewModel) (i as FileViewModel)?.FreeResources(); });
                if (entity is FileViewModel) (entity as FileViewModel)?.FreeResources();
                DeleteEntityDialog diag = new DeleteEntityDialog(selectedItems.With(entity));
                if (diag.ShowDialog() ?? false)
                {
                    Items.RemoveWhere(e => diag.Successful.Contains(e));
                }
            }
            else if (key == Key.Enter)
            {
                if (isRenaming) StoppedRenaming(lastClickedItem);
                else
                {
                    if (entity is FileViewModel)
                    {
                        try
                        {
                            var pi = new ProcessStartInfo { UseShellExecute = true, FileName = (entity as FileViewModel)!.FullPath, Verb = "Open" };
                            System.Diagnostics.Process.Start(pi);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show($"Error while tying to open file [{entity.FullPath}].\n{e.Message}");
                        }
                    }
                    else if (entity is DirectoryViewModel)
                    {
                        Parent = entity as DirectoryViewModel;
                    }
                }
            }
            else if (key == Key.C && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                BeginCopy(entity ?? lastClickedItem);
            else if (key == Key.X && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                BeginMove(entity ?? lastClickedItem);
            else if (key == Key.V && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                CommitCopyMove((entity as DirectoryViewModel) ?? parent);
            else if (key == Key.F2)
            {
                Edit(entity ?? lastClickedItem);
            }
            else if (key == Key.F5) _ = filterData();
        }
        public void BeginCopy(BaseEntityViewModel? entity)
        {
            var selected = selectedItems;
            if (entity != null) selected.Add(entity);
            if (selected.Count == 0) return;
            isCopying = true;
            isMoving = false;
            itemsToCopyMove.ToList().ForEach(i => i.Background = new SolidColorBrush(Colors.Transparent));
            itemsToCopyMove = selected.ToHashSet();
            itemsToCopyMove.ToList().ForEach(i => i.Background = (Application.Current.Resources["Primary"] as SolidColorBrush)!);
            if (selected.Count > 0) CanPaste = true;

        }
        public void BeginMove(BaseEntityViewModel? entity)
        {
            var selected = selectedItems;
            if (entity != null) selected.Add(entity);
            if (selected.Count == 0) return;
            isCopying = false;
            isMoving = true;
            itemsToCopyMove.ToList().ForEach(i => i.Background = new SolidColorBrush(Colors.Transparent));
            itemsToCopyMove = selected.ToHashSet();
            itemsToCopyMove.ToList().ForEach(i => i.Background = (Application.Current.Resources["Primary"] as SolidColorBrush)!);
            if (selected.Count > 0) CanPaste = true;

        }
        public void UndoCopy()
        {

        }
        public void UndoMove()
        {

        }
        public async void CommitCopyMove(DirectoryViewModel? entity)
        {
            if (itemsToCopyMove.Count == 0) return;
            var p = entity ?? Parent;


            var dirs = itemsToCopyMove.Where(s => s is DirectoryViewModel).Cast<DirectoryViewModel>().ToList();
            var files = itemsToCopyMove.Where(s => s is FileViewModel).Cast<FileViewModel>().ToList();
            var dids = dirs.Select(d => (long)d.Id!).ToList();
            var fids = files.Select(d => (long)d.Id!).ToList();
            var fs = new List<FileViewModel>();
            var ds = new List<DirectoryViewModel>();
            foreach (var d in dirs)
            {
                //ds.Add(new DirectoryViewModel
                //{
                //    Name = d.Name,
                //    Description = d.Description,
                //    Tags = d.Tags,
                //    Children = d.Children,
                //    Files = d.Files,
                //    OriginalPath = d.FullPath,
                //    Parent = entity,
                //    ParentId = parent?.Id,
                //    IsHidden = d.IsHidden,
                //    OnDeskName = d.OnDeskName
                //});
                var newd = d.FullPath.GetFullDirectory(Parent).directory;
                newd.Tags = d.Tags;
                ds.Add(newd);
            }
            foreach (var f in files)
            {
                //fs.Add(new FileViewModel
                //{
                //    Name = f.Name,
                //    Description = f.Description,
                //    Tags = f.Tags,
                //    OriginalPath = f.FullPath,
                //    Parent = entity,
                //    ParentId = parent?.Id,
                //    IsHidden = f.IsHidden,
                //    OnDeskName = f.OnDeskName,
                //    Extension = f.Extension,
                //    FileType = f.FileType,
                //});
                var newf = f.FullPath.GetFile(Parent).file;
                newf.Tags = f.Tags;
                fs.Add(newf);
            }
            var diag = new CopyMoveProcessDialog(ds, fs, isMoving);
            diag.ShowDialog();
            if (isMoving)
            {

                await new FileRepository().DeleteInvalidReference(fids);
                await new DirectoryRepository().DeleteInvalidReference(dids);
            }
            var moved = itemsToCopyMove.ToList();
            itemsToCopyMove.Clear();
            CanPaste = false;
            await filterData();
            Items.Where(i =>moved.Any(m => i.Name == m.Name && i.Description == m.Description && i.OnDeskName.Contains(m.OnDeskName))).ToList().ForEach(i => i.IsSelected = true) ;

        }
        private void cancelAll()
        {
            CancelRenaming(lastClickedItem);
            try { foreach (var i in Items.ToList()) if (i.IsSelected) i.IsSelected = false; } catch (Exception) { }
            lastClickedItem = null;
        }
        public void KeyUp(Key key)
        {
            pressedKeys.Remove(key);
        }
        private BaseEntityViewModel? lastClickedItem;
        public void SelectAll()
        {
            foreach (var item in Items) if (!item.IsSelected) item.IsSelected = true;
        }
        public void Edit(BaseEntityViewModel? entity)
        {
            var b = entity ?? lastClickedItem;
            if (b == null) return;
            var items = selectedItems.With(b);
            var diag = new EditEntitiesDialog(b, items);
            if (diag.ShowDialog() ?? false)
            {
                items.ForEach(async i => await i.Refresh());
            }
        }

        public void SelectItem(BaseEntityViewModel clickedItem)
        {
            if (Interlocked.Read(ref isWorking) != 0) return;
            if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) && lastClickedItem != null && Items.Contains(lastClickedItem) && Items.Contains(lastClickedItem))
            {
                var index1 = Items.IndexOf(clickedItem);
                var index2 = Items.IndexOf(lastClickedItem);
                var min = Math.Min(index1, index2);
                var max = Math.Max(index1, index2);
                for (int i = 0; i < Items.Count; i++)
                {
                    if ((i < min || i > max) && Items[i].IsSelected) Items[i].IsSelected = false;
                    else if (i >= min && i <= max && !Items[i].IsSelected) Items[i].IsSelected = true;
                }
                return;
            }
            else if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                clickedItem.IsSelected = !clickedItem.IsSelected;
                if (!clickedItem.IsSelected) return;
            }
            else
            {
                var its = Items.ToList();
                try
                {
                    foreach (var item in its) if (item != clickedItem && item.IsSelected) item.IsSelected = false;
                    if (clickedItem != null) clickedItem.IsSelected = !clickedItem.IsSelected;
                }
                catch { }
            }
            lastClickedItem = clickedItem;

        }
        public void Reset()
        {
            this.FilterTypes.ForEach(ft => ft.IsChecked = true);
            this.Orders.ForEach(o => o.IsChecked = o.Order == FileOrder.Name);
            this.Name = "";
            this.CurrentFolderOnly = true;
            this.Tags.Clear();
            itemsToCopyMove.Clear();
            isCopying = false;
            isMoving = false;
            isRenaming = false;
            this.FilterDescription = false;
            this.FilterFileName = false;
            this.Parent = null;
            _ = filterData();
        }
        public void HandleDelete(BaseEntityViewModel? entity)
        {
            var selected = Items.Where(i => i.IsSelected).ToList();
            if (entity != null && !selected.Contains(entity)) selected.Add(entity);
            if (selected.Count == 0) return;
            selected.ForEach(s => { if (s is FileViewModel) (s as FileViewModel)?.FreeResources(); });
            DeleteEntityDialog diag = new DeleteEntityDialog(selectedItems.With(entity));
            if (diag.ShowDialog() ?? false)
            {
                Items.RemoveWhere(e => diag.Successful.Contains(e));
            }
        }
        private bool isRenaming = false;
        private Visibility loadingVisibility = Visibility.Collapsed;
        private bool canPaste = false;
        private ScrollBarVisibility verticalScrollVisibility = ScrollBarVisibility.Auto;
        private string onDeskSize = "";
        private string onDeskSize1 = "";

        public void BeginRenaming(BaseEntityViewModel? entity)
        {
            if (entity != null) lastClickedItem = entity;
            isRenaming = true;
        }
        public void StoppedRenaming(BaseEntityViewModel? entity)
        {
            if (entity != null && entity.NameChanged)
            {

                entity.CommitName();
                isRenaming = false;
            }

        }
        public void CancelRenaming(BaseEntityViewModel? entity)
        {

            entity?.RestoreName();
            isRenaming = false;
        }

        public void GoBack()
        {
            DirectoryViewModel? dir;
            var poped = history.TryPop(out dir);
            if (!poped) return;
            var peek = history.TryPeek(out dir);
            if (!peek) CanGoBack = false;
            Parent = peek ? dir : null;
        }
        public void GoHome()
        {
            history.Clear();
            Parent = null;
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
            _ = filterData();
        }
        private void orderChanged(object f, PropertyChangedEventArgs args)
        {
            NotifyPropertyChanged("Order");
            _ = filterData();
        }




        private async Task filterData()
        {
            Interlocked.Increment(ref isWorking);
            CancellationTokenSource? old;
            var last = _cancellationTokens.TryPop(out old);
            if (last && old != null)
            {
                old.Cancel();
            }
            LoadingVisibility = Visibility.Visible;
            var source = new CancellationTokenSource();
            var token = source.Token;

            _cancellationTokens.Push(source);
            if (token.IsCancellationRequested) return;
            var numOfItems = 0;
            Items.Clear();
            if (IncludeFolders)
            {

                var df = DirectoryFilter;
                List<MSDirecotry> directories;
                int p = 0;
                int l = 30;
                do
                {
                    if (token.IsCancellationRequested) return;

                    df.Page = p;
                    df.Limit = l;
                    p++;
                    if (l < 100) l += 50;
                    directories = await new DirectoryRepository().FilterDirectories(df);
                    numOfItems += directories.Count;
                    numberOfItems = numOfItems;
                    NotifyPropertyChanged("NumberOfItems");
                    foreach (var dir in directories)
                    {
                        if (token.IsCancellationRequested) return;
                        var d = dir.ToDirectoryViewModel();
                        Items.Add(d);
                        _ = Task.Run(async () => { await getItemsCount(d, token); d.LoadOnDeskSize(); });
                    }
                } while (directories != null && directories.Count > 0);

                if (OnlyFolders)
                {
                    lastClickedItem = null;
                    numberOfItems = numOfItems;
                    NotifyPropertyChanged("NumberOfItems");
                    return;
                }
            }
            if (token.IsCancellationRequested) return;
            var ff = FileFilter;
            List<MSFile> files;
            int page = 0;
            int limit = 30;
            do
            {
                if (token.IsCancellationRequested) return;
                ff.Page = page;
                ff.Limit = limit;
                page++;
                if (limit < 100) limit += 50;
                files = await new FileRepository().FilterFiles(ff);
                foreach (var file in files)
                {
                    if (token.IsCancellationRequested) return;
                    Items.Add(file.ToFileViewModel());
                }
                numOfItems += files.Count;
                NumberOfItems = numOfItems;
            } while (files != null && files.Count > 0);


            lastClickedItem = null;

            numberOfItems = numOfItems;
            NotifyPropertyChanged("NumberOfItems");
            old?.Dispose();
            _cancellationTokens.TryPop(out old);
            old?.Dispose();
            LoadingVisibility = Visibility.Collapsed;
            Interlocked.Exchange(ref isWorking, 0);
        }
        private async Task getItemsCount(DirectoryViewModel dir, CancellationToken token)
        {
            if (token.IsCancellationRequested) return;
            await dir.LoadNumberOfItems();

        }
        public void Clear()
        {
            CancellationTokenSource? source;

            while (_cancellationTokens.TryPop(out source))
            {
                source.Cancel();
                source.Dispose();
                source = null;
            }
        }
        public MainViewModel()
        {
            FilterTypes.ForEach(ft => { ft.PropertyChanged += filterTypesChanged!; });
            Orders.ForEach(o => { o.PropertyChanged += orderChanged!; });
            _ = filterData();
        }
        public MainViewModel(string name, string tagName, bool filterFileName,
            bool filterDescription, bool currentFolderOnly,
            bool currentFolderRecursive, bool allFolders,
            bool showHidden, DirectoryViewModel parent,
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




        public MainViewModel Clone => new MainViewModel(
            Name, TagName, FilterFileName, FilterDescription, CurrentFolderOnly, CurrentFolderRecursive, AllFolders, ShowHidden, Parent, FilterTypes, Orders, Tags);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
