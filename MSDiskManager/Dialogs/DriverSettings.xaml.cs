using MSDiskManager.Helpers;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data;
using MSDiskManagerData.Data.Entities;
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
    /// Interaction logic for DriverSettings.xaml
    /// </summary>
    public partial class DriverSettings : Window
    {
        public ObservableCollection<MSDriversInfo> Drivers = new ObservableCollection<MSDriversInfo>();
        private AppSettings appSettings;
        private Prop<bool> isValid = new Prop<bool>(false);
        public DriverSettings(AppSettings appSettings)
        {
            if (appSettings == null) throw new ArgumentException("Appsettings was null");
            this.appSettings = appSettings;
            Drivers = SelectDeviceInformation().ToObservableCollection();
            if (appSettings.Drivers.Count > 0)
            {
                var selected = false;
                foreach (var d in appSettings.Drivers)
                {
                    var uuidPath = d.DriverLetter[0] + ":\\" + appSettings.DBName + ".msid";
                    if (File.Exists(uuidPath) && File.ReadAllText(uuidPath) == d.DriverUUID && Drivers.Any(d2 => d2.DriverLetter.ToLower()[0] == d.DriverLetter.ToLower()[0]))
                    {
                        selected = true;
                        Drivers.First(d2 => d2.DriverLetter.ToLower()[0] == d.DriverLetter.ToLower()[0]).IsSelected = true;
                    }
                }
                if (!selected) Drivers[0].IsSelected = true;
            }
            else
            {
                Drivers[0].IsSelected = true;
            }

            InitializeComponent();
        }
        private void checkDriver()
        {
            var driver = this.Drivers.FirstOrDefault(d => d.IsSelected);
            if (driver != null)
            {
                var uuidPath = driver.DriverLetter[0] + ":\\" + appSettings.DBName + ".msdm";
                if (File.Exists(uuidPath))
                {
                    var uuid = File.ReadAllText(uuidPath);
                    if (appSettings.Drivers.Any(d => d.DriverUUID == uuid))
                    {
                        InfoTxtBlk.Text = "Driver already part of the db. click select to choose it";
                        InfoTxtBlk.Foreground = new SolidColorBrush(Colors.Green);
                        isValid.Value = true;
                        ChooseButton.IsEnabled = true;
                    }
                    else
                    {
                        InfoTxtBlk.Text = "Driver is connected to same dbname but other connection";
                        InfoTxtBlk.Foreground = new SolidColorBrush(Colors.Red);
                        isValid.Value = false;
                        ChooseButton.IsEnabled = false;
                    }
                }
                else
                {
                    InfoTxtBlk.Text = "Driver is not yet part of the db. click select to choose it";
                    InfoTxtBlk.Foreground = new SolidColorBrush(Colors.Green);
                    isValid.Value = true;
                    ChooseButton.IsEnabled = true;
                }
            }
        }

        private IEnumerable<MSDriversInfo> SelectDeviceInformation()
        {
            foreach (ManagementObject device in SelectDevices())
            {
                var deviceId = (string)device.GetPropertyValue("DeviceID");
                var pnpDeviceId = (string)device.GetPropertyValue("PNPDeviceID");
                var driveLetter = (string)SelectPartitions(device).SelectMany(SelectDisks).Select(disk => disk["Name"]).Single();

                yield return new MSDriversInfo { DriverLetter = driveLetter, DeviceId = deviceId, PNPDeviceId = pnpDeviceId, IsSelected = false};//(deviceId, pnpDeviceId, driveLetter);
            }

            static IEnumerable<ManagementObject> SelectDevices() => new ManagementObjectSearcher(
                    @"SELECT * FROM Win32_DiskDrive ").Get()
                .Cast<ManagementObject>();

            static IEnumerable<ManagementObject> SelectPartitions(ManagementObject device) => new ManagementObjectSearcher(
                    "ASSOCIATORS OF {Win32_DiskDrive.DeviceID=" +
                    "'" + device.Properties["DeviceID"].Value + "'} " +
                    "WHERE AssocClass = Win32_DiskDriveToDiskPartition").Get()
                .Cast<ManagementObject>();

            static IEnumerable<ManagementObject> SelectDisks(ManagementObject partition) => new ManagementObjectSearcher(
                    "ASSOCIATORS OF {Win32_DiskPartition.DeviceID=" +
                    "'" + partition["DeviceID"] + "'" +
                    "} WHERE AssocClass = Win32_LogicalDiskToPartition").Get()
                .Cast<ManagementObject>();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.MouseLeftButtonDown += delegate { DragMove(); };
            DriverComboBox.ItemsSource = Drivers;
           
            checkDriver();
        }

        private  void DriverComboBox_Selected(object sender, RoutedEventArgs e)
        {
            checkDriver();
        }

        private async void ChooseClicked(object sender, RoutedEventArgs e)
        {
            if (isValid.Value)
            {
            this.isValid.Value = false;
                var selected = Drivers.FirstOrDefault(d => d.IsSelected);
                if (selected == null)
                {
                    MessageBox.Show("No Driver is selected");
                    return;
                }
                var uuid = "";
                var uuidPath = selected.DriverLetter[0] + ":\\" + appSettings.DBName + ".msdm";
                if (File.Exists(uuidPath) && (uuid = File.ReadAllText(uuidPath)) != null)
                {
                    MSDriver dr = null;
                    if ((dr = appSettings.Drivers.FirstOrDefault(d => d.DriverUUID == uuid)) != null)
                    {
                        if (this.DialogResult == null)
                        {
                            this.appSettings.SelectDriver(dr);
                            this.DialogResult = true;
                            this.Close();
                        }
                    }
                    else
                    {

                        MessageBox.Show("Driver is connected to same dbname but other connection");
                        return;
                    }

                } else
                {
                    if (this.DialogResult == null)
                    {
                   await this.appSettings.AddDriver(new MSDriver { DriverLetter = selected.DriverLetter, DriverUUID = Guid.NewGuid().ToString(), PNPDriverId = selected.PNPDeviceId, DriverId = selected.DeviceId });
                        this.DialogResult = true;
                        this.Close();
                    }
                }
            }
        }
    }
}
