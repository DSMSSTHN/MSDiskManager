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
        private string _name = "";
        public string TagName { get=>_name; set { _name = value; _ = checkName(value); } }
        public int TagColor { get; set; } = 0;
        public Visibility ErrorVisibility { get; set; } = Visibility.Hidden;
        public string Error { get; set; } = "";
        public static List<int> AllColors { get; set; }
        private Action<Tag> tagAddedFunc;
        public AddTagPage(string name, Action<Tag> tagAddedFunc)
        {
            this.tagAddedFunc = tagAddedFunc;
            initColors();
            this._name = name;
            this.DataContext = this;
            InitializeComponent();
        }
        private async Task checkName(string name)
        {
            
            if (Globals.IsNullOrEmpty(name))
            {
                showEmptyError();
                return;
            }
            if(await new TagRepository().TagExists(name))
            {
                showExistsError();
                return;
            }
            hideError();

        }

        private async Task addTag()
        {
            await new TagRepository().AddTag(_name, TagColor);
        }
        private void showEmptyError()
        {
            this.ErrorVisibility = Visibility.Visible;
            this.Error = "tag name shouldn't be empty";
        }
        private void showExistsError()
        {
            this.ErrorVisibility = Visibility.Visible;
            this.Error = "tag name already exits";
        }
        private void showError(string error)
        {
            this.ErrorVisibility = Visibility.Visible;
            this.Error = error;
        }
        private void hideError()
        {
            this.ErrorVisibility = Visibility.Hidden;
            this.Error = "";
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
                var tag = await rep.AddTag(_name, TagColor);
                this.tagAddedFunc(tag);
                this.NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                showError(ex.Message);
            }
        }
        private void initColors()
        {
            var result = new List<int>();
            for (int i = 0; i < 11; i++) result.Add(i);
            AllColors = result;
        }

        private void ChangeColor(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var num = (int)button.CommandParameter;
            TagColor = num;
        }
    }
}
