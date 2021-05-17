using MSDiskManager.Controls;
using MSDiskManager.Dialogs;
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
        public MainViewModel Model { get; set; } = new MainViewModel();
        //public FilterTopViewModel TopFilterModel = new FilterTopViewModel();
        public MainPage()
        {
            this.DataContext = Model;
            InitializeComponent();
            Application.Current.MainWindow.Closed += (a, b) => { Model?.Clear(); };
            //this.FilterTopViewModel.PropertyChanged += (a, f) => { this.UpdateLayout(); };
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var bottom = new FilesFoldersList(Model, (a, b, c) => this.NavigationService.Navigate(new AddItemsPage(a, b, c)));
            BottomControl.Content = bottom;
            this.NavigationService.FragmentNavigation += (a,e)=>{
                object content = ((ContentControl)e.Navigator).Content;
                if(content is MainPage)Model.Parent = Model.Parent;
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var addPage = new AddItemsPage(Model.Parent);
            this.NavigationService.Navigate(addPage);
        }
        private void OpenTagsDialog(object sender, RoutedEventArgs e)
        {
            var diag = new SelectTagsWindow(Model.Tags.Select(t => (long)t.Id).ToList(), (tag) => Model.AddTag(tag));
            var success = diag.ShowDialog();
            if (success != null && (bool)success)
            {

            }
        }

        private void DeleteTag(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var id = (long)button.CommandParameter;
            Model.RemoveTag(id);
        }
        private HashSet<Key> pressedKeys = new HashSet<Key>();
        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            pressedKeys.Add(e.Key);
            if(e.Key == Key.F && (pressedKeys.Contains(Key.LeftCtrl) || pressedKeys.Contains(Key.RightCtrl)))
            {
                NameFilterTXTBX.Focus();
                NameFilterTXTBX.SelectAll();
            }
        }

        private void Page_KeyUp(object sender, KeyEventArgs e)
        {
            pressedKeys.Remove(e.Key);
        }

        private void TypeChecked(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
