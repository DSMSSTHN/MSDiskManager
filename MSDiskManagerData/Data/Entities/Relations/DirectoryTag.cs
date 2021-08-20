using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Entities.Relations
{
    public class DirectoryTag
    {
        public long DirectoryId { get; set; }
        public long TagId { get; set; }
        public virtual MSDirecotry Directory { get; set; }
        public virtual Tag Tag { get; set; }
    }
}
