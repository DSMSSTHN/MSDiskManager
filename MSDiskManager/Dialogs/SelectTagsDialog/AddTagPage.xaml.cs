using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using MSDiskManagerData.Helpers;
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

namespace MSDiskManager.Dialogs.SelectTagsDialog
{
    /// <summary>
    /// Interaction logic for AddTagPage.xaml
    /// </summary>
    public partial class AddTagPage : Page
    {
        public AddTagViewModel Model { get; } = new AddTagViewModel();
        public static List<int> AllColors { get; set; }
        private Action<Tag> tagAddedFunc;
        public AddTagPage(string name, Action<Tag> tagAddedFunc)
        {
            this.DataContext = Model;
            this.tagAddedFunc = tagAddedFunc;
            this.Model.Name= name;
            InitializeComponent();
        }
        private async Task checkName(string name)
        {
            
            if (Globals.IsNullOrEmpty(name))
            {
                MessageBox.Show("tag name shouldn't be empty");
                return;
            }
            if(await new TagRepository().TagExists(name))
            {
                MessageBox.Show("tag name already exits");
                return;
            }

        }

        private async Task addTag()
        {
            await new TagRepository().AddTag(Model.Name, Model.Color);
        }
  
       

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }

        private async void AddClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var rep = new TagRepository();
                var tag = await rep.AddTag(Model.Name, Model.Color);
                this.tagAddedFunc(tag);
                this.NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var num = (int)button.CommandParameter ;
            Model.Color = num;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            NameTBX.Focus();
            NameTBX.SelectAll();
        }
    }
}
