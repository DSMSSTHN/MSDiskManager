using MSDiskManager.Helpers;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Entities.Relations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManager.Controls
{
    public static class MockData
    {
        public static List<FileViewModel> Files
        {
            get
            {
                var fileTypes = Enum.GetValues(typeof(FileType)).Cast<FileType>().ToList();
                FileType random()
                {
                    return fileTypes[new Random().Next(fileTypes.Count)];
                }
                var result = new List<FileViewModel>();
                for (int i = 0; i < 15; i++)
                {
                    result.Add(new FileViewModel
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
                        Tags = Tags.ToObservableCollection(),

                    });
                }
                return result;
            }
        }
        public static List<DirectoryViewModel> Folders
        {
            get
            {


                var result = new List<DirectoryViewModel>();
                for (int i = 0; i < 5; i++)
                {
                    result.Add(new DirectoryViewModel
                    {
                        Name = "DirectoryName" + i,
                        AddingDate = new NodaTime.Instant(),
                        Description = "dsadasd",
                        Path = "ASdasd as sa s as sa",
                        IsHidden = false,
                        MovingDate = new NodaTime.Instant(),
                        OnDeskName = "sdadsa",
                        Tags = Tags.ToObservableCollection(),

                    }) ;
                }
                return result;
            }
        }
        public static List<Tag> Tags{
            get
            {
                var result = new List<Tag>();
                for (int i = 0; i < 10; i++)
                {
                    result.Add( new Tag { Name = $"Tag{ i}", Color = new Random().Next(10) });
                }
                return result;
            } }
        public static List<BaseEntityViewModel> Items
        {
            get
            {
                var result = new List<BaseEntityViewModel>();
                result.AddRange(Folders);
                result.AddRange(Files);
                return result;
            }
        }
    }
}
