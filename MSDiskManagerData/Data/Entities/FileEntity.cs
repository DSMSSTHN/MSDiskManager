#nullable enable
using MSDiskManagerData.Data.Entities.Relations;
using NodaTime;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Entities
{
    public class FileEntity : BaseEntity
    {
        public long? Id { get; set; }
        public string Name { get; set; } = "new_file";
        public string OnDeskName { get; set; } = "new_file";
        public string Description { get; set; } = "new_file";
        public String Extension { get; set; } = "txt";
        public String Path { get; set; } = "";
        public FileType FileType { get; set; } = FileType.Unknown;
        public long? ParentId { get; set; }
        public DirectoryEntity? Parent { get; set; }
        public List<long> AncestorIds { get; set; } = new List<long>();
        public Instant AddingDate { get; set; }
        public Instant MovingDate { get; set; }
        public virtual List<FileTag> FileTags { get; set; } = new List<FileTag>();
        public bool IsHidden { get; set; }

        public string FullPath { get => MSDM_DBContext.DriverName + Path; }

        public IconType IconType
        {
            get
            {
                switch (FileType)
                {
                    case FileType.Unknown:
                        return IconType.Unknown;
                    case FileType.Text:
                        return IconType.Text;
                    case FileType.Image:
                        return IconType.Image;
                    case FileType.Music:
                        return IconType.Music;
                    case FileType.Video:
                        return IconType.Video;
                    case FileType.Compressed:
                        return IconType.Compressed;
                    case FileType.Document:
                        return IconType.Document;
                }
                return IconType.Unknown;
            }
        }
    }

    public enum FileType
    {
        Unknown,
        Text,
        Image,
        Music,
        Video,
        Compressed,
        Document,

    }

    public class FileFilter
    {
        public string? _name = null;
        public string? Name { get => _name?.ToLower()?.Trim(); set => _name = value; }
        public string? TagName { get; set; }
        public List<long> tagIds { get; set; } = new List<long>();
        public long? DirectoryId { get; set; }
        public List<long> AncestorIds { get; set; } = new List<long>();
        public bool IncludeFileNameInSearch { get; set; }
        public bool IncludeDescriptionInSearch { get; set; }
        public bool IncludeHidden { get; set; } = false;
        public Instant? MovedAfter { get; set; }
        public Instant? AddedAfter { get; set; }
        public List<string> Extensions { get; set; } = new List<string>();
        public List<FileType> Types { get; set; } = new List<FileType>();
        public List<long>? ExcludeIds { get; set; }
        public FileOrder Order { get; set; } = FileOrder.Name;
        public int Page { get; set; } = 0;
        public int Limit { get; set; } = 0;
    }

    public enum FileOrder
    {
        Name,
        NameDesc,
        AddTime,
        AddTimeDesc,
        MoveTime,
        MoveTimeDesc,
        Type,
        TypeDesc,
    }


}
