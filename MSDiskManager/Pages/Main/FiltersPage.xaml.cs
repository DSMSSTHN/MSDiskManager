using MSDiskManager.Dialogs;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Helpers;
using NodaTime;
using System;
using System.Collections.Generic;
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

namespace MSDiskManager.Pages.Main
{
    /// <summary>
    /// Interaction logic for FiltersPage.xaml
    /// </summary>
    public partial class FiltersPage : Page
    {

        public FilterTopViewModel FilterTopViewModel { get; set; }
        private FilterModel Filter => FilterTopViewModel.FilterModel;
        public FiltersPage()
        {
            this.FilterTopViewModel = Application.Current.Resources["TopViewModel"] as FilterTopViewModel;
            this.FilterTopViewModel.PropertyChanged += (a, f) => { this.UpdateLayout(); };
            //this.DataContext = this;
            InitializeComponent();
           
        }

       
        private void OpenTagsDialog(object sender, RoutedEventArgs e)
        {
            var diag = new SelectTagsWindow(Filter.Tags.Select(t => (long)t.Id).ToList(), (tag)=>Filter.AddTag(tag));
            var success = diag.ShowDialog();
            if (success != null && (bool)success)
            {

            }
        }

        private void DeleteTag(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var id = (int)button.CommandParameter;
            Filter.Tags.Remove(Filter.Tags.FirstOrDefault(t => t.Id == id));
        }
       
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

      
    }
}
