using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace MSDiskManager.Helpers
{
    public class MSDeskIdentifier
    {
        /// <summary>
        /// get the driver letter for all Drives
        /// </summary>
        public static List<String> DriveLetters => DriveInfo.GetDrives().Select(d => d.Name).ToList();

        /// <summary>
        /// gets the Drives which are already part of MSDM
        /// </summary>
        public static List<MSDriverInfo> MSDrives
        {
            get
            {
                var letters = DriveLetters;
                var result = new List<MSDriverInfo>();
                letters.ForEach(l =>
                {
                    var msidFile = Directory.GetFiles(l).FirstOrDefault(f => new FileInfo(f).Extension == ".msdm");
                    if (msidFile != null) result.Add(new MSDriverInfo
                    {
                        Letter = l,
                        MSID_Path = msidFile,
                        MSID_FileName = new FileInfo(msidFile).Name
                    });
                });
                return result;
            }
        }
    }
    public struct MSDriverInfo
    {
        public string Letter { get; set; }
        public string MSID_Path { get; set; }
        public string MSID_FileName { get; set; }


        public override string ToString()
        {
            return $"Driver:[{Letter}]\nMSID File Path:[{MSID_Path}]";
        }
    }
}
