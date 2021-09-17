using MSDiskManager.Helpers;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using MSDiskManagerData.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

namespace MSDiskManager.Dialogs
{
    /// <summary>
    /// Interaction logic for CreateDirectoryDialog.xaml
    /// </summary>
    public partial class CreateDirectoryDialog : Window
    {
        public CreateDirectoryViewModel Model { get; } = new CreateDirectoryViewModel();
        Action<DirectoryViewModel> DirectoryCreatedCallback;
        private readonly DirectoryViewModel parentDirectory;

        public CreateDirectoryDialog(Action<DirectoryViewModel> directoryCreatedCallback, DirectoryViewModel parentDirectory = null)
        {
            this.DirectoryCreatedCallback = directoryCreatedCallback;

            this.DataContext = Model;
            this.parentDirectory = parentDirectory;
            InitializeComponent();
        }



        private void AddTags(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var diag = new SelectTagsWindow(Model.Tags.Select(t => (long)t.Id).ToList(), (tag) => Model.Tags.Add(tag), true);
            diag.ShowDialog();
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void CreateFolder(object sender, RoutedEventArgs e)
        {
            if (Globals.IsNotNullNorEmpty(Model.Name))
            {
                LoadingLabel.Visibility = Visibility.Visible;
                var rep = new DirectoryRepository();
                
                var path = ((parentDirectory?.FullPath ?? (MSDM_DBContext.DriverName[0]+ ":")) + "\\" + Model.Name);
                path = path.Trim();
                var strategy = MSDM_IO.ExistsStrategy.None;
                if (Directory.Exists(path))
                {
                    var diag = new ItemExistsDialog(path);
                    diag.ShowDialog();
                    strategy = diag.ExistsStrategy;
                    if (diag.Cancel) this.Close();
                    if (strategy == MSDM_IO.ExistsStrategy.Replace)
                    {
                        try
                        {
                            Directory.Delete(path, true);
                            await rep.DeletePerPath(path);
                        }
                        catch (Exception ex)
                        {
                            MSMessageBox.Show(ex.Message);
                            diag = new ItemExistsDialog(path, false);
                            diag.ShowDialog();
                            strategy = diag.ExistsStrategy;
                        }
                    }
                }
                MSDM_IO.MSDMIO.CreateDirectory(path, strategy);
                var dir = await rep.CreateDirectory(parentDirectory?.Id, Model.Name, Model.Description);
                foreach (var tag in Model.Tags)
                {
                    await rep.AddTag(dir.Id, tag.Id);
                }
                LoadingLabel.Visibility = Visibility.Collapsed;
                DirectoryCreatedCallback(dir.ToDirectoryViewModel());
                this.Close();
            }
            else
            {
                MSMessageBox.Show("Cannot create a Directory with an empty name");

            }
        }

        private void DeleteTag(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var tag = button.DataContext as Tag;
            Model.Tags.Remove(tag);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.MouseLeftButtonDown += delegate { DragMove(); };
            this.KeyDown += (a, r) => { if (r.Key == Key.Escape) if (this.DialogResult == null) { this.DialogResult = false; this.Close(); } };
            NameTBX.Focus();
            NameTBX.SelectAll();
        }
    }
}
