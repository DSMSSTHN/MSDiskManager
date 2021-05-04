#nullable enable
using MSDiskManagerData.Data.Entities.Relations;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Entities
{
    public class DirectoryEntity : BaseEntity
    {
        private string path = "";

        public long? Id { get; set; }
        public string Name { get; set; } = "new_folder";
        public string OnDeskName { get; set; } = "new_folder";
        public string Description { get; set; } = "new_folder";
        public Instant AddingDate { get; set; }
        public long? ParentId { get; set; }
        public DirectoryEntity? Parent { get; set; }
        public List<long> AncestorIds { get; set; } = new List<long>();
        public String Path { get => path; set { path = value; if (value != null && value.Length > 0) while (path[path.Length - 1] == '/' || path[path.Length - 1] == '\\') path = path.Substring(0, path.Length - 1); } }
        public Instant MovingDate { get; set; }
        public Instant LastFileAddedDate { get; set; }
        public int NumberOfFiles { get; set; }
        public virtual List<DirectoryTag> DirectoryTags { get; set; } = new List<DirectoryTag>();
        public bool IsHidden { get; set; }
        public virtual List<FileEntity> Files { get; set; } = new List<FileEntity>();
        public virtual List<DirectoryEntity> Children { get; set; } = new List<DirectoryEntity>();
        public string FullPath { get => MSDM_DBContext.DriverName + Path; }

        public IconType IconType => IconType.Directory;

    }

    public class DirectoryFilter
    {
        public string? _name = null;
        public string? Name { get => _name?.ToLower()?.Trim(); set => _name = value; }
        public string? TagName { get; set; }
        public List<long> tagIds { get; set; } = new List<long>();
        public long? ParentId { get; set; }
        public List<long> AncestorIds { get; set; } = new List<long>();
        public bool IncludeDirectoryNameInSearch { get; set; }
        public bool IncludeDescriptionInSearch { get; set; }
        public bool IncludeHidden { get; set; } = false;
        public Instant? MovedAfter { get; set; }
        public Instant? AddedAfter { get; set; }
        public List<long>? ExcludeIds { get; set; }
        public DirectoryOrder Order { get; set; } = DirectoryOrder.Name;
        public int Page { get; set; } = 0;
        public int Limit { get; set; } = 0;
    }

    public enum DirectoryOrder
    {
        Name,
        NameDesc,
        AddTime,
        AddTimeDesc,
        MoveTime,
        MoveTimeDesc,
    }
}
