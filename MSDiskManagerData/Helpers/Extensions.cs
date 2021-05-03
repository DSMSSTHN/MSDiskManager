using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Helpers
{
    public static class Extensions
    {
        public static IQueryable<T> TrySkipLimit<T>(this IQueryable<T> que, int page,int limit)
        {
            if (limit <= 0) return que;
            var p = page;
            if (page <= 0) p = 0;
            return que.Skip(page * limit).Take(limit);
        }

        public static bool IsEmpty(this string str) => str.Trim().Length == 0;
        public static bool IsNotEmpty(this string str) => str.Trim().Length > 0;
        public static bool IsEmpty<T>(this List<T> list) => list.Count == 0;
        public static bool IsNotEmpty<T>(this List<T> list) => list.Count > 0;
    }
}
