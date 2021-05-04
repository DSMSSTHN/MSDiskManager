using MSDiskManager.Controls;
using MSDiskManager.Pages.AddItems;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MSDiskManager.Pages.Main
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private FilterTopViewModel filterTopViewModel;
        //public FilterTopViewModel TopFilterModel = new FilterTopViewModel();
        public MainPage()
        {
            this.filterTopViewModel = Application.Current.Resources["TopViewModel"] as FilterTopViewModel;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var top = new FiltersPage();
            var bottom = new FilesFoldersList();
            TopFrame.NavigationService.Navigate(top);
            BottomControl.Content = bottom;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var addPage = new AddItemsPage(filterTopViewModel.FilterModel.Parent);
            this.NavigationService.Navigate(addPage);
        }
    }
}
