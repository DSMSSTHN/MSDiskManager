using MSDiskManager.Controls;
using MSDiskManager.Dialogs;
using MSDiskManager.Helpers;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Helpers;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;

namespace MSDiskManager.Pages.AddItems
{
    /// <summary>
    /// Interaction logic for AddItemsPage.xaml
    /// </summary>
    public partial class AddItemsPage : Page
    {
        public AddItemsViewModel Model { get; } = new AddItemsViewModel();

        private List<string> currentPathes { get; set; } = new List<string>();
        private List<BaseEntityViewModel> toRemove = new List<BaseEntityViewModel>();
        private PauseTokenSource pauseLoading = new PauseTokenSource();
        private CancellationTokenSource cancelLoading;
        public AddItemsPage(object sender, DragEventArgs e, DirectoryViewModel initialDist = null) : this(initialDist)
        {
            this.Page_Drop(sender, e);
        }
        public AddItemsPage(DirectoryViewModel initialDist = null)
        {
            this.DataContext = Model;
            if (initialDist != null)
            {
                Model.Distanation = initialDist;
            }
            Model.PropertyChanged += async (a, args) =>
            {
                var name = (args as PropertyChangedEventArgs).PropertyName;
                var model = (a as AddItemsViewModel);
                if (name == "Model.FilesOnly")
                {
                    if (model.FilesOnly)
                    {
                        model.CurrentDirectory = null;
                        BackButton.Background = Application.Current.Resources["Primary"] as SolidColorBrush;
                        var fs = this.Model.Files.ToList();
                        foreach (var d in Model.Dirs)
                        {
                            Model.Files.AddRange(d.FilesRecursive);
                        }
                        Model.Reset();
                        var res = Model.Files.OrderBy(f => f.Name).ToList();
                        Model.Items.AddMany(res);
                    }
                    else
                    {
                        Model.Reset();
                        await Model.AddEntities(Model.Dirs.OrderBy(d => d.Name), false, pauseLoading);
                        await Model.AddEntities(Model.Files.OrderBy(f => f.Name), false, pauseLoading);
                    }
                }
            };
            InitializeComponent();

        }

        private void AddTags(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entity = button.CommandParameter as BaseEntityViewModel;
            var diag = new SelectTagsWindow(entity.Tags.Select(t => (long)t.Id).ToList(), (tag) => {
                //Model.SelectedItems.ToList().ForEach(i => { if (!i.Tags.Contains(tag)) i.Tags.Add(tag); });
            }, true);
            diag.ShowDialog();
        }

        private void DeleteTag(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entity = button.CommandParameter as BaseEntityViewModel;
            var tag = button.DataContext as Tag;
            entity.Tags.Remove(tag);
            Model.SelectedItems.ToList().ForEach(i => { if (i.Tags.Contains(tag)) i.Tags.Remove(tag); });
        }

        private void OpenOriginalPath(object sender, MouseButtonEventArgs e)
        {
            var button = sender as Button;
            var str = button.CommandParameter.ToString();
            if (str.IsFile()) openFile(str);
            else openExplorerDirectory(str);

        }
        private void openFile(string path)
        {
            try
            {
                var pi = new ProcessStartInfo { UseShellExecute = true, FileName = path, Verb = "Open" };
                System.Diagnostics.Process.Start(pi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while tying to open file [{path}].\n{ex.Message}");
            }
        }
        private void openExplorerDirectory(string path)
        {
            try
            {
                var filePath = System.IO.Path.GetFullPath(path);
                System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while tying to open file [{path}].\n{ex.Message}");
            }
        }

        private void checkAllDirectoriesClicked(object sender, RoutedEventArgs e)
        {
            var check = sender as CheckBox;
            var isChecked = check.IsChecked ?? false;
            foreach (var f in Model.Items) f.IsSelected = isChecked;
        }


        private async void OpenDirectory(object sender, MouseButtonEventArgs e)
        {
            var button = sender as Button;
            var dir = button.CommandParameter as DirectoryViewModel;
            if (dir == null)
            {
                var file = button.CommandParameter as FileViewModel;
                if (file != null) openFile(file.OriginalPath);
                return;
            }
            Model.CurrentDirectory = dir;
            Model.Reset();
            dir.Files.ForEach(async f => await f.LoadImage());
            await Model.AddEntities(dir.Children.OrderBy(d => d.Name), false, pauseLoading);
            await Model.AddEntities(dir.Files.OrderBy(d => d.Name), false, pauseLoading);
            BackButton.Background = new SolidColorBrush(Colors.White);
        }
        private async void GoBack(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (Model.CurrentDirectory == null) return;//MessageBox.Show("there are no more parent directories to go back to");
            else
            {
                Model.CurrentDirectory = Model.CurrentDirectory.Parent;
                Model.Reset();
                if (!Model.FilesOnly) await Model.AddEntities(
                    (Model.CurrentDirectory == null ? Model.Dirs.ToList() : Model.CurrentDirectory.Children).OrderBy(f => f.Name), false
                    , pauseLoading);
                await Model.AddEntities(
                    (Model.CurrentDirectory == null ? Model.Files.ToList() : Model.CurrentDirectory.Files).OrderBy(d => d.Name), false
                    , pauseLoading);
                
                if (Model.CurrentDirectory == null) BackButton.Background = Application.Current.Resources["Primary"] as SolidColorBrush;
            }
        }

        private async void MoveClicked(object sender, RoutedEventArgs e)
        {
            await CopyMove(true);
        }

        private async void CopyClicked(object sender, RoutedEventArgs e)
        {
            await CopyMove();
        }
        private async Task CopyMove(bool move = false)
        {
            Model.Reset();
            var fo = Model.FilesOnly;
            var fs = this.Model.Files.ToList();
            List<DirectoryViewModel> ds;
            if (fo)
            {
                foreach (var d in Model.Dirs) fs.AddRange(d.FilesRecursive);
                foreach (var f in fs) { f.Parent = null; f.FreeResources(); }
                ds = new List<DirectoryViewModel>();
            }
            else
            {
                ds = Model.Dirs.ToList();
                foreach (var f in fs) f.FreeResources();
                foreach (var d in ds) d.FreeResources();
            }

            var diag = new CopyMoveDialog(fo ? new List<DirectoryViewModel>() : ds, fs, Model.Distanation, move);
            if (diag?.ShowDialog() ?? false)
            {
                this.NavigationService.GoBack();
            }
            else
            {
                await removeSuccessful();

                Model.Files.ForEach(f => { f.Parent = null; f.ResetResources(); });
                Model.Dirs.ForEach(d => { d.Parent = null; d.ResetResources(); });
                if (Model.FilesOnly) { Model.FilesOnly = false; }
                else
                {
                    Model.Reset();
                    await Model.AddEntities(Model.Dirs.OrderBy(d => d.Name), false, pauseLoading);
                    await Model.AddEntities(Model.Files.OrderBy(f => f.Name), false, pauseLoading);
                }
            }

        }
        private async Task removeSuccessful()
        {
            Model.Files = Model.Files.Where(f => !f.ShouldRemove).ToList();
            Model.Dirs = Model.Dirs.Where(d => !d.StartRemoving()).ToList();
            Model.Reset(Model.Dirs);
            await Model.AddEntities(Model.Files.OrderBy(f => f.Name), false, pauseLoading);
            Model.Files.ForEach(f => f.ShouldRemove = false);
            Model.Dirs.ForEach(d => { d.ResetShouldRemove(); d.Parent = null; });
            var pathes = Model.Files.Select(f => f.OriginalPath).ToList();
            pathes.AddRange(Model.Dirs.SelectMany(d => d.OriginalPathesRecursive));
            currentPathes = pathes;
        }


        private async void Page_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                await addFiles((string[])e.Data.GetData(DataFormats.FileDrop), Model.CurrentDirectory);
                AddBorder.Background = Application.Current.Resources["Primary"] as SolidColorBrush;
            }
        }
        private async Task addFiles(string[] pathes, DirectoryViewModel parent = null)
        {
            var toadd = new List<string>();
            if (pathes != null)
            {
                foreach (var path in pathes)
                {
                    if (currentPathes.Contains(path)) continue;
                    if (path.IsFile())
                    {
                        var f = path.GetFile(parent);
                        if (parent == null) Model.Files.Add(f.file);
                        else parent.Files.Add(f.file);
                        toadd.Add(f.path);
                    }
                    else
                    {
                        var ds = path.GetFullDirectory(parent);
                        if (parent == null) Model.Dirs.Add(ds.directory);
                        else parent.Children.Add(ds.directory);
                        toadd.AddRange(ds.pathes);
                    }
                }
                this.currentPathes.AddRange(toadd);
                if (parent == null)
                {
                    Model.Reset();
                    await Model.AddEntities(Model.Dirs.OrderBy(d => d.Name), false, pauseLoading);
                    await Model.AddEntities(Model.Files.OrderBy(d => d.Name), false, pauseLoading);
                }
                else
                {
                    if (parent == Model.CurrentDirectory)
                    {
                        Model.Reset();
                        await Model.AddEntities(parent.Children.OrderBy(d => d.Name), false, pauseLoading);
                        await Model.AddEntities(parent.Files.OrderBy(d => d.Name), false, pauseLoading);
                    }
                }
            }

        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }




        //private void Button_MouseEnter(object sender, MouseEventArgs e)
        //{
        //    AddDG.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        //    var button = sender as Button;
        //    var entity = button.CommandParameter as BaseEntityViewModel;
        //    if (entity is DirectoryViewModel)
        //    {
        //        button.ToolTip = entity.TooltipContent;
        //    }
        //    else
        //    {

        //        var file = entity as FileViewModel;
        //        switch (file.FileType)
        //        {
        //            case FileType.Unknown:
        //                break;
        //            case FileType.Text:
        //                if (button.ToolTip == null) button.ToolTip = file.TextContent;
        //                break;
        //            case FileType.Image:
        //                if (button.ToolTip == null) button.ToolTip = file.ImageContent;
        //                break;
        //            case FileType.Music:
        //                var audio = file.AudioContent;
        //                audio.Play();
        //                break;
        //            case FileType.Video:
        //                var video = file.VideoContent;
        //                if (button.ToolTip == null) button.ToolTip = video;
        //                video.Play();
        //                break;
        //            case FileType.Compressed:
        //                break;
        //            case FileType.Document:
        //                break;
        //        }
        //    }
        //}

        //private void Button_MouseLeave(object sender, MouseEventArgs e)
        //{
        //    AddDG.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
        //    var button = sender as Button;
        //    var entity = button.CommandParameter as BaseEntityViewModel;
        //    var content = entity.TooltipContent;
        //    if (entity is DirectoryViewModel)
        //    {
        //        return;
        //    }
        //    else
        //    {
        //        var file = entity as FileViewModel;
        //        switch (file.FileType)
        //        {
        //            case FileType.Unknown:
        //                break;
        //            case FileType.Text:
        //                break;
        //            case FileType.Image:
        //                button.ToolTip = null;
        //                break;
        //            case FileType.Music:
        //                file.StopPlaying();
        //                break;
        //            case FileType.Video:
        //                file.StopPlaying();
        //                button.ToolTip = null;
        //                break;
        //            case FileType.Compressed:
        //                break;
        //            case FileType.Document:
        //                break;
        //        }
        //    }
        //}
        //private void Button_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        //{
        //    e.Handled = true;
        //    var button = sender as Button;
        //    var entity = button.CommandParameter as BaseEntityViewModel;
        //    if (entity is FileViewModel)
        //    {
        //        var file = entity as FileViewModel;
        //        if (e.Delta < 0) file.MouseWheelDown();
        //        else if (e.Delta > 0) file.MouseWheelUp();
        //    }

        //}

        //private void DeleteItem(object sender, RoutedEventArgs e)
        //{
        //    var button = sender as Button;
        //    var entity = button.CommandParameter as BaseEntityViewModel;
        //    doDelete(entity);
        //}
        private void doDelete(BaseEntityViewModel entity)
        {
            //if (entity.Parent == null)
            //{
            //    if (entity is FileViewModel) Model.Files.Remove(entity as FileViewModel);
            //    else Model.Dirs.Remove(entity as DirectoryViewModel);

            //}
            //else
            //{
            //    entity.Parent.RemoveChild(entity);
            //}
            Model.HandleDelete(entity);
            currentPathes.RemoveAll(p => p.Contains(entity.OriginalPath));
        }

        //private void StackPanel_Drop(object sender, DragEventArgs e)
        //{
        //    if (e.Data.GetDataPresent(DataFormats.FileDrop))
        //    {
        //        var stack = sender as StackPanel;
        //        var dir = stack.DataContext as DirectoryViewModel;
        //        if (dir != null)
        //        {
        //            e.Handled = true;

        //            addModel.Files((string[])e.Data.GetData(DataFormats.FileDrop), dir);
        //            AddBorder.Background = Application.Current.Resources["Primary"] as SolidColorBrush;
        //        }
        //    }
        //}

        private void HandleDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (sender is StackPanel && (sender as StackPanel).DataContext is DirectoryViewModel || sender is Page)
                {
                    var brush = (((string[])e.Data.GetData(DataFormats.FileDrop))?.All(s => currentPathes.Contains(s)) ?? false) ? Application.Current.Resources["WarnRed"] as SolidColorBrush
                        : Application.Current.Resources["MSBlue"] as SolidColorBrush;
                    e.Handled = true;
                    if (sender is StackPanel) (sender as StackPanel).Background = brush;
                    else AddBorder.Background = brush;

                }
            }
        }

        private void HandleDragLeave(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (sender is StackPanel && (sender as StackPanel).DataContext is DirectoryViewModel || sender is Page)
                {
                    e.Handled = true;
                    if (sender is StackPanel) (sender as StackPanel).Background = new SolidColorBrush(Colors.Transparent);
                    else AddBorder.Background = Application.Current.Resources["Primary"] as SolidColorBrush;

                }
            }
        }

        private void AddDG_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var dg = sender as DataGrid;
            //if (dg.ItemsSource is ICollection<BaseEntityViewModel>)
            //{
            //    if (e.Key == Key.Delete)
            //    {
            //        var items = dg.ItemsSource as ICollection<BaseEntityViewModel>;
            //        var selected = items?.Where(i => i.IsSelected)?.ToList();
            //        if (!Globals.IsNullOrEmpty(selected))
            //        {
            //            foreach (var item in selected) doDelete(item);
            //        }
            //    }
            //}
        }

        //private void DataGridRow_Selected(object sender, RoutedEventArgs e)
        //{
        //    var row = sender as DataGridRow;
        //    var entity = row.DataContext as BaseEntityViewModel;
        //    entity.IsSelected = true;
        //}

        //private void DataGridRow_Unselected(object sender, RoutedEventArgs e)
        //{
        //    var row = sender as DataGridRow;
        //    var entity = row.DataContext as BaseEntityViewModel;
        //    entity.IsSelected = false;
        //}

        private void AddPage_Loaded(object sender, RoutedEventArgs e)
        {
            FilterTBX.Focus();
            FilterTBX.SelectAll();
            this.PreviewKeyDown += (a, r) =>
            {
                if (r.Key == Key.A)
                {
                    if (Model.CurrentDirectory == null)
                    {
                        Model.Files.ForEach(f => f.IsSelected = true);
                        Model.Dirs.ForEach(f => f.IsSelected = true);
                    }
                    else
                    {
                        Model.CurrentDirectory.Children.ForEach(d => d.IsSelected = true);
                        Model.CurrentDirectory.Files.ForEach(f => f.IsSelected = true);
                    }
                }
                else Model.KeyDown(r.Key);
            };
        }

        //private void NameGotFocus(object sender, RoutedEventArgs e)
        //{
        //    var textBox = sender as TextBox;
        //    var entity = textBox.DataContext as BaseEntityViewModel;
        //}

        //private void NameLostFocus(object sender, RoutedEventArgs e)
        //{
        //    var textBox = sender as TextBox;
        //    var entity = textBox.DataContext as BaseEntityViewModel;
        //    if (entity?.NameChanged ?? false)
        //    {
        //        entity?.CommitName();
        //        //Model.CommitName(entity.Name);
        //    }
        //}

        //private void NamePKeyDown(object sender, KeyEventArgs e)
        //{
        //    var textBox = sender as TextBox;
        //    var entity = textBox.DataContext as BaseEntityViewModel;
        //    if(e.Key == Key.Enter)
        //    {
        //        if (entity.NameChanged)
        //        {
        //            entity.CommitName();
        //            //Model.CommitName(entity.Name);
        //        }
        //    } else if (e.Key == Key.Escape)
        //    {
        //        entity.RestoreName();
        //    }
        //}



        //private void DeleteClickeds(object sender, RoutedEventArgs e)
        //{
        //    var mi = sender as MenuItem;
        //    var entity = mi.DataContext as BaseEntityViewModel;
        //    if (entity == null) return;
        //    var rm = Model.HandleDelete(entity);
        //    Model.Files = Model.Files.Where(f => !rm.Contains(f)).ToList();
        //    Model.Dirs = Model.Dirs.Where(d => !rm.Contains(d)).ToList();
        //}

        //private void EditClickeds(object sender, RoutedEventArgs e)
        //{
        //    var mi = sender as MenuItem;
        //    var entity = mi.DataContext as BaseEntityViewModel;
        //    if (entity == null) return;
        //    Model.Edit(entity);
        //}

        private void DontAddToDbChecked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var entity = checkbox.DataContext as BaseEntityViewModel;
            Model.SelectedItems.ToList().ForEach(i => { if (!i.IgnoreAdd) i.IgnoreAdd = true; });
        }

        private void DontAddToDbUnchecked(object sender, RoutedEventArgs e)
        {
            Model.SelectedItems.ToList().ForEach(i => { if (i.IgnoreAdd) i.IgnoreAdd = false; });
        }

        private void DataGridRow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var row = sender as DataGridRow;
            var entity = row.DataContext as BaseEntityViewModel;
            Model.SelectEntity(entity);
        }

        private void DeleteEntityCTX(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var entity = mi.DataContext as BaseEntityViewModel;
            if (entity == null) return;
            var rm = Model.HandleDelete(entity);
            Model.Files = Model.Files.Where(f => !rm.Contains(f)).ToList();
            Model.Dirs = Model.Dirs.Where(d => !rm.Contains(d)).ToList();
        }

        private void EditEntity(object sender, RoutedEventArgs e)
        {
            BaseEntityViewModel entity = null;
            entity = ((sender as MenuItem)?.DataContext ?? (sender as Button)?.CommandParameter) as BaseEntityViewModel;
            if (entity == null) return;
            Model.Edit(entity);
        }

        private void CopyEntity(object sender, RoutedEventArgs e)
        {
            //CopyMove();
        }

        private void MoveEntity(object sender, RoutedEventArgs e)
        {
            //CopyMove(true);
        }

        private void PasteEntity(object sender, RoutedEventArgs e)
        {

        }

        private void ShowInExplorerClicked(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var entity = (mi?.DataContext as BaseEntityViewModel) ?? Model.CurrentDirectory;
            if (entity == null) return;
            string args = "/select, \"" + @entity.OriginalPath + "\"";
            System.Diagnostics.Process.Start("explorer.exe", args);
        }





        private void Grid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            var grid = sender as DBImage;
            var entity = grid.DataContext as BaseEntityViewModel;
            if (entity is FileViewModel)
            {
                var file = entity as FileViewModel;
                if (e.Delta < 0) file.MouseWheelDown();
                else if (e.Delta > 0) file.MouseWheelUp();
            }
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void DeleteEntity(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entity = button.CommandParameter as BaseEntityViewModel;
            doDelete(entity);
        }

        private void DragLeaveElement(object sender, DragEventArgs e)
        {
            var g = sender as Grid;
            if (!(g.DataContext is FileViewModel))
            {
                e.Handled = true;
                g.Background = new SolidColorBrush(Colors.Transparent);
            }
        }

        private void DragEnterElement(object sender, DragEventArgs e)
        {
            var g = sender as Grid;
            if (!(g.DataContext is FileViewModel))
            {
                e.Handled = true;
                g.Background = Application.Current.Resources["MSBlue"] as SolidColorBrush;
            }
        }

        private async void DropOnElement(object sender, DragEventArgs e)
        {
            var c = sender as Grid;
            var directory = c.DataContext as DirectoryViewModel;
            if (directory == null) return;
            e.Handled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                await addFiles((string[])e.Data.GetData(DataFormats.FileDrop), directory);
                AddBorder.Background = Application.Current.Resources["Primary"] as SolidColorBrush;
            }
        }

        private void Grid_GotFocus(object sender, RoutedEventArgs e)
        {
            var grid = sender as Grid;
            var entity = grid.DataContext as BaseEntityViewModel;
            Model.SelectEntity(entity);
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            Model.CanScroll = false;
            var dbImage = sender as DBImage;
            var entity = dbImage.DataContext as BaseEntityViewModel;
            if (entity is DirectoryViewModel)
            {
                dbImage.ToolTip = entity.TooltipContent;

            }
            else
            {
                var file = entity as FileViewModel;
                switch (file.FileType)
                {
                    case FileType.Unknown:
                        break;
                    case FileType.Text:
                        if (dbImage.ToolTip == null) dbImage.ToolTip = file.TextContent;
                        break;
                    case FileType.Image:
                        if (dbImage.ToolTip == null) dbImage.ToolTip = file.ImageContent;
                        break;
                    case FileType.Music:
                        var audio = file.AudioContent;
                        audio.Play();
                        break;
                    case FileType.Video:
                        var video = file.VideoContent;
                        if (dbImage.ToolTip == null) dbImage.ToolTip = video;
                        video.Play();
                        break;
                    case FileType.Compressed:
                        break;
                    case FileType.Document:
                        break;
                }
            }
            if (dbImage.ToolTip as Control != null) (dbImage.ToolTip as Control).Background = Application.Current.Resources["Primary"] as SolidColorBrush;
        }
        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            Model.CanScroll = true;
            var grid = sender as DBImage;
            var entity = grid.DataContext as BaseEntityViewModel;
            if (entity == null) return;
            var content = entity.TooltipContent;
            if (entity is DirectoryViewModel)
            {
                return;
            }
            else
            {
                var file = entity as FileViewModel;
                switch (file.FileType)
                {
                    case FileType.Unknown:
                        break;
                    case FileType.Text:
                        break;
                    case FileType.Image:
                        grid.ToolTip = null;
                        file.FreeResources();
                        break;
                    case FileType.Music:
                        file.StopPlaying();
                        break;
                    case FileType.Video:
                        file.StopPlaying();
                        grid.ToolTip = null;
                        break;
                    case FileType.Compressed:
                        break;
                    case FileType.Document:
                        break;
                }
            }
        }

        

        private void ItemsListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Decorator border = VisualTreeHelper.GetChild(ItemsListView, 0) as Decorator;
            ScrollViewer scrollViewer = border.Child as ScrollViewer;
            if (Model.Items.Count >= 100 && scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 20)
            {
                pauseLoading.IsPaused = false;
            }
        }
        private void EntityClicked(object sender, MouseButtonEventArgs e)
        {
            //var entity = (sender as Grid)?.DataContext as BaseEntityViewModel;
            //if (entity != null) Model.SelectEntity(entity);
            foreach (var item in Model.CurrentItems.Where(i => !Model.Items.Contains(i)))
            {
                item.IsSelected = false;
            }
        }
        private void EntityRightClicked(object sender, MouseButtonEventArgs e)
        {
            var entity = (sender as Grid)?.DataContext as BaseEntityViewModel;
            if (entity == null) return;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                Model.SelectEntity(entity);
                entity.IsSelected = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) ||Keyboard.IsKeyDown(Key.RightCtrl))
            {
                foreach (var item in Model.CurrentItems.Where(i => !Model.Items.Contains(i)))
                {
                    item.IsSelected = false;
                }
            }
            else
            {
                foreach (var item in Model.SelectedItems)
                {
                    item.IsSelected = entity == item;
                }
            }

        }

        private void ItemsListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            Decorator border = VisualTreeHelper.GetChild(ItemsListView, 0) as Decorator;
            ScrollViewer scrollViewer = border.Child as ScrollViewer;
            if (Model.CanScroll)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (Math.Sign(e.Delta) * 100));
            }
            e.Handled = true;
        }
    }
}
