using MSDiskManager.Helpers;
using MSDiskManager.ViewModels;
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
    /// Interaction logic for CopyMoveExistsHandler.xaml
    /// </summary>
    public partial class CopyMoveExistsHandler : Window
    {
        public ObservableCollection<BaseEntityViewModel> Items = new ObservableCollection<BaseEntityViewModel>();
        private Action<BaseEntityViewModel> skipAction;
        private List<BaseEntityViewModel> selected
        {
            get
            {
                var s = Items.Where(i => i.IsSelected).ToList();
                if (s.Count > 0) return s;
                var existsArr = new object[ExistsDG.SelectedItems.Count];
                var arr = new object[ExistsDG.Items.Count];
                ExistsDG.SelectedItems.CopyTo(existsArr, 0);
                ExistsDG.Items.CopyTo(arr, 0);
                var lst = arr.ToList();
                var indexes = existsArr.Select(e => lst.IndexOf(e)).ToList();
                return indexes.Select(i => Items[i]).ToList();
            }
        }
        public CopyMoveExistsHandler(ICollection<BaseEntityViewModel> items, Action<BaseEntityViewModel> skipAction)
        {
            foreach (var i in items) i.IsSelected = false;
            this.Items = items.ToObservableCollection();
            this.skipAction = skipAction;

            InitializeComponent();
            this.ExistsDG.ItemsSource = this.Items;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.MouseLeftButtonDown += delegate { DragMove(); };
        }

        private void RenameSelectedClicked(object sender, RoutedEventArgs e)
        {
            var s = selected;
            if (Globals.IsNotNullNorEmpty(s))
            {
                Items.RemoveWhere(i => s.Contains(i));
            }
            checkClose();
        }

        private async void ReplaceSelectedClicked(object sender, RoutedEventArgs e)
        {
            var s = selected;
            if (Globals.IsNotNullNorEmpty(s))
            {
                var fRep = new FileRepository();
                var dRep = new DirectoryRepository();
                foreach (var i in s)
                {
                    try
                    {

                        if (i is FileViewModel) { File.Delete(@i.FullPath); await fRep.DeletePerPath(i.Path); }
                        else { Directory.Delete(@i.FullPath, true); await dRep.DeletePerPath(i.Path); ; }
                        Items.Remove(i);
                    }
                    catch (Exception ex)
                    {
                        MSMessageBox.Show($"Item:[{i.FullPath}] cannot be remove.\nError:[{ex.Message}]");
                    }
                }
            }
            checkClose();
        }

        private void SkipSelectedClicked(object sender, RoutedEventArgs e)
        {
            var s = selected;
            if (Globals.IsNotNullNorEmpty(s))
            {
                s.ForEach(e => skipAction(e));
                Items.RemoveWhere(i => s.Contains(i));
            }
            checkClose();
        }

        private void CancelOperationClicked(object sender, RoutedEventArgs e)
        {
            if (this.DialogResult == null)
            {
                this.DialogResult = false;
                this.Close();
            }
        }
        private void checkClose()
        {
            if (Items.Count == 0)
            {
                if (this.DialogResult == null)
                {
                    this.DialogResult = true;
                    this.Close();
                }
            }
        }
    }
}
