using Microsoft.EntityFrameworkCore;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using MSDiskManagerData.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MSDiskManager.Helpers
{

    public class AppSettings
    {
        public string HostName { get; set; } = "";
        public string Port { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string DBName { get; set; } = "";
        [JsonIgnore]
        public string ConnectionString => $"host={HostName};port={Port};database={DBName};username={UserName};password={Password};timeout=0;";


        public bool ConnectToDriver()
        {

            MSDM_DBContext.SetConnectionString(ConnectionString);
            return MSDM_DBContext.ConnectionState == System.Data.ConnectionState.Open;
        }

        public void Save()
        {
            var settingsFile = Application.Current.GetApplicationDataFile();
            var json = JsonConvert.SerializeObject(this);
            File.WriteAllText(settingsFile, json);
        }

        public static AppSettings StoredAppSettings
        {
            get
            {
                var file = Application.Current.GetApplicationDataFile();
                if (File.Exists(file)){
                    var json = File.ReadAllText(file);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    return settings;
                }
                return null;
            }
        }
        
        //private static IEnumerable<MSDrivesInfo> SelectDeviceInformation()
        //{
        //    foreach (ManagementObject device in SelectDevices())
        //    {
        //        var deviceId = (string)device.GetPropertyValue("DeviceID");
        //        var pnpDeviceId = (string)device.GetPropertyValue("PNPDeviceID");
        //        var driveLetter = (string)SelectPartitions(device).SelectMany(SelectDisks).Select(disk => disk["Name"]).Single();

        //        yield return new MSDrivesInfo { DriverLetter = driveLetter, DeviceId = deviceId, PNPDeviceId = pnpDeviceId };//(deviceId, pnpDeviceId, driveLetter);
        //    }

        //    static IEnumerable<ManagementObject> SelectDevices() => new ManagementObjectSearcher(
        //            @"SELECT * FROM Win32_DiskDrive WHERE InterfaceType LIKE 'USB%'").Get()
        //        .Cast<ManagementObject>();

        //    static IEnumerable<ManagementObject> SelectPartitions(ManagementObject device) => new ManagementObjectSearcher(
        //            "ASSOCIATORS OF {Win32_DiskDrive.DeviceID=" +
        //            "'" + device.Properties["DeviceID"].Value + "'} " +
        //            "WHERE AssocClass = Win32_DiskDriveToDiskPartition").Get()
        //        .Cast<ManagementObject>();

        //    static IEnumerable<ManagementObject> SelectDisks(ManagementObject partition) => new ManagementObjectSearcher(
        //            "ASSOCIATORS OF {Win32_DiskPartition.DeviceID=" +
        //            "'" + partition["DeviceID"] + "'" +
        //            "} WHERE AssocClass = Win32_LogicalDiskToPartition").Get()
        //        .Cast<ManagementObject>();
        //}
    }
}
