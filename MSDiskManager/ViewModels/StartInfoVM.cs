using MSDiskManager.Helpers;
using MSDiskManagerData.Data;
using MSDiskManagerData.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MSDiskManager.ViewModels
{
    public class StartInfoVM : INotifyPropertyChanged
    {
        
        private bool connectionValid = false;
        private CancellationTokenSource cancels;
        private string hostName = "";
        private string port = "";
        private string userName = "";
        private string password = "";
        private string databaseName = "";

        public bool ConnectionValid { get => connectionValid; set { connectionValid = value; NotifyPropertyChanged("ConnectionValid");} }
        public Visibility ConnectionErrorVisibility { get; set; } = Visibility.Visible;
        public Visibility ConnectionVisibility { get; set; } = Visibility.Collapsed;
        public string HostName { get => hostName; set { hostName = value; NotifyPropertyChanged("ConnectionValid"); checkConnection(); }
}
        public string Port { get => port; set { port = value; NotifyPropertyChanged("ConnectionValid"); checkConnection(); } }
        public string UserName { get => userName; set { userName = value; NotifyPropertyChanged("ConnectionValid"); checkConnection(); } }
        public string Password { get => password; set { password = value; NotifyPropertyChanged("ConnectionValid"); checkConnection(); } }
        public string DatabaseName { get => databaseName; set { databaseName = value; NotifyPropertyChanged("ConnectionValid"); checkConnection(); } }




        public String ConnectionString => $"host={hostName};port={port};database={databaseName};username={userName};password={password};timeout=0;";

        private void checkConnection()
        {
            if (Globals.IsNoneNullOrEmpty(hostName, port, userName, password, databaseName))
            {
                try
                {
                    MSDM_DBContext.SetConnectionString(ConnectionString.Replace(databaseName,"postgres"));
                    var state = MSDM_DBContext.ConnectionState;
                    Console.WriteLine(state.ToString());
                    ConnectionValid = state == System.Data.ConnectionState.Open;
                }
                catch (Exception)
                {
                    ConnectionValid = false;
                }
            }
            if (connectionValid)
            {
                ConnectionErrorVisibility = Visibility.Collapsed; ConnectionVisibility = Visibility.Visible;
            } else
            {
                ConnectionErrorVisibility = Visibility.Visible; ConnectionVisibility = Visibility.Collapsed;
            }
        }

        public StartInfoVM()
        {
            
        }

        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
    public class MSDriversInfo : INotifyPropertyChanged
    {
        private string driverLetter;
        private string deviceId;
        private string pNPDeviceId;
        private bool isSelected;

        public String DriverLetter { get => driverLetter; set { driverLetter = value; NotifyPropertyChanged("DriverLetter"); } }
        public String DeviceId
        {
            get => deviceId; set { deviceId = value; NotifyPropertyChanged("DeviceId"); }
        }
        public String PNPDeviceId
        {
            get => pNPDeviceId; set { pNPDeviceId = value; NotifyPropertyChanged("PNPDeviceId"); }
        }
        public bool IsSelected { get => isSelected; set { isSelected = value; NotifyPropertyChanged("IsSelected"); } }



        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
