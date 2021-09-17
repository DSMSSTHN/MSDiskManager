using MSDiskManager.Helpers;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using MSDiskManagerData.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;
using System.Threading;
using System.ComponentModel;

namespace MSDiskManager.Dialogs
{
    /// <summary>
    /// Interaction logic for CopyMoveDialog.xaml
    /// </summary>
    public partial class CopyMoveDialog : Window
    {
        public CopyMoveDiagViewModel Model { get; set; } = new CopyMoveDiagViewModel();
        private List<DirectoryViewModel> dirs;
        private List<FileViewModel> files;
        private bool move;


        public CopyMoveDialog(List<DirectoryViewModel> dirs, List<FileViewModel> files, DirectoryViewModel parentDirectory, bool move)
        {
            this.DataContext = Model;
            this.dirs = dirs.ToList();
            this.files = files.ToList();
            var count = dirs?.Count ?? 0;
            if (dirs != null) foreach (var d in dirs) count += d.DirectoryCountRecursive;
            Model.DirectoryCount = count;
            count = files?.Count ?? 0;
            if (dirs != null) foreach (var d in dirs) count += d.FileCountRecursive;
            Model.FileCount = count;
            this.Model.Parent = parentDirectory;
            this.move = move;
            Model.MCString = move ? "move" : "copy";
            Model.HeaderContent = $"There are {Model.DirectoryCount} Directories and {Model.FileCount} Files to {Model.MCString}.\n choose Destination";
            InitializeComponent();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.MouseLeftButtonDown += delegate { DragMove(); };
        }

        private void CloseButtonClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void OpenFolder(object sender, MouseButtonEventArgs e)
        {
            var row = sender as DataGridRow;
            var dir = row.DataContext as DirectoryViewModel;
            if (dir != null)
            {
                Model.Parent = dir;
            }
        }

        private async void GoBack(object sender, RoutedEventArgs e)
        {
            if (Model.Parent == null)
            {
                MSMessageBox.Show("you're at the root directory. you can't go back anymore");
                return;
            }
            if (Model.Parent.ParentId == null)
            {
                Model.Parent = null;
                return;
            }
            Model.CanGoBack = false;
            Model.Directories.Clear();
            Model.Parent = (await new DirectoryRepository().GetDirectory((long)Model.Parent.ParentId))?.ToDirectoryViewModel();
        }

        private void AddDirectory(object sender, RoutedEventArgs e)
        {
            var diag = new CreateDirectoryDialog((dir) => Model.Parent = dir, Model.Parent);
            diag.ShowDialog();
        }


        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Proceed(object sender, RoutedEventArgs e)
        {
            var parent = Model.Parent == null ? null : new DirectoryViewModel { Path = Model.Parent.Path, Id = Model.Parent.Id,Name = Model.Parent.Name,OnDeskName = Model.Parent.OnDeskName };
            foreach (var f in files) f.Parent = parent;
            foreach (var d in dirs) d.Parent = parent;
            var diag = new CopyMoveProcessDialog(dirs, files, move);
            this.DialogResult = diag.ShowDialog();
            this.Close();

        }
    }
}
