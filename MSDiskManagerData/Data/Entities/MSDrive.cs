#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Entities
{
    public class MSDrive
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Password { get; set; }
        public virtual string? Letter { get; set; }

        public virtual bool IsLocked => Password != null && Password.Length > 0;

        public virtual List<MSFile> Files { get; set; } = new List<MSFile>();
        public virtual List<MSDirecotry> Directories { get; set; } = new List<MSDirecotry>();
    }
}
