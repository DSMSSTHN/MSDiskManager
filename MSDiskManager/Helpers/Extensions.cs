using MSDiskManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManager.Helpers
{
    public static class Extensions
    {
        public static ObservableCollection<T> Clone<T>(this ObservableCollection<T> col) => new ObservableCollection<T>(col.ToList());
        public static TypeModel Clone(this TypeModel ft) => new TypeModel(ft.Type, ft.IsChecked);
        public static OrderModel Clone(this OrderModel om) => new OrderModel(om.Order, om.IsChecked);
        public static List<TypeModel> Clone(this List<TypeModel> lst) => lst.Select(tm => tm.Clone()).ToList();
        public static List<OrderModel> Clone(this List<OrderModel> lst) => lst.Select(om => om.Clone()).ToList();


        public static void InsertSorted<T,E>(this ObservableCollection<T> col, T item, Func<T,E> conversion)
            where E : IComparable
        {
            for (int i = 0; i < col.Count; i++)
            {
                if(conversion(col[i]).CompareTo(item) >= 0)
                {
                    col.Insert(i, item);
                    return;
                }
            }
            col.Add(item);
        } 
    }
}
