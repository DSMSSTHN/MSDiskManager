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
using System.Windows.Shapes;
using System.Management;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MSDiskManager.ViewModels;
using MSDiskManager.Helpers;
using Newtonsoft.Json;
using System.IO;
using MSDiskManagerData.Helpers;
using MSDiskManagerData.Data;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;

namespace MSDiskManager.Dialogs
{
    /// <summary>
    /// Interaction logic for StartInfo.xaml
    /// </summary>
    public partial class StartInfo : Window
    {
        public StartInfoVM Model = new StartInfoVM();
        private AppSettings appSettings = null;
        public StartInfo(AppSettings appSettings = null)
        {
            this.appSettings = appSettings;
            if(appSettings != null)
            {
                Model.HostName = appSettings.HostName;
                Model.Port = appSettings.Port;
                Model.UserName = appSettings.UserName;
                Model.Password = appSettings.Password;
                Model.DatabaseName = appSettings.DBName;
            }
            this.DataContext = Model;
            InitializeComponent();
        }

        private async void CancelClicked(object sender, RoutedEventArgs e)
        {
            await (Application.Current.MainWindow as MainWindow)?.AppSettings?.ConnectToDriver();
            this.Close();
        }

        private async void ProceedClicked(object sender, RoutedEventArgs e)
        {
            
            if (!Model.ConnectionValid) return;
            (sender as Button).IsEnabled = false;
            CancelButton.IsEnabled = false;
            HostNameTxtBx.IsEnabled = false;
            PortTextBx.IsEnabled = false;
            UserNameTextBx.IsEnabled = false;
            PasswordTextBx.IsEnabled = false;
            DBNameTextBx.IsEnabled = false;
            var wasNull = this.appSettings == null;
            if (wasNull) this.appSettings = new AppSettings();
            this.appSettings.DBName = Model.DatabaseName;
            this.appSettings.HostName = Model.HostName;
            this.appSettings.Port = Model.Port;
            this.appSettings.UserName = Model.UserName;
            this.appSettings.Password = Model.Password;
            if (!wasNull)
            {
                MSDM_DBContext.SetConnectionString(this.appSettings.ConnectionString);
                this.appSettings.Drivers = await new DriverRepository().GetAll();
            }
           
           
           
            try
            {
                if ((this.appSettings.Drivers?.Count ?? 0) == 0)
                {
                    var diag = new DriverSettings(this.appSettings);
                    diag.ShowDialog();
                }
                this.appSettings.Save();
                await this.appSettings.ConnectToDriver();
                (Application.Current.MainWindow as MainWindow).AppSettings = appSettings;
                var db = new MSDM_DBContext();
                db.Database.EnsureCreated();
                if(this.DialogResult == null)
                {
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal while saving app settings.\nError:[{ex.Message}]");
                this.Close();
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
    
}
