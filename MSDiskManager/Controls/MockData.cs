using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManager.Controls
{
    public static class MockData
    {
        public static List<FileEntity> Files
        {
            get
            {
                var fileTypes = Enum.GetValues(typeof(FileType)).Cast<FileType>().ToList();
                FileType random()
                {
                    return fileTypes[new Random().Next(fileTypes.Count)];
                }
                var result = new List<FileEntity>();
                for (int i = 0; i < 15; i++)
                {
                    result.Add(new FileEntity
                    {
                        Name = "FileName" + i,
                        AddingDate = new NodaTime.Instant(),
                        Description = "dsadasd",
                        Path = "ASdasd as sa s as sa",
                        FileType = random(),
                        Extension = "txt",
                        IsHidden = false,
                        MovingDate = new NodaTime.Instant(),
                        OnDeskName = "sdadsa",


                    });
                }
                return result;
            }
        }
        public static List<DirectoryEntity> Folders
        {
            get
            {


                var result = new List<DirectoryEntity>();
                for (int i = 0; i < 5; i++)
                {
                    result.Add(new DirectoryEntity
                    {
                        Name = "DirectoryName" + i,
                        AddingDate = new NodaTime.Instant(),
                        Description = "dsadasd",
                        Path = "ASdasd as sa s as sa",
                        IsHidden = false,
                        MovingDate = new NodaTime.Instant(),
                        OnDeskName = "sdadsa",


                    });
                }
                return result;
            }
        }
        public static List<BaseEntity> Items
        {
            get
            {
                var result = new List<BaseEntity>();
                result.AddRange(Folders);
                result.AddRange(Files);
                return result;
            }
        }
    }
}
