using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Entities.Relations
{
    public class FileTag
    {
        public long FileId { get; set; }
        public long TagId { get; set; }
        public virtual MSFile File { get; set; }
        public virtual Tag Tag { get; set; }
    }
}
