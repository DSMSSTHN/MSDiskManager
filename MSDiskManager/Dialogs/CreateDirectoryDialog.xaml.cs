using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using MSDiskManagerData.Helpers;
using System;
using System.Collections.Generic;
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

namespace MSDiskManager.Dialogs
{
    /// <summary>
    /// Interaction logic for CreateDirectoryDialog.xaml
    /// </summary>
    public partial class CreateDirectoryDialog : Window
    {
        public CreateDirectoryViewModel Model { get; } = new CreateDirectoryViewModel();
        Action<DirectoryEntity> DirectoryCreatedCallback;
        private readonly DirectoryEntity parentDirectory;
        public CreateDirectoryDialog(Action<DirectoryEntity> directoryCreatedCallback,DirectoryEntity parentDirectory = null )
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
                var dir = await rep.CreateDirectory(parentDirectory?.Id, Model.Name, Model.Description);
                foreach (var tag in Model.Tags)
                {
                    await rep.AddTag(dir.Id,tag.Id);
                }
                LoadingLabel.Visibility = Visibility.Collapsed;
                DirectoryCreatedCallback(dir);
                this.Close();
            } else
            {
            MessageBox.Show("Cannot create a Directory with an empty name");

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
            NameTBX.Focus();
        }
    }
}
