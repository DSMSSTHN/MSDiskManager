using MSDiskManager.Dialogs;
using MSDiskManager.Helpers;
using MSDiskManager.Pages.Main;
using MSDiskManagerData.Data;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MSDiskManager.Pages
{
    /// <summary>
    /// Interaction logic for SelectDrivePage.xaml
    /// </summary>
    public partial class SelectDrivePage : Page
    {
        private bool connectionValid = false;
        private string dbName = "";
        private Timer checkConnectionTimer;
        private ObservableCollection<MSDrive> Drives = new ObservableCollection<MSDrive>();
        public SelectDrivePage()
        {

            InitializeComponent();
        }



        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {


            DrivesListView.ItemsSource = Drives;
            await loadDrives();
            ConnectionStatusText.Text =
                connectionValid ? $"Connected to {dbName}" : "No Valid Connection";
            while (!connectionValid)
            {
                AddButton.IsEnabled = false;
                NoValidConnectionBorder.Visibility = Visibility.Visible;
                if (!Application.Current.Windows.OfType<StartInfo>().Any())
                {
                    var diag = new StartInfo();
                    diag.ShowDialog();
                    await loadDrives();
                }
            }
            checkConnectionTimer = new Timer();
            checkConnectionTimer.Elapsed += (a, b) => { checkConnection(); };
            checkConnectionTimer.Interval = 3000;
            checkConnectionTimer.Start();
        }


        private async void AddDriveClicked(object sender, RoutedEventArgs e)
        {
            if (!connectionValid) return;
            var diag = new Drivesettings();
            diag.ShowDialog();
            await Task.Delay(500);
            _ = loadDrives();

        }
        private void DriveSelected(object sender, RoutedEventArgs e)
        {
            var drive = (sender as Button).CommandParameter as MSDrive;
            selectDrive(drive);

        }
        private async Task loadDrives()
        {
            Drives.Clear();
            var appSettings = AppSettings.StoredAppSettings;
            if (appSettings == null) { connectionValid = false; return; }
            dbName = appSettings.DBName;
            MSDM_DBContext.SetConnectionString(appSettings.ConnectionString);
            if (MSDM_DBContext.ConnectionState != System.Data.ConnectionState.Open) { connectionValid = false; return; }
            connectionValid = true;
            var drives = MSDeskIdentifier.MSDrives;
            if (drives == null || drives.Count == 0) return;
            var ids = drives.Select(d => File.ReadAllText(d.MSID_Path)).ToList();
            var rep = new DriveRepository();
            var dbDrives = await rep.LoadDrives(ids);
            for (int i = 0; i < ids.Count; i++)
            {
                var dbd = dbDrives.FirstOrDefault(d => d.Id == ids[i]);
                if (dbd != null) dbd.Letter = drives[i].Letter;
            }
            Drives.Clear();
            Drives.AddMany(dbDrives);
            AddButton.IsEnabled = true;
            NoValidConnectionBorder.Visibility = Visibility.Collapsed;


        }
        private async void checkConnection()
        {
            connectionValid = MSDM_DBContext.ConnectionState == System.Data.ConnectionState.Open;
            if (connectionValid) return;
            checkConnectionTimer.Stop();
            Application.Current.Dispatcher.Invoke(() =>
            {
                AddButton.IsEnabled = false;
                NoValidConnectionBorder.Visibility = Visibility.Visible;
            });
            while (!connectionValid)
            {
                await Task.Delay(1000);
                connectionValid = MSDM_DBContext.IsConnectionValid;
            }
            checkConnectionTimer.Start();
            Application.Current.Dispatcher.Invoke(() => { _ = loadDrives(); });
        }
        private void selectDrive(MSDrive drive)
        {
            MSDM_DBContext.SetDrive(drive);
            this.NavigationService.Navigate(new MainPage());

        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            checkConnectionTimer?.Stop();
            checkConnectionTimer.Close();
        }
    }
}
