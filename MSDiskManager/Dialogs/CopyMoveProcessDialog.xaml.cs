using MSDiskManager.Helpers;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Repositories;
using MSDiskManagerData.Helpers;
using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for CopyMoveProcessDialog.xaml
    /// </summary>
    
    public partial class CopyMoveProcessDialog : Window
    {
        public CopyMoveProcessVM Model {get;}



        public CopyMoveProcessDialog(List<DirectoryViewModel> dirs, List<FileViewModel> files, bool move)
        {
            Model = new CopyMoveProcessVM(dirs, files, move, () => { if (DialogResult == null) { DialogResult = false; Close(); } }, () => { if (DialogResult == null) { DialogResult = true; Close(); } });
            this.DataContext = Model;
            Closed += (a, b) => { Model.Clear(); };
           
            InitializeComponent();

        }



        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.MouseLeftButtonDown += delegate { DragMove(); };
            await Model.Start();


        }
       
        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            Model.Cancel();
        }

        private void SkipClicked(object sender, RoutedEventArgs e)
        {

        }

        private void RetryClicked(object sender, RoutedEventArgs e)
        {

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

        private async void RowRetryClicked(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button.CommandParameter as BaseEntityViewModel;
            await Model.Retry(item);

        }

        private async void RowSkipClicked(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button.CommandParameter as BaseEntityViewModel;
            Model.Skip(item);
        }

        private void FailureDB_Loaded(object sender, RoutedEventArgs e)
        {
            (sender as DataGrid).ItemsSource = Model.CurrentFails;
        }
    }
    public enum CopyMoveEventType
    {
        Success,
        Skip,
        Cancel
    }
}
