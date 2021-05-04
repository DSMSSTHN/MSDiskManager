using MSDiskManager.Dialogs;
using MSDiskManager.Helpers;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
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

        private List<DirectoryViewModel> allDirs = new List<DirectoryViewModel>();
        private List<FileViewModel> allFiles = new List<FileViewModel>();
        private List<string> currentPathes { get; set; } = new List<string>();
        private DirectoryViewModel currentDirectory;
        private object filesLock = new object();
        private object dirLock = new object();
        private List<BaseEntityViewModel> toRemove = new List<BaseEntityViewModel>();
        public AddItemsPage(DirectoryEntity initialDist = null)
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
                        var files = allFiles.ToList();
                        foreach (var d in allDirs)
                        {
                            files.AddRange(d.FilesRecursive);
                        }
                        Model.BaseFiles = files;
                        Model.BaseDirectories = new List<DirectoryViewModel>();
                        DirectoriesLabel.Visibility = Visibility.Collapsed;
                        DirectoriesTextBox.Visibility = Visibility.Collapsed;
                        DirectoriesGrid.Visibility = Visibility.Collapsed;
                        DirectoriesRowDef.Height = GridLength.Auto;
                    }
                    else
                    {
                        Model.BaseFiles = allFiles.ToList();
                        Model.BaseDirectories = allDirs.ToList();
                        DirectoriesLabel.Visibility = Visibility.Visible;
                        DirectoriesTextBox.Visibility = Visibility.Visible;
                        DirectoriesGrid.Visibility = Visibility.Visible;
                        DirectoriesRowDef.Height = new GridLength(1, GridUnitType.Star);
                    }
                }
            };
            InitializeComponent();
        }

        private void AddTags(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entity = button.CommandParameter as BaseEntityViewModel;
            var diag = new SelectTagsWindow(entity.Tags.Select(t => (long)t.Id).ToList(), (tag) => entity.Tags.Add(tag), true);
            diag.ShowDialog();
        }

        private void DeleteTag(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entity = button.CommandParameter as BaseEntityViewModel;
            var tag = button.DataContext as Tag;
            entity.Tags.Remove(tag);
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
            foreach (var f in Model.Directories) f.IsSelected = isChecked;
        }
        private void checkAllFilesClicked(object sender, RoutedEventArgs e)
        {
            var check = sender as CheckBox;
            var isChecked = check.IsChecked ?? false;
            foreach (var f in Model.Files) f.IsSelected = isChecked;
        }

        private void openDirectory(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dir = button.CommandParameter as DirectoryViewModel;
            currentDirectory = dir;
            Model.BaseFiles = dir.Files;
            Model.BaseDirectories = dir.Children;
        }
        private void GoBack(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (currentDirectory == null) MessageBox.Show("there are no more parent directories to go back to");
            else
            {
                currentDirectory = currentDirectory.Parent;
                Model.BaseFiles = currentDirectory == null ? allFiles.ToList() : currentDirectory.Files;
                Model.BaseDirectories = currentDirectory == null ? allDirs.ToList() : currentDirectory.Children;
            }
        }

        private void MoveClicked(object sender, RoutedEventArgs e)
        {
            var fo = Model.FilesOnly;
            var files = allFiles.ToList();
            if (fo) allDirs.ForEach(d => files.AddRange(d.FilesRecursive));
            var diag = new CopyMoveDialog(fo ? new List<DirectoryViewModel>() : allDirs, files, Model.Parent, true);
            if (diag.ShowDialog() ?? false)
            {
                this.NavigationService.GoBack();
            } else
            {
                removeSuccessful();
            }
        }
        
        private void CopyClicked(object sender, RoutedEventArgs e)
        {
            var fo = Model.FilesOnly;
            var files = allFiles.ToList();
            if (fo) allDirs.ForEach(d => files.AddRange(d.FilesRecursive));
            var diag = new CopyMoveDialog(fo ? new List<DirectoryViewModel>() : allDirs, files, Model.Parent, false);
            if (diag.ShowDialog() ?? false)
            {
                this.NavigationService.GoBack();
            } else
            {
                removeSuccessful();
            }
        }
        private void removeSuccessful()
        {
            allFiles = allFiles.Where(f => !f.ShouldRemove).ToList();
            allDirs = allDirs.Where(d => !d.StartRemoving()).ToList();
            Model.Reset(allFiles, allDirs);
            allFiles.ForEach(f => f.ShouldRemove = false);
            allDirs.ForEach(d => { d.ResetShouldRemove();d.Parent = null; });
            var pathes = allFiles.Select(f => f.OriginalPath).ToList();
            pathes.AddRange(allDirs.SelectMany(d => d.OriginalPathesRecursive));
            currentPathes = pathes;
        }


        private void Page_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var toadd = new List<string>();
                string[] pathes = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (pathes != null)
                {
                    foreach (var path in pathes)
                    {
                        if (currentPathes.Any(cp => path.Contains(cp))) continue;
                        toadd.Add(path);
                        if (path.IsFile())
                        {
                            allFiles.Add(path.GetFile());
                        }
                        else
                        {
                            allDirs.Add(path.GetFullDirectory());
                        }
                    }
                    this.currentPathes.AddRange(toadd);
                    
                    Model.BaseFiles = allFiles;
                    Model.BaseDirectories = allDirs;
                }
            }
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }
    }
}
