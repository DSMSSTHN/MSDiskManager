using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManager.Pages.Main
{
    public static class MockData
    {
        private static FilterTopViewModel topModel;
        public static FilterTopViewModel TopModel { get
            {
                if (topModel == null) topModel = new FilterTopViewModel { FilterModel = FilterModel };
                return topModel;
            } }
        public static FilterModel FilterModel
        {
            get
            {
                return new FilterModel("sdadsad","",true,true,true,false,false,false,new DirectoryEntity { Id = 15 },TypeModel.AllFilterTypes,OrderModel.AllOrdersModels,
                    toObservable(initList<long>(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15).Select(id => new Tag { Id = id, Name = $"Tag{id + 1}", Color = (int)id }).ToList()));
            }
        }
        public static List<TypeModel> FilterTypes { get => TypeModel.AllFilterTypes; }
        public static List<OrderModel> OrderModels { get => OrderModel.AllOrdersModels; }

        public static List<E> initList<E>(params E[] elements)
        {
            var lst = new List<E>();
            for (int i = 0; i < elements.Length; i++)
            {
                lst.Add(elements[i]);

            }
            return lst;
        }
        public static ObservableCollection<T> toObservable<T>(List<T> lst)
        {
            var result = new ObservableCollection<T>();
            foreach (var item in lst)
            {
                result.Add(item);
            }
            return result;
        }
    }




}
