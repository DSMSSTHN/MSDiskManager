using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Entities
{
    public class ImageThumbnail
    {
        public long Id { get; set; }
        public long FileId { get; set; }
        public virtual MSFile  File { get; set; }
        public byte[] Thumbnail { get; set; }
    }
}
