using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MSDiskManager.Helpers
{
    public static class Extensions
    {
        public static ObservableCollection<T> Clone<T>(this ObservableCollection<T> col) => new ObservableCollection<T>(col.ToList());
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> col) => new ObservableCollection<T>(col);

        public static List<T> RemoveWhere<T>(this ICollection<T> obs, Predicate<T> predicate)
        {
            var lst = new List<T>();
            foreach (var o in obs) if (predicate(o)) lst.Add(o);
            foreach (var l in lst) obs.Remove(l);
            return lst;
        }
        public static string ByteSizeToSizeString(this long size)
        {
            double s = (double)size;
            string[] units = new string[] { "Bytes", "KB", "MB", "GB", "TB" };
            int i = 0;
            for (; i < units.Length; i++)
            {
                var newSize = ((double)size) / 1024;
                if (s > 1024)
                {
                    s = s / 1024;
                }
                else
                {
                    break;
                }
            }
            return $"{s.ToString().Substring(0, Math.Min(s.ToString().Length, 4))} {units[i]}";
        }
        public static List<T> RemoveWhere<T>(this List<T> obs, Predicate<T> predicate)
        {
            var lst = new List<T>();
            foreach (var o in obs) if (predicate(o)) lst.Add(o);
            foreach (var l in lst) obs.Remove(l);
            return lst;
        }
        public static void AddMany<T, V>(this ObservableCollection<T> col, IEnumerable<V> collection, CancellationToken? cancellation = null)
            where V : T
        {
            foreach (var item in collection)
            {
                if (cancellation?.IsCancellationRequested ?? false) return;
                col.Add(item);
            }
        }
        public static string GetTempDirectory(this object _)
        {
            var p =  Path.Combine(Path.GetTempPath(),"MSDM");
            if(!Directory.Exists(p))Directory.CreateDirectory(p);
            return p;
        }
        public static string GetThumbnailTempDirectory(this object obj)
        {
            var p =  Path.Combine(obj.GetTempDirectory(), "icons");
            if (!Directory.Exists(p)) Directory.CreateDirectory(p);
            return p;
        }
       
        public static long GetDirSize(this DirectoryInfo d, Action<long> reportSize = null)
        {
            long size = 0;
            // Add file sizes.
            try
            {
                FileInfo[] fis = d.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    size += fi.Length;
                }

                if (reportSize != null) reportSize(size);
                // Add subdirectory sizes.
                DirectoryInfo[] dis = d.GetDirectories();
                foreach (DirectoryInfo di in dis)
                {
                    size += di.GetDirSize(reportSize);
                }
            }
            catch (Exception) { }
            return size;

        }
        //        public static string GetUUIDFilePath(this DriveInfo d, string DBName)
        //        {
        //#if DEBUG
        //            return d.Name + DBName + ".msdmtest";
        //#endif
        //            return d.Name + DBName + ".msdm";
        //        }
        //        public static string GetUUIDFilePath(this MSDriver d, string DBName)
        //        {
        //#if DEBUG
        //            return d.DriverLetter[0] + ":\\" + DBName + ".msdmtest";
        //#endif
        //            return d.DriverLetter[0] + ":\\" + DBName + ".msdm";
        //        }
        public static string GetApplicationDataFile(this Application app)
        {
            try
            {
                var dirName = "MSDiskManager";
#if DEBUG
                dirName = "MSDiskManagerTest";
#endif
                var p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), dirName);
                if (!Directory.Exists(p)) Directory.CreateDirectory(p);
                return Path.Combine(p, "AppSettings.json");
            }
            catch (Exception e)
            {

                throw;
            }
        }
        public static TypeModel Clone(this TypeModel ft) => new TypeModel(ft.Type, ft.IsChecked);
        public static OrderModel Clone(this OrderModel om) => new OrderModel(om.Order, om.IsChecked);
        public static List<TypeModel> Clone(this List<TypeModel> lst) => lst.Select(tm => tm.Clone()).ToList();
        public static List<OrderModel> Clone(this List<OrderModel> lst) => lst.Select(om => om.Clone()).ToList();
        public static C With<C, T>(this C lst, T? item)
            where C : ICollection<T>
        {
            if (item != null) lst.Add(item!);
            return lst;
        }
        public static C With<C, T>(this C lst, ICollection<T> items)
            where C : ICollection<T>
        {
            if (Globals.IsNullOrEmpty(items)) foreach (var i in items) lst.Add(i);
            return lst;
        }

        public static void InsertSorted<T, V, E>(this ObservableCollection<T> col, IEnumerable<V> items, Func<T, E> conversion)
            where E : IComparable
            where V : T
        {
            foreach (var item in items) col.InsertSorted(item, conversion);
        }
        public static async Task InsertSortedWithDelay<T, V, E>(this ObservableCollection<T> col, IEnumerable<V> items, Func<T, E> conversion, int msDelay)
            where E : IComparable
            where V : T
        {
            foreach (var item in items)
            {
                if (msDelay > 0) await Task.Delay(msDelay);
                col.AppInvoke(() =>
                {
                    col.InsertSorted(item, conversion);
                });
            }
        }   
        public static void InsertSorted<T, V, E>(this ObservableCollection<T> col, V item, Func<T, E> conversion)
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
        public static DirectoryViewModel ToDirectoryViewModel(this MSDirecotry entity, DirectoryViewModel? parent = null)
        {
            var result = new DirectoryViewModel
            {
                Id = entity.Id,

                Files = entity.Files?.Select(f => f.ToFileViewModel())?.ToList() ?? new List<FileViewModel>(),
                AddingDate = entity.AddingDate,
                AncestorIds = entity.AncestorIds ?? new List<long>(),
                Description = entity.Description,
                IsHidden = entity.IsHidden,
                Name = entity.Name,
                MovingDate = entity.MovingDate,
                OnDeskName = entity.OnDeskName,
                Parent = parent ?? entity.Parent?.ToDirectoryViewModel(),
                Path = entity.Path,
                OriginalPath = entity.FullPath,
                Tags = entity.DirectoryTags?.Select(t => t.Tag)?.ToObservableCollection() ?? new ObservableCollection<Tag>(),
                ParentId = entity.ParentId,

            };
            result.Children = entity.Children?.Select(c => c.ToDirectoryViewModel(result))?.ToList() ?? new List<DirectoryViewModel>();
            return result;
        }

        public static FileViewModel ToFileViewModel(this MSFile entity)
        {
            return new FileViewModel
            {
                Id = entity.Id,
                AddingDate = entity.AddingDate,
                AncestorIds = entity.AncestorIds ?? new List<long>(),
                Description = entity.Description,
                IsHidden = entity.IsHidden,
                Name = entity.Name,
                MovingDate = entity.MovingDate,
                OnDeskName = entity.OnDeskName,
                Parent = entity.Parent?.ToDirectoryViewModel(),
                Path = entity.Path,
                OriginalPath = entity.FullPath,
                Tags = entity.FileTags?.Select(t => t.Tag)?.ToObservableCollection() ?? new ObservableCollection<Tag>(),
                ParentId = entity.ParentId,
                FileType = entity.FileType,
                Extension = entity.Extension,
            };
        }
        public static void AppInvoke(this object _, Action action) => Application.Current.Dispatcher.Invoke(() => { action?.Invoke(); });
        public static void AppInvokeLock(this object _, object lockObject, Action action) => Application.Current.AppInvoke(() =>
        {
            lock (lockObject) { action?.Invoke(); }
        });
        public static void AppInvokeAfter(this object obj, int msDuration, Action action) => obj.AppInvoke(async () =>
        {
            await Task.Delay(msDuration);
            action?.Invoke();
        });
    }
}
