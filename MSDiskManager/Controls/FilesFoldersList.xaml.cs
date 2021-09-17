using MSDiskManager.Pages.Main;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Collections.Concurrent;
using System.Threading;
using MSDiskManager.ViewModels;
using System.Diagnostics;
using MSDiskManager.Dialogs;
using MSDiskManager.Helpers;

namespace MSDiskManager.Controls
{
    /// <summary>
    /// Interaction logic for FilesFoldersList.xaml
    /// </summary>
    public partial class FilesFoldersList : UserControl
    {

        private Action<object, DragEventArgs, DirectoryViewModel> openAdd;
        public MainViewModel Model { get; set; }
        public FilesFoldersList(MainViewModel filterModel, Action<object, DragEventArgs, DirectoryViewModel> openAdd)
        {
            this.Model = filterModel;
            this.openAdd = openAdd;
            this.DataContext = Model;
            InitializeComponent();
        }

        private void EntityClicked(object sender, MouseButtonEventArgs e)
        {
            var button = sender as Button;
            var entity = button.CommandParameter as BaseEntityViewModel;
            if (entity is FileViewModel)
            {
                handelFileClicked(entity as FileViewModel);
            }
            else if (entity is DirectoryViewModel)
            {
                handleDirectoryClicked(entity as DirectoryViewModel);

            }
        }
        private void handelFileClicked(FileViewModel file)
        {
            try
            {
                var pi = new ProcessStartInfo { UseShellExecute = true, FileName = @file.FullPath, Verb = "Open" };
                System.Diagnostics.Process.Start(pi);
            }
            catch (Exception e)
            {
                MSMessageBox.Show($"Error while tying to open file [{file.FullPath}].\n{e.Message}");
            }
        }
        private void handleDirectoryClicked(DirectoryViewModel directory)
        {
            if (!Model.CurrentFolderOnly) Model.CurrentFolderOnly = true;
            Model.Parent = directory;

        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            Model.GoBack();
        }

        private void GoHome(object sender, RoutedEventArgs e)
        {
            //Model.GoHome();
            (Window.GetWindow(this) as MainWindow)?.MainWindowFrame.GoBack();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.PreviewKeyDown += (a, r) => Model.KeyDown(r.Key);
            this.PreviewKeyUp += (a, r) => Model.KeyUp(r.Key);

            //filterTopViewModel.FilterModel.Name = "";
            //filterTopViewModel.FilterModel.Name = "";
        }

        private void DropOnGrid(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                openAdd(sender, e, Model.Parent);
            }
        }

        private void DropOnElement(object sender, DragEventArgs e)
        {
            var c = sender as Grid;
            var directory = c.DataContext as DirectoryViewModel;
            if (directory == null) return;
            e.Handled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                openAdd(sender, e, directory ?? Model.Parent);
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

        private void DragLeaveElement(object sender, DragEventArgs e)
        {
            var g = sender as Grid;
            if (!(g.DataContext is FileViewModel))
            {
                e.Handled = true;
                g.Background = new SolidColorBrush(Colors.Transparent);
            }

        }

        private void DeleteEntity(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entity = button.CommandParameter as BaseEntityViewModel;
            Model.HandleDelete(entity);
        }

        private void ListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //Model.KeyDown(e.Key);

        }

        private void ListView_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            //Model.KeyUp(e.Key);
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var txtbx = sender as TextBox;
            var entity = txtbx.DataContext as BaseEntityViewModel;
            Model.BeginRenaming(entity);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var txtbx = sender as TextBox;
            var entity = txtbx.DataContext as BaseEntityViewModel;
            Model.StoppedRenaming(entity);
        }

        private void Grid_GotFocus(object sender, RoutedEventArgs e)
        {
            var grid = sender as Grid;
            var entity = grid.DataContext as BaseEntityViewModel;
            Model.SelectItem(entity);
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.F2)
            //{
            //    var txtbx = sender as TextBox;
            //    var entity = txtbx.DataContext as BaseEntityViewModel;
            //    if (entity != null && Model.LastSelectedItem == entity)
            //    {
            //        txtbx.Focus();
            //        txtbx.SelectAll();
            //    }
            //}
        }

        private void TextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var txtbx = sender as TextBox;
            var entity = txtbx.DataContext as BaseEntityViewModel;
            Model.SelectItem(entity);
        }

        private void EditEntity(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var entity = (mi?.DataContext as BaseEntityViewModel) ?? Model.Parent;
            if (entity == null) return;
            Model.Edit(entity);
        }

        private void DeleteEntityCTX(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var entity = (mi?.DataContext as BaseEntityViewModel) ?? Model.Parent;
            if (entity == null) return;
            Model.HandleDelete(entity);
        }
        private void ShowInExplorerClicked(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var entity = (mi?.DataContext as BaseEntityViewModel) ?? Model.Parent;
            if (entity == null) return;
            string args = "/select, \"" + @entity.FullPath + "\"";

            System.Diagnostics.Process.Start("explorer.exe", args);
        }
        private void CopyEntity(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var entity = (mi?.DataContext as BaseEntityViewModel) ?? Model.Parent;
            if (entity == null) return;
            Model.BeginCopy(entity);
        }

        private void MoveEntity(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var entity = (mi?.DataContext as BaseEntityViewModel) ?? Model.Parent;
            if (entity == null) return;
            Model.BeginMove(entity);
        }

        private void PasteEntity(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var entity = (mi?.DataContext as BaseEntityViewModel) ?? Model.Parent;
            if (entity == null) return;
            Model.CommitCopyMove(entity as DirectoryViewModel);
        }

        private void CreateDirectory(object sender, RoutedEventArgs e)
        {
            var diag = new CreateDirectoryDialog((dir) => Model.Parent = dir, Model.Parent);
            diag.ShowDialog();

        }
        private void AddTagsRecursiveClicked(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var entity = (mi?.DataContext as BaseEntityViewModel) ?? Model.Parent;
            if (entity == null) return;
            var diag = new SelectTagsWindow(entity.Tags.Select(t => t.Id).Cast<long>().ToList(),
                async (tag) =>
                {
                    if (entity is FileViewModel)
                    {
                        if (!entity.Tags.Contains(tag)) { entity.Tags.Add(tag); await new FileRepository().AddTag(entity.Id, tag.Id); }
                    }
                    else
                    {
                        if (!entity.Tags.Contains(tag))
                        {
                            entity.Tags.Add(tag);
                        }
                        LoadingTxtblk.Text = "Adding tags";
                        Model.LoadingVisibility = Visibility.Visible;
                        await new DirectoryRepository().AddTagRecursive(entity.Id, tag.Id);
                        Model.LoadingVisibility = Visibility.Collapsed;
                        LoadingTxtblk.Text = "Loading...";
                    }
                }, true);
            diag.Show();
        }
        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            Model.VerticalScrollVisibility = ScrollBarVisibility.Disabled;
            var grid = sender as Grid;
            var entity = grid.DataContext as BaseEntityViewModel;
            if (entity is DirectoryViewModel)
            {
                grid.ToolTip = entity.TooltipContent;

            }
            else
            {

                var file = entity as FileViewModel;
                switch (file.FileType)
                {
                    case FileType.Unknown:
                        break;
                    case FileType.Text:
                        if (grid.ToolTip == null) grid.ToolTip = file.TextContent;
                        break;
                    case FileType.Image:
                        if (grid.ToolTip == null) grid.ToolTip = file.ImageContent;
                        break;
                    case FileType.Music:
                        var audio = file.AudioContent;
                        audio.Play();
                        break;
                    case FileType.Video:
                        var video = file.VideoContent;
                        if (grid.ToolTip == null) grid.ToolTip = video;
                        var cancels = new CancellationTokenSource();
                        video.Loaded +=async (a, b) =>
                        {
                            var player = (MediaElement)a;
                            try
                            {
                                var canSeek = false;
                                while (!canSeek)
                                {
                                    await Task.Delay(100);
                                    if (cancels.IsCancellationRequested) return;
                                    this.AppInvoke(() => { canSeek = player.NaturalDuration.HasTimeSpan; });
                                }
                                var duration = (int)player.NaturalDuration.TimeSpan.TotalMinutes;
                                this.AppInvoke(() => { player.Position = new TimeSpan(0, new Random().Next(duration), 0); });
                            }
                            catch (Exception)
                            {

                                player.Position = new TimeSpan(0);
                            }
                        };
                        video.Unloaded += (a,b) =>
                        {
                            cancels.Cancel();
                        };
                        video.Play();
                        break;
                    case FileType.Compressed:
                        break;
                    case FileType.Document:
                        break;
                }
            }
            if (grid.ToolTip as Control != null) (grid.ToolTip as Control).Background = Application.Current.Resources["Primary"] as SolidColorBrush;
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            Model.VerticalScrollVisibility = ScrollBarVisibility.Auto;
            var grid = sender as Grid;
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

        private void Grid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            var grid = sender as Grid;
            var entity = grid.DataContext as BaseEntityViewModel;
            if (entity is FileViewModel)
            {
                var file = entity as FileViewModel;
                if (e.Delta < 0) file.MouseWheelDown();
                else if (e.Delta > 0) file.MouseWheelUp();
            }
        }

        private void InheritTags(object sender, RoutedEventArgs e)
        {
            if (Model.Parent == null || Model.Parent.Tags.Count == 0) return;
            var entity = (sender as MenuItem)?.DataContext as BaseEntityViewModel;
            var selected = Model.Items.Where(i => i.IsSelected).ToList();
            if (!selected.Contains(entity)) selected.Add(entity);

            selected.ForEach(e =>
            {
                Model.Parent.Tags.ToList().ForEach(async t =>
               {
                   var fRep = new FileRepository();
                   var dRep = new DirectoryRepository();
                   if (!e.Tags.Any(et => et.Id == t.Id))
                   {
                       e.Tags.Add(t);
                   }
                   if (e.Id == null) return;
                   if (e is FileViewModel) await fRep.AddTag(e.Id.Value, t);
                   else await dRep.AddTagRecursive(e.Id.Value, t.Id.Value);

               });
            });
        }
    }


}
