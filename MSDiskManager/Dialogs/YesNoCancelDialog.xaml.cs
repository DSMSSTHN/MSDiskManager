using MSDiskManager.Helpers;
using MSDiskManager.Pages.AddItems;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data;
using MSDiskManagerData.Data.Repositories;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for YesNoCancelDialog.xaml
    /// </summary>
    public partial class YesNoCancelDialog : Window
    {
        private DirectoryViewModel distanation;
        private readonly string[] pathes;
        private List<DirectoryViewModel> dirs = new List<DirectoryViewModel>();
        private List<FileViewModel> files = new List<FileViewModel>();
        private List<string> newToAdd = new List<string>();



        public YesNoCancelDialog(DirectoryViewModel distanation, string[] pathes)
        {
            this.distanation = distanation;
            this.pathes = pathes;
            InitializeComponent();
        }

        private void NoClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
            (Application.Current.MainWindow as MainWindow).MainWindowFrame.NavigationService.Navigate(
                new AddItemsPage(pathes, distanation));
        }

        private async void YesClicked(object sender, RoutedEventArgs e)
        {

            YesNoCancelBorder.IsEnabled = false;
            await addItemsToDB();
            this.DialogResult = newToAdd.Count == 0;
            this.Close();
            if (newToAdd.Count > 0)
            {
                (Application.Current.MainWindow as MainWindow).MainWindowFrame.NavigationService.Navigate(
                                new AddItemsPage(newToAdd.ToArray(), distanation));
            }
        }
        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private async Task addItemsToDB()
        {
            var letter = MSDM_DBContext.DriverName.ToLower().First();
            var addToDB = new HashSet<string>();
            foreach (var path in pathes)
            {
                if (path.ToLower().First() != letter)
                {
                    newToAdd.Add(path); continue;
                }
                else addToDB.Add(path.Substring(0, path.LastIndexOf("\\")));
                if (path.IsFile())
                {
                    var f = path.GetFile();
                    files.Add(f.file);
                }
                else
                {
                    var ds = path.GetFullDirectory();
                    dirs.Add(ds.directory);
                }
            }
            addToDB = addToDB.Where(d => d.LastIndexOf("\\") > 2).ToHashSet();
            var ods = new List<DirectoryViewModel>();
            var fRep = new FileRepository();
            var dRep = new DirectoryRepository();
            foreach (var a in addToDB)
            {
                ods.Add((await dRep.AddPathToDB(a)).ToDirectoryViewModel());
            }
            var filesFlat = files.Select(f => { var result = f.FileEntity; result.Path = result.OldPath.Substring(3);return result; }).ToList();
            var dirsFlat = dirs.Select(d => { var result = d.DirectoryEntity; result.Path = result.OldPath.Substring(3); return result; }).ToList();
            dirs.ForEach(d =>
            {
                filesFlat.AddRange(d.FilesRecursive.Select(f => { var result = f.FileEntity; result.Path = result.OldPath.Substring(3); return result; }));
                dirsFlat.AddRange(d.ChildrenRecursive.Select(c => { var result = c.DirectoryEntity; result.Path = result.OldPath.Substring(3); return result; }));
            });
            var (newFiles, newDires) = await dRep.AddToDBAsIs(filesFlat, dirsFlat,true);


        }
    }
}
