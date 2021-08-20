using MSDiskManager.Dialogs;
using MSDiskManager.Helpers;
using MSDiskManager.Pages.AddItems;
using MSDiskManager.Pages.Main;
using MSDiskManagerData.Data;
using MSDiskManagerData.Data.Repositories;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MSDiskManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private bool dialogIsShown = false;
        public AppSettings Settings { get; set; } = null;
        public MainWindow()
        {
            InitializeComponent();
            ToolTipService.ShowDurationProperty.OverrideMetadata(
    typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.MouseLeftButtonDown += delegate { DragMove(); };
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //#if DEBUG
            //            var appSettings = await AppSettings.GetLastAppSettings();
            //            if(appSettings == null)
            //            {
            //                    appSettings = new AppSettings { };
            //                    appSettings.Save();
            //                var fromDb = await new DriveRepository().GetAll();
            //                if(fromDb == null || fromDb.Count == 0)
            //                {
            //                    var driver = new MSDiskManagerData.Data.Entities.MSDrive { Letter = "D", DriverUUID = Guid.NewGuid().ToString(),DriverId = "sdasda",PNPDriverId="sdadsa"};
            //                    driver = await appSettings.AddDriver(driver);
            //                    MSDM_DBContext.SetDrive(driver);
            //                } else
            //                {
            //                    var driver = fromDb[0];
            //                    driver = await appSettings.AddDriver(driver);
            //                    MSDM_DBContext.SetDrive(driver);
            //                }
            //            } else
            //            {
            //                MSDM_DBContext.SetDriver(appSettings.Drives[0]);
            //            }
            //            this.AppSettings = appSettings;
            //#else
            //            await checkDriver();
            //            checkConnection(null, null);
            //            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            //            _timer.Tick += checkConnection;
            //            _timer.Start();
            //#endif
            //            MainWindowFrame.NavigationService.Navigate(new MainPage());
            //MainWindowFrame.NavigationService.Navigate(new MainPage());
        }

        //private void checkConnection(object state, EventArgs args)
        //{

        //    if (dialogIsShown) return;
        //    var connectionState = MSDM_DBContext.ConnectionState;
        //    if (connectionState != System.Data.ConnectionState.Open)
        //    {
        //        var diag = new ConnectionErrorDialog();
        //        dialogIsShown = true;
        //        Application.Current.Dispatcher.Invoke(() =>
        //        {
        //            diag.ShowDialog();
        //            dialogIsShown = false;
        //        });
        //    }

        //}
        //private async Task checkDriver()
        //{
        //    try
        //    {
        //        var settings = await AppSettings.GetLastAppSettings();
        //        if (settings == null || !(await settings.ConnectToDriver()))
        //        {
        //            var diag = new StartInfo();
        //            diag.ShowDialog();
        //            if (AppSettings == null)
        //            {
        //                Close();
        //            }
        //            return;
        //        }
        //        else
        //        {
        //            this.AppSettings = settings;
        //            await settings.ConnectToDriver();
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        MessageBox.Show($"Error while trying to access Application data.\nError:[{e.Message}]");
        //        this.Close();
        //    }
        //}

        private void NewConnectionClicked(object sender, RoutedEventArgs e)
        {
            var diag = new StartInfo(AppSettings.StoredAppSettings);
            diag.ShowDialog();

            //(this.MainWindowFrame.Content as MainPage)?.Model.Reset();
        }

        //private async void EditCurrentConnectionClicked(object sender, RoutedEventArgs e)
        //{

        //    var diag = new StartInfo(AppSettings ?? await AppSettings.GetLastAppSettings());
        //    diag.ShowDialog();

        //    (this.MainWindowFrame.Content as MainPage)?.Model.Reset();
        //}

        //private void ChooseDriveClicked(object sender, RoutedEventArgs e)
        //{
        //    if (AppSettings == null) throw new Exception("App settings was null");
        //    var diag = new Drivesettings(AppSettings);
        //    diag.ShowDialog();


        //    (this.MainWindowFrame.Content as MainPage)?.Model.Reset();
        //}
    }
}
