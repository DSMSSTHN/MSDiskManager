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
        public long? Id { get; set; } = null;
        public List<MSDriver> Drivers { get; set; } = new List<MSDriver>();
        public string HostName { get; set; } = "";
        public string Port { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string DBName { get; set; } = "";
        [JsonIgnore]
        public string ConnectionString => $"host={HostName};port={Port};database={DBName};username={UserName};password={Password};timeout=0;";
        

        public async Task<bool> ConnectToDriver()
        {
            try
            {
                MSDM_DBContext.SetConnectionString(ConnectionString);
                var drives = DriveInfo.GetDrives();
                if (this.Drivers != null && this.Drivers.Count > 0)
                {
                    foreach (var d in Drivers.ToList())
                    {
                        if (File.Exists(d.GetUUIDFilePath(DBName)))
                        {
                            var uuid = await File.ReadAllTextAsync(d.GetUUIDFilePath(DBName));
                            if (uuid != null && uuid.Length > 0 && uuid == d.DriverUUID)
                            {
                                var rep = new DriverRepository();
                                var dvs = await rep.GetAll();
                                MSDriver dbDriver = dvs.FirstOrDefault(dbd => dbd.DriverUUID == uuid);
                                //Console.WriteLine(dbDriver.DriverUUID);
                                if (dbDriver != null)
                                {
                                    MSDM_DBContext.SetDriver(dbDriver);
                                    if (MSDM_DBContext.DriverName.ToLower()[0] != d.DriverLetter.ToLower()[0])
                                    {
                                        await rep.ChangeLetter((long)d.Id, d.DriverLetter[0].ToString());
                                    }
                                    return true;
                                }

                            }
                        }
                        else
                        {
                            foreach (var d2 in drives)
                            {
                                if (File.Exists(d2.GetUUIDFilePath(DBName)))
                                {
                                    var uuid = File.ReadAllText(d2.GetUUIDFilePath(DBName));
                                    if (uuid != d.DriverUUID) continue;
                                    MSDriver dbdr = null;
                                    if (uuid != null && uuid.Length > 0 && (dbdr = await new DriverRepository().GetDriver(uuid)) != null)
                                    {
                                        if (MSDM_DBContext.DriverName.ToLower()[0] != d2.Name.ToLower()[0])
                                        {
                                            await new DriverRepository().ChangeLetter((long)d.Id, d2.Name[0].ToString());
                                        }
                                        d.DriverLetter = d2.Name[0].ToString();
                                        Drivers.Remove(d);
                                        Drivers.Insert(0, d);
                                        Save();
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public void Save()
        {
            var settingsFile = Application.Current.GetApplicationDataFile();
            List<AppSettings> settings = new List<AppSettings>();
            if (!File.Exists(settingsFile))
            {
                if (Id == null) Id = 0;
                settings.Add(this);
            }
            else
            {
                settings = JsonConvert.DeserializeObject<List<AppSettings>>(File.ReadAllText(settingsFile));
                if (Id != null)
                {
                    var s = settings.FirstOrDefault(s => s.Id == Id);
                    if (s != null) settings.Remove(s);
                    settings.Insert(0, this);
                }
                else
                {
                    long? max = 0;
                    foreach (var s in settings) if (s.Id != null && s.Id > max) max = s.Id;
                    this.Id = max + 1;
                    settings.Insert(0, this);
                }
            }
            var json = JsonConvert.SerializeObject(settings);
            File.WriteAllText(settingsFile, json);
        }

        public async static Task<AppSettings> GetLastAppSettings()
        {

            try
            {
                var settingsFile = Application.Current.GetApplicationDataFile();
                if (!File.Exists(settingsFile)) return null;
                var str = File.ReadAllText(settingsFile);
                var lst = JsonConvert.DeserializeObject<List<AppSettings>>(str);
                if (Globals.IsNullOrEmpty(lst)) return null;
                var driverInfos = SelectDeviceInformation();
                var first = true;
                foreach (var i in lst)
                {
                    if (await i.ConnectToDriver())
                    {
                        if (!first) i.Save();
                        return i;
                    }
                    first = false;
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }

        }
        public void SelectDriver(MSDriver driver)
        {
            if (!this.Drivers.Contains(driver)) throw new Exception("Driver not found exception");
            MSDM_DBContext.SetDriver(driver);
            this.Drivers.Remove(driver);
            this.Drivers.Insert(0, driver);
            Save();
        }
        public async Task<MSDriver> AddDriver(MSDriver driver)
        {
            if (File.Exists(driver.GetUUIDFilePath(DBName)))
            {
                throw new Exception("Identifier file for this dbname already exists");
            }
            if (this.Drivers != null && this.Drivers.Any(d => d.DriverUUID == driver.DriverUUID))
            {
                throw new Exception("driver with this uuid already exists in settings");
            }
            File.WriteAllText(driver.GetUUIDFilePath(DBName), driver.DriverUUID);
            MSDM_DBContext.SetConnectionString(ConnectionString);
            var rep = new DriverRepository();
            var drd = await rep.AddDriver(driver);
            MSDM_DBContext.SetDriver(drd);
            this.Drivers.Add(drd);
            Save();
            return drd;
        }
        private static IEnumerable<MSDriversInfo> SelectDeviceInformation()
        {
            foreach (ManagementObject device in SelectDevices())
            {
                var deviceId = (string)device.GetPropertyValue("DeviceID");
                var pnpDeviceId = (string)device.GetPropertyValue("PNPDeviceID");
                var driveLetter = (string)SelectPartitions(device).SelectMany(SelectDisks).Select(disk => disk["Name"]).Single();

                yield return new MSDriversInfo { DriverLetter = driveLetter, DeviceId = deviceId, PNPDeviceId = pnpDeviceId };//(deviceId, pnpDeviceId, driveLetter);
            }

            static IEnumerable<ManagementObject> SelectDevices() => new ManagementObjectSearcher(
                    @"SELECT * FROM Win32_DiskDrive WHERE InterfaceType LIKE 'USB%'").Get()
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
    }
}
