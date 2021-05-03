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
        public static bool IsNullOrEmpty<T>(List<T> list)
        {
            return list == null || list.Count == 0;
        }
        public static bool IsNotNullNorEmpty<T>(List<T> list) => !IsNullOrEmpty(list);
    }
}
