using MSDiskManager.Helpers;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management;
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
    /// Interaction logic for Drivesettings.xaml
    /// </summary>
    public partial class Drivesettings : Window
    {
        public ObservableCollection<string> Drives = new ObservableCollection<string>();
        private string selectedDrive = "";
        public Drivesettings()
        {
            var letters = MSDeskIdentifier.DriveLetters;
            Drives.AddMany(letters);
            selectedDrive = Drives[0];

            InitializeComponent();
        }


        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            DriverComboBox.ItemsSource = Drives;
            
            DriverComboBox.SelectedIndex = 0;
        }

        private void DriverComboBox_Selected(object sender, RoutedEventArgs e)
        {
            selectedDrive = (sender as ComboBox).SelectedItem.ToString();
        }

        private async void ChooseClicked(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text;
            if (name == null || name.Trim().Length == 0)
            {
                showMessage("Name can't be null!!");
                return;
            }
            var idFile = selectedDrive + name + ".msdm";
            if (File.Exists(idFile))
            {
                showMessage("Drive with this id File already exits!!");
                return;
            }
            var uuid = Guid.NewGuid().ToString();
            var msDrive = new MSDrive { Id = uuid, Name = name };
            try
            {
                if ((await new DriveRepository().AddDriver(msDrive)) == null)
                {
                    showMessage("Couldn't add drive to database!!");
                    return;
                }
                File.WriteAllText(idFile, uuid);
                this.Close();
            }
            catch (Exception ex)
            {
                showMessage(ex.Message);
            }

        }
        private void showMessage(string msg)
        {
            ChooseButton.IsEnabled = false;
            var txt = new TextBlock();
            txt.Text = msg;
            ChooseButton.Content = txt;
            Task.Run(async () =>
            {
                await Task.Delay(1500);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    txt.Text = "Choose";
                    ChooseButton.IsEnabled = true;
                });
            });
        }
        private void CancelClicked(object sender, RoutedEventArgs e)
        {
                this.Close();
        }
    }
}
