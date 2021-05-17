using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Helpers
{
    public static class Globals
    {
        public static bool IsNullOrEmpty(string str)    
        {
            return str == null || str.Trim().Length == 0;
        }
        public static bool IsNotNullNorEmpty(string str) => !IsNullOrEmpty(str);
        public static bool IsNoneNullOrEmpty(IEnumerable<string> strings) => strings.All(s => IsNotNullNorEmpty(s));
        public static bool IsNoneNullOrEmpty(params string[] strings) => strings.All(s => IsNotNullNorEmpty(s));
        public static bool IsNullOrEmpty<T>(ICollection<T> list)
        {
            return list == null || list.Count == 0;
        }
        public static bool IsNotNullNorEmpty<T>(ICollection<T> list) => !IsNullOrEmpty(list);
    }
}
