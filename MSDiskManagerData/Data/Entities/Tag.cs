#nullable enable
using MSDiskManagerData.Data.Entities.Relations;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Entities
{
    public class Tag
    {
        public long? Id { get; set; }
        public string Name { get; set; } = "";
        public int Color { get; set; }
        public virtual List<FileTag> FileTags { get; set; } = new List<FileTag>();
        public virtual List<DirectoryTag> DirectoryTags { get; set; } = new List<DirectoryTag>();
        public bool IsHidden { get; set; }
        public Instant CreationDate { get; set; }
        public Instant ModificationDate { get; set; }
        public Instant LastAccessDate { get; set; }
    }
}
