using MSDiskManager.Dialogs;
using MSDiskManager.Helpers;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MSDiskManager.Pages.AddItems
{
    /// <summary>
    /// Interaction logic for AddItemsPage.xaml
    /// </summary>
    public partial class AddItemsPage : Page
    {
        public AddItemsViewModel Model { get; } = new AddItemsViewModel();

        private List<DirectoryViewModel> dirs = new List<DirectoryViewModel>();
        private List<FileViewModel> files = new List<FileViewModel>();
        private List<string> currentPathes { get; set; } = new List<string>();
        private DirectoryViewModel currentDirectory;
        private List<BaseEntityViewModel> toRemove = new List<BaseEntityViewModel>();
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
            Model.PropertyChanged += (a, args) =>
            {
                var name = (args as PropertyChangedEventArgs).PropertyName;
                var model = (a as AddItemsViewModel);
                if (name == "FilesOnly")
                {
                    if (model.FilesOnly)
                    {
                        currentDirectory = null;
                        BackButton.Background = Application.Current.Resources["primary"] as SolidColorBrush;
                        var files = this.files.ToList();
                        foreach (var d in dirs)
                        {
                            files.AddRange(d.FilesRecursive);
                        }
                        Model.Reset();
                        var res = files.OrderBy(f => f.Name).ToList();
                        Model.Items.AddMany(res);
                    }
                    else
                    {
                        Model.Reset();
                        Model.AddEntities(dirs);
                        Model.AddEntities(files);
                    }
                }
            };
            InitializeComponent();

        }

        private void AddTags(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entity = button.CommandParameter as BaseEntityViewModel;
            var diag = new SelectTagsWindow(entity.Tags.Select(t => (long)t.Id).ToList(), (tag) => { entity.Tags.Add(tag); Model.SelectedItems.ForEach(i => { if (!i.Tags.Contains(tag))i.Tags.Add(tag); }); }, true);
            diag.ShowDialog();
        }

        private void DeleteTag(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entity = button.CommandParameter as BaseEntityViewModel;
            var tag = button.DataContext as Tag;
            entity.Tags.Remove(tag);
            Model.SelectedItems.ForEach(i => { if (i.Tags.Contains(tag)) i.Tags.Remove(tag); });
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


        private void openDirectory(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dir = button.CommandParameter as DirectoryViewModel;
            if (dir == null)
            {
                var file = button.CommandParameter as FileViewModel;
                if (file != null) openFile(file.OriginalPath);
                return;
            }
            currentDirectory = dir;
            Model.Reset();
            dir.Files.ForEach(async f => await f.LoadImage());
            Model.AddEntities(dir.Children);
            Model.AddEntities(dir.Files);
            BackButton.Background = new SolidColorBrush(Colors.White);
        }
        private void GoBack(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (currentDirectory == null) return;//MessageBox.Show("there are no more parent directories to go back to");
            else
            {
                currentDirectory = currentDirectory.Parent;
                Model.Reset();
                Model.AddEntities(currentDirectory == null ? files.ToList() : currentDirectory.Files);
                if (!Model.FilesOnly) Model.AddEntities(currentDirectory == null ? dirs.ToList() : currentDirectory.Children);
                if (currentDirectory == null) BackButton.Background = Application.Current.Resources["primary"] as SolidColorBrush;
            }
        }

        private void MoveClicked(object sender, RoutedEventArgs e)
        {
            CopyMove(true);
        }

        private void CopyClicked(object sender, RoutedEventArgs e)
        {
            CopyMove();
        }
        private void CopyMove(bool move = false)
        {
            Model.Reset();
            var fo = Model.FilesOnly;
            var fs = this.files.ToList();
            List<DirectoryViewModel> ds;
            if (fo)
            {
                foreach (var d in dirs) fs.AddRange(d.FilesRecursive);
                foreach (var f in fs) { f.Parent = null; f.FreeResources(); }
                ds = new List<DirectoryViewModel>();
            }else
            {
                ds = dirs.ToList();
                foreach (var f in fs) f.FreeResources();
                foreach (var d in ds)d.FreeResources();
            }
            
            var diag = new CopyMoveDialog(fo ? new List<DirectoryViewModel>() : ds, fs, Model.Distanation, move);
            if (diag?.ShowDialog() ?? false)
            {
                this.NavigationService.GoBack();
            }
            else
            {
                removeSuccessful();

                files.ForEach(f => { f.Parent = null; f.ResetResources(); });
                dirs.ForEach(d => { d.Parent = null;d.ResetResources(); });
                if (Model.FilesOnly) { Model.FilesOnly = false; }
                else
                {
                    Model.Reset();
                    Model.AddEntities(dirs);
                    Model.AddEntities(files);
                }
            }

        }
        private void removeSuccessful()
        {
            files = files.Where(f => !f.ShouldRemove).ToList();
            dirs = dirs.Where(d => !d.StartRemoving()).ToList();
            Model.Reset(dirs);
            Model.AddEntities(files);
            files.ForEach(f => f.ShouldRemove = false);
            dirs.ForEach(d => { d.ResetShouldRemove(); d.Parent = null; });
            var pathes = files.Select(f => f.OriginalPath).ToList();
            pathes.AddRange(dirs.SelectMany(d => d.OriginalPathesRecursive));
            currentPathes = pathes;
        }


        private void Page_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                addFiles((string[])e.Data.GetData(DataFormats.FileDrop), currentDirectory);
                AddBorder.Background = Application.Current.Resources["primary"] as SolidColorBrush;
            }
        }
        private void addFiles(string[] pathes, DirectoryViewModel parent = null)
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
                        if (parent == null) files.Add(f.file);
                        else parent.Files.Add(f.file);
                        toadd.Add(f.path);
                    }
                    else
                    {
                        var ds = path.GetFullDirectory(parent);
                        if (parent == null) dirs.Add(ds.directory);
                        else parent.Children.Add(ds.directory);
                        toadd.AddRange(ds.pathes);
                    }
                }
                this.currentPathes.AddRange(toadd);
                if (parent == null)
                {
                    Model.Reset();
                    Model.AddEntities(dirs);
                    Model.AddEntities(files);
                }
                else
                {
                    if (parent == currentDirectory)
                    {
                        Model.Reset();
                        Model.AddEntities(parent.Children);
                        Model.AddEntities(parent.Files);
                    }
                }
            }

        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }




        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            AddDG.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            var button = sender as Button;
            var entity = button.CommandParameter as BaseEntityViewModel;
            if (entity is DirectoryViewModel)
            {
                button.ToolTip = entity.TooltipContent;
            }
            else
            {

                var file = entity as FileViewModel;
                switch (file.FileType)
                {
                    case FileType.Unknown:
                        break;
                    case FileType.Text:
                        if (button.ToolTip == null) button.ToolTip = file.TextContent;
                        break;
                    case FileType.Image:
                        if (button.ToolTip == null) button.ToolTip = file.ImageContent;
                        break;
                    case FileType.Music:
                        var audio = file.AudioContent;
                        audio.Play();
                        break;
                    case FileType.Video:
                        var video = file.VideoContent;
                        if (button.ToolTip == null) button.ToolTip = video;
                        video.Play();
                        break;
                    case FileType.Compressed:
                        break;
                    case FileType.Document:
                        break;
                }
            }
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            AddDG.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            var button = sender as Button;
            var entity = button.CommandParameter as BaseEntityViewModel;
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
                        button.ToolTip = null;
                        break;
                    case FileType.Music:
                        file.StopPlaying();
                        break;
                    case FileType.Video:
                        file.StopPlaying();
                        button.ToolTip = null;
                        break;
                    case FileType.Compressed:
                        break;
                    case FileType.Document:
                        break;
                }
            }
        }
        private void Button_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            var button = sender as Button;
            var entity = button.CommandParameter as BaseEntityViewModel;
            if (entity is FileViewModel)
            {
                var file = entity as FileViewModel;
                if (e.Delta < 0) file.MouseWheelDown();
                else if (e.Delta > 0) file.MouseWheelUp();
            }

        }

        private void DeleteItem(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entity = button.CommandParameter as BaseEntityViewModel;
            doDelete(entity);
        }
        private void doDelete(BaseEntityViewModel entity)
        {
            if (entity.Parent == null)
            {
                if (entity is FileViewModel) files.Remove(entity as FileViewModel);
                else dirs.Remove(entity as DirectoryViewModel);

            }
            else
            {
                entity.Parent.RemoveChild(entity);
            }
            Model.RemoveEntity(entity);
            currentPathes.RemoveAll(p => p.Contains(entity.OriginalPath));
        }

        private void StackPanel_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var stack = sender as StackPanel;
                var dir = stack.DataContext as DirectoryViewModel;
                if (dir != null)
                {
                    e.Handled = true;

                    addFiles((string[])e.Data.GetData(DataFormats.FileDrop), dir);
                    AddBorder.Background = Application.Current.Resources["primary"] as SolidColorBrush;
                }
            }
        }

        private void HandleDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (sender is StackPanel && (sender as StackPanel).DataContext is DirectoryViewModel || sender is Page)
                {
                    var brush = (((string[])e.Data.GetData(DataFormats.FileDrop))?.All(s => currentPathes.Contains(s)) ?? false) ? Application.Current.Resources["warnColor"] as SolidColorBrush
                        : Application.Current.Resources["lightBlue900"] as SolidColorBrush;
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
                    else AddBorder.Background = Application.Current.Resources["primary"] as SolidColorBrush;

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

        private void DataGridRow_Selected(object sender, RoutedEventArgs e)
        {
            var row = sender as DataGridRow;
            var entity = row.DataContext as BaseEntityViewModel;
            entity.IsSelected = true;
        }

        private void DataGridRow_Unselected(object sender, RoutedEventArgs e)
        {
            var row = sender as DataGridRow;
            var entity = row.DataContext as BaseEntityViewModel;
            entity.IsSelected = false;
        }

        private void AddPage_Loaded(object sender, RoutedEventArgs e)
        {
            FilterTBX.Focus();
            FilterTBX.SelectAll();
            this.PreviewKeyDown += (a, r) => Model.KeyDown(r.Key);
            this.PreviewKeyUp += (a, r) => Model.KeyUp(r.Key);
        }

        private void NameGotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            var entity = textBox.DataContext as BaseEntityViewModel;
        }

        private void NameLostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            var entity = textBox.DataContext as BaseEntityViewModel;
            if (entity?.NameChanged ?? false)
            {
                entity?.CommitName();
                //Model.CommitName(entity.Name);
            }
        }

        private void NamePKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            var entity = textBox.DataContext as BaseEntityViewModel;
            if(e.Key == Key.Enter)
            {
                if (entity.NameChanged)
                {
                    entity.CommitName();
                    //Model.CommitName(entity.Name);
                }
            } else if (e.Key == Key.Escape)
            {
                entity.RestoreName();
            }
        }

      

        private void DeleteClickeds(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var entity = mi.DataContext as BaseEntityViewModel;
            if (entity == null) return;
            var rm = Model.HandleDelete(entity);
            files = files.Where(f => !rm.Contains(f)).ToList();
            dirs = dirs.Where(d => !rm.Contains(d)).ToList();
        }

        private void EditClickeds(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var entity = mi.DataContext as BaseEntityViewModel;
            if (entity == null) return;
            Model.Edit(entity);
        }

        private void DontAddToDbChecked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var entity = checkbox.DataContext as BaseEntityViewModel;
            Model.SelectedItems.ForEach(i => { if (!i.IgnoreAdd) i.IgnoreAdd = true; });
        }

        private void DontAddToDbUnchecked(object sender, RoutedEventArgs e)
        {
            Model.SelectedItems.ForEach(i => { if (i.IgnoreAdd) i.IgnoreAdd = false; });
        }

        private void DataGridRow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var row = sender as DataGridRow;
            var entity = row.DataContext as BaseEntityViewModel;
            Model.SelectEntity(entity);
        }
    }
}
