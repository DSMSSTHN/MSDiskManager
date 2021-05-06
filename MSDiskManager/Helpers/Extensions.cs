using MSDiskManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace MSDiskManager.Helpers
{
    public static class Extensions
    {
        public static ObservableCollection<T> Clone<T>(this ObservableCollection<T> col) => new ObservableCollection<T>(col.ToList());
        public static ObservableCollection<T> ToObservableCollection<T>(this ICollection<T> col) => new ObservableCollection<T>(col);
        public static List<T> RemoveWhere<T>(this ObservableCollection<T> obs,Predicate<T> predicate)
        {
            var lst = new List<T>();
            foreach (var o in obs) if (predicate(o)) lst.Add(o);
            foreach (var l in lst) obs.Remove(l);
            return lst;
        }
        public static List<T> RemoveWhere<T>(this List<T> obs, Predicate<T> predicate)
        {
            var lst = new List<T>();
            foreach (var o in obs) if (predicate(o)) lst.Add(o);
            foreach (var l in lst) obs.Remove(l);
            return lst;
        }
        public static void AddMany<T,V>(this ObservableCollection<T> col, ICollection<V> collection, CancellationToken? cancellation = null)
            where V : T
        {
            foreach (var item in collection)
            {
                if (cancellation?.IsCancellationRequested ?? false) return;
                col.Add(item);
            }
        }
        public static TypeModel Clone(this TypeModel ft) => new TypeModel(ft.Type, ft.IsChecked);
        public static OrderModel Clone(this OrderModel om) => new OrderModel(om.Order, om.IsChecked);
        public static List<TypeModel> Clone(this List<TypeModel> lst) => lst.Select(tm => tm.Clone()).ToList();
        public static List<OrderModel> Clone(this List<OrderModel> lst) => lst.Select(om => om.Clone()).ToList();

        public static void InsertSorted<T,V, E>(this ObservableCollection<T> col, ICollection<V> items, Func<T, E> conversion)
            where E : IComparable
            where V:T
        {
            foreach (var item in items) col.InsertSorted(item, conversion);
        }
        public static void InsertSorted<T,V, E>(this ObservableCollection<T> col, V item, Func<T, E> conversion)
            where E : IComparable
            where V : T
        {
            for (int i = 0; i < col.Count; i++)
            {
                if (conversion(col[i]).CompareTo(conversion(item)) >= 0)
                {
                    col.Insert(i, item);
                    return;
                }
            }
            col.Add(item);
        }
    }
}
