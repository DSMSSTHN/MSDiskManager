using MSDiskManager.Helpers;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using MSDiskManagerData.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MSDiskManager.Dialogs.SelectTagsDialog
{
    /// <summary>
    /// Interaction logic for AddTagPage.xaml
    /// </summary>
    public partial class AddTagPage : Page, INotifyPropertyChanged
    {
        private int color = new Random().Next(20);
        private string name = "";
        private bool isFavourite = false;
        private int rating = 0;
        private CancellationTokenSource cancels;

        public int Color { get => color; set { color = value; Changed(); } }
        public string ItemName
        {
            get => name; set
            {
                if (value.Trim().Length > 0)
                {
                    try
                    {
                        var titleVal = value.ToTitleCase();
                        if (titleVal != value.Trim())
                        {
                            ItemName = titleVal;
                            return;
                        }
                    }
                    catch { }
                }
                name = value;
                Changed();
                checkName(value);
            }
        }
        public bool IsFavourite { get => isFavourite; set { isFavourite = value; Changed(); } }
        public int Rating { get => rating; set { rating = value; Changed(); } }

        public List<int> Colors { get; set; } = new List<int>(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });

        private Action<Tag> tagAddedFunc;
        public AddTagPage(string name, Action<Tag> tagAddedFunc)
        {
            this.ItemName = name;
          this.tagAddedFunc = tagAddedFunc;
            InitializeComponent();
        }





        private async void checkName(string name)
        {
            if (await new TagRepository().TagExists(name))
            {
                NameTextBox.Foreground = Brushes.Red;
            }
            else
            {
                NameTextBox.Foreground = Brushes.White;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

      

        private void ColorClicked(object sender, RoutedEventArgs e)
        {
            var num = (sender as Button).DataContext as int?;
            if (num == null) return;
            Color = num!.Value;
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).PreviewKeyDown -= listenToKeys;
            this.NavigationService.GoBack();

        }

        private async void AddClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var tag = await new TagRepository().AddTag(name, Color);
                tagAddedFunc?.Invoke(tag);
                Window.GetWindow(this).PreviewKeyDown -= listenToKeys;
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                var x = ex;
                var msg = x.Message;
                while (x.InnerException != null)
                {
                    x = x.InnerException;
                    msg += ".\n     " + x.Message;
                }
                MSMessageBox.Show(msg);
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            NameTextBox.Focus();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).PreviewKeyDown += listenToKeys;
            
        }
        private void listenToKeys(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { NavigationService.GoBack(); Window.GetWindow(this).PreviewKeyDown -= listenToKeys; e.Handled = true; }
            if (e.Key == Key.Enter) { AddClicked(null, null); e.Handled = true; }
        
            
        }
    }
}
