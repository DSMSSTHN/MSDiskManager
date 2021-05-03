using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Entities
{
    public class IgnoredFile
    {
        public long? Id { get; set; }
        public string Name { get; set; } = "new_file";
        public String Extension { get; set; } = "txt";
        public String Path { get; set; } = "";
        public string FullPath { get => MSDM_DBContext.DriverName + Path; }
    }
}
