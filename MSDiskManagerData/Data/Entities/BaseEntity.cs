#nullable enable
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Entities
{
    public interface BaseEntity
    {
        public long? Id { get; set; }
        public String Name { get; set; }
        public String Path { get; set; }
        public string? DriveId { get; set; }
        public String OldPath { get; set; }
        public List<long> AncestorIds { get; set; }
        public Instant AddingDate { get; set; }
        public Instant MovingDate { get; set; }
        public long? ParentId { get; set; }
        public MSDirecotry? Parent { get; set; }
        public bool IsHidden { get; set; }
        public string OnDeskName { get; set; }
        public string Description { get; set; }
        public string FullPath{get;}
        public IconType IconType { get; }
    }
    public enum IconType
    {
        Unknown,
        Text,
        Image,
        Music,
        Video,
        Compressed,
        Document,
        Directory,
    }
}
