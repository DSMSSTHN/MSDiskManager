using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;

namespace MSDiskManagerData.Helpers
{
    public static class Extensions
    {
        public static IQueryable<T> TrySkipLimit<T>(this IQueryable<T> que, int page, int limit)
        {
            if (limit <= 0) return que;
            var p = page;
            if (page <= 0) p = 0;
            return que.Skip(page * limit).Take(limit);
        }
        public static void AddRange<T>(this ConcurrentBag<T> col, IEnumerable<T> c)
        {
            foreach (var i in c) col.Add(i);
        }
        public static bool IsEmpty(this string str) => str.Trim().Length == 0;
        public static bool IsNotEmpty(this string str) => str.Trim().Length > 0;
        public static bool IsEmpty<T>(this ICollection<T> list) => list.Count == 0;
        public static bool IsNotEmpty<T>(this ICollection<T> list) => list.Count > 0;
        public static ConcurrentBag<T> AsConcurrentBag<T>(this IEnumerable<T> list)
        {
            return new ConcurrentBag<T>(list);
        }
        public static string ToTitleCase(this string str)
        {
            if (str == null || str.Length == 0) return "";
            var s = str.Trim();
            if (s.Length == 0) return s;
            try
            {
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                var res = textInfo.ToTitleCase(s.ToLower());
                return res;
            }
            catch (Exception)
            {
                return str;
            }
        }
        public async static Task<(List<T> success, List<T> failure)> ForEachLimitedAsync<T>(this List<T> ts, Func<T, Task> action, int limit)
        {
            long count = ts.Count;
            ConcurrentBag<T> bag = new ConcurrentBag<T>();
            if (limit > ts.Count)
            {
                ts.ForEach(async t => await action(t));
                return (ts, null);
            }
            else
            {
                var pause = new PauseTokenSource();
                var active = 0;
                long done = 0;
                foreach (var t in ts)
                    await Task.Run(async () =>
                    {
                        {
                            await pause.Token.WaitWhilePausedAsync();
                            if (Interlocked.Increment(ref active) >= limit)
                            {
                                pause.IsPaused = true;
                            }
                            try
                            {
                                await action(t);
                                Interlocked.Increment(ref done);
                                if (pause.IsPaused && Interlocked.Decrement(ref active) < limit)
                                {
                                    pause.IsPaused = false;
                                }
                            }
                            catch (Exception)
                            {
                                bag.Add(t);
                                Interlocked.Increment(ref done);
                                if (pause.IsPaused && Interlocked.Decrement(ref active) < limit)
                                {
                                    pause.IsPaused = false;
                                }
                            }

                        }
                    });
                while (Interlocked.Read(ref done) < count)
                {
                    await Task.Delay(100);
                }
                return (ts, bag.ToList());
            }

        }
    }
}
