using MSDiskManager.Helpers;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManager.Dialogs
{
    public static class MockData
    {
        public static List<ENTITYEXCEPTION> Failures
        {
            get
            {
                return MSDiskManager.Pages.AddItems.MockData.Directories.Select(d => new ENTITYEXCEPTION(d as BaseEntityViewModel,new Exception("Some Exception Message"))).ToList();

            }
        }
        public static ObservableCollection<Tag> Tags
        {
            get
            {
                var tags = new ObservableCollection<Tag>();
                for (int i = 0; i < 30; i++)
                {
                    tags.Add(new Tag { Name = $"Tag{i}", Color = i % 11 });
                }
                return tags;
            }
        }
        public static List<int> AllColors
        {
            get
            {
                var result = new List<int>();
                for (int i = 0; i < 20; i++) result.Add(i);
                return result;
            }
        }
    }
}
