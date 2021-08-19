#nullable enable
using MSDiskManagerData.Data.Entities.Relations;
using MSDiskManagerData.Helpers;
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
        private string onDeskName = "New_File";

        public long? Id { get; set; }
        public long? DriverId { get; set; }
        public string Name { get; set; } = "New Directory";
        public string OnDeskName { get => onDeskName; set => onDeskName = value.Replace("\\","").Replace("/","").Trim(); }
        public string Description { get; set; } = "";
        public Instant AddingDate { get; set; }
        public long? ParentId { get; set; }
        public DirectoryEntity? Parent { get; set; }
        public List<long> AncestorIds { get; set; } = new List<long>();
        public String Path
        {
            get => path; set
            {
                var v = value;
                if (Globals.IsNotNullNorEmpty(v))
                {
                    while (v.Contains('/') || v.Contains("\\\\"))
                    {
                        v.Replace('/', '\\'); v.Replace("\\\\", "\\");
                    }
                    if (v[0] == '\\') v = v.Length > 1 ? v.Substring(1, v.Length) : "";
                    if (v[v.Length - 1] == '\\') v = v.Length > 1 ? v.Substring(0, v.Length - 1) : "";
                }
                path = v.Trim();
            }
        }
        public String OldPath { get; set; } = "";
        public Instant MovingDate { get; set; }
        public Instant LastFileAddedDate { get; set; }
        public int NumberOfFiles { get; set; }
        public int NumberOfFilesRec { get; set; }
        public int NumberOfDirectories { get; set; }
        public int NumberOfDirectoriesRec { get; set; }
        public int NumberOfItems => NumberOfFiles + NumberOfDirectories;
        public int NumberOfItemsRec => NumberOfFilesRec + NumberOfDirectoriesRec;
        public virtual List<DirectoryTag> DirectoryTags { get; set; } = new List<DirectoryTag>();
        public bool IsHidden { get; set; }
        public virtual List<FileEntity> Files { get; set; } = new List<FileEntity>();
        public virtual List<DirectoryEntity> Children { get; set; } = new List<DirectoryEntity>();
        public string FullPath { get => MSDM_DBContext.DriverName[0] + ":\\" + Path; }

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
