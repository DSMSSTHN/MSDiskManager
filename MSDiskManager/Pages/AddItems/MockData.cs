using MSDiskManager.Helpers;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using MSDiskManager.Helpers;
using System.Threading.Tasks;

namespace MSDiskManager.Pages.AddItems
{
    public static class MockData
    {
        public static ObservableCollection<BaseEntityViewModel> Items
        {
            get
            {
                var items = Directories.Cast<BaseEntityViewModel>().ToList();
                items.AddRange(Files);
                return items.ToObservableCollection();
            }
        }
        public static ObservableCollection<FileViewModel> Files
        {
            get
            {
                var result = new ObservableCollection<FileViewModel>();
                var dir = "D:\\MSDMAddTest";
                var dvm = dir.GetFullDirectory();

                foreach (var item in dvm.directory.Files)
                {
                    item.Tags.AddMany(Tags);
                    item.IgnoreAdd = new Random().Next(100) % 2 == 0;
                    result.Add(item);
                }
                return result;
            }
        }
        public static ObservableCollection<DirectoryViewModel> Directories
        {
            get
            {
                var result = new ObservableCollection<DirectoryViewModel>();
                var dir = "D:\\MSDMAddTest";
                var dvm = dir.GetFullDirectory();
                foreach (var item in dvm.directory.Children)
                {
                    item.Tags = Tags.ToObservableCollection();
                    item.IgnoreAdd = new Random().Next(100) % 2 == 0;
                    result.Add(item);
                    result.Add(item);
                    result.Add(item);
                }
                return result;
            }
        }
        public static List<Tag> Tags
        {
            get
            {
                var lst = new List<Tag>();
                for (int i = 0; i < 10; i++)
                {
                    lst.Add(new Tag { Color = new Random().Next(10), Name = $"Tag{i + 1}" });
                    lst.Add(new Tag { Color = new Random().Next(10), Name = $"Tag{i + 1}" });
                    lst.Add(new Tag { Color = new Random().Next(10), Name = $"Tag{i + 1}" });
                }
                return lst;
            }
        }
    }
}
