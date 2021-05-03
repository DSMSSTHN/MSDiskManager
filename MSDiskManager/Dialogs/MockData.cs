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
        public static ObservableCollection<Tag> Tags { get
            {
                var tags = new ObservableCollection<Tag>();
                for (int i = 0; i < 30; i++)
                {
                    tags.Add(new Tag { Name = $"Tag{i}", Color = i % 11 });
                }
                return tags;
            } }
        public static List<int> AllColors { get
            {
                var result = new List<int>();
                for (int i = 0; i < 11; i++) result.Add(i);
                return result;
            }
        }
    }
}
