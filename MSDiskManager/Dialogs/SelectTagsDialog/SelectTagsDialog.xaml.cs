using MSDiskManager.Dialogs.SelectTagsDialog;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for SelectTagsDialog.xaml
    /// </summary>
    public partial class SelectTagsWindow : Window
    {
        private List<long> selectedTagIds { get; set; }
        private Action<Tag> selectTagFunction { get; set; }
        private SelectTagsMainPage _mainPage { get; set; }
        private bool allowAdd { get; set; } = false;
        public SelectTagsWindow(List<long> selectedTagIds, Action<Tag> selectTagFunction, bool allowAdd = false)
        {
            this.allowAdd = allowAdd;
            this._mainPage = new SelectTagsMainPage(selectedTagIds, selectTagFunction, allowAdd);
            
            this.selectTagFunction = selectTagFunction;
            this.selectedTagIds = selectedTagIds;
            InitializeComponent();
            this.DataContext = this;
            //this.Deactivated += (a,b) => { this.Close(); };
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            SelectDialogMainFrame.NavigationService.Navigate(_mainPage);
        }

        private void CloseClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.MouseLeftButtonDown += delegate { DragMove(); };
            
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            
        }
    }
}
