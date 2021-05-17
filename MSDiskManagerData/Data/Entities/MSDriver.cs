#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Entities
{
    public class MSDriver
    {
        public long? Id { get; set; }
        public string? DriverLetter { get; set; }
        public string? DriverId { get; set; }
        public string? PNPDriverId { get; set; }
        public string? DriverUUID { get; set; }

        public virtual List<FileEntity> Files { get; set; } = new List<FileEntity>();
        public virtual List<DirectoryEntity> Directories { get; set; } = new List<DirectoryEntity>();
    }
}
