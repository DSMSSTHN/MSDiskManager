using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MSDM_IO
{
    public enum ExistsStrategy
    {
        None,
        Rename,
        Replace,
        Skip,
        Merge
    }
    public static class MSDMIO
    {
        private static string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static char randomLetter => letters[new Random().Next(letters.Length)];
        public static async Task<(bool success, string additional)> AddFile(string oldPath,string newPath, Action<long> progress = null, ExistsStrategy addFileStrategy = ExistsStrategy.None, CancellationToken? cancels = null)
        {
            if (oldPath == null || !File.Exists(oldPath) || newPath == null || newPath.Length == 0) throw new ArgumentException("Given pathes are not valid");
            var op = oldPath.Trim().ToLower().Replace("\\\\", "\\").Replace("/", "\\").Replace(";", "\\");
            var np = newPath.Trim().ToLower().Replace("\\\\", "\\").Replace("/", "\\").Replace(";", "\\");
            var exists = File.Exists(newPath);
            var addedLetters = "";
            if (exists)
            {
                switch (addFileStrategy)
                {
                    case ExistsStrategy.None:
                    case ExistsStrategy.Merge:
                        return (false, null);
                    case ExistsStrategy.Rename:
                        var name = makeNewFileName(np);
                        np = name.newPath;
                        addedLetters = name.addedLetters;
                        break;
                    case ExistsStrategy.Replace:
                        try { File.Delete(newPath); } catch(Exception e) { return (false, e.Message); }
                        break;
                    case ExistsStrategy.Skip:
                        return (true, "");
                }
            }
            try
            {
                await CopyToAsync(oldPath, newPath, progress: progress, cancels: cancels);
                return (true,addedLetters);
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }


        }
        public static (bool success, string additional) CreateDirectory(string path, ExistsStrategy strategy = ExistsStrategy.None)
        {
            if (path == null| path.Length == 0) throw new ArgumentException("Given path are not valid");
            var p = path.Trim().ToLower().Replace("\\\\", "\\").Replace("/", "\\").Replace(";", "\\"); ;
            var exists = Directory.Exists(p);
            var added = "";
            if (exists)
            {
                switch (strategy)
                {
                    case ExistsStrategy.None:
                        return (false,null);
                    case ExistsStrategy.Rename:
                        var n = makeNewDirName(p);
                        p = n.newPath;
                        added = n.addedLetters;
                        break;
                    case ExistsStrategy.Replace:
                        try { Directory.Delete(p, true); } catch(Exception e) { return (false, e.Message); }
                        break;
                    case ExistsStrategy.Skip:
                    case ExistsStrategy.Merge:
                        return (true, "");
                }
            }
            try
            {
                Directory.CreateDirectory(p);
                return (true, added);
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }
        public static (string newPath, string addedLetters) makeNewDirName(string path)
        {
            var p = path;
            var added = "";
            while (Directory.Exists(p))
            {
                var c = randomLetter;
                p += c;
                added += c;
            }
            return (p, added);
        }
        private static (string newPath, string addedLetters) makeNewFileName(string path)
        {
            var slash = path.LastIndexOf("\\") + 1;
            var dot = path.LastIndexOf(".", slash);
            var s = path;
            var ext = "";
            if(dot > 0)
            {
                s = path.Substring(0, dot);
                ext = path.Substring(dot + 1);
            }
            var addedChars = "";
            while(File.Exists(s + ext))
            {
                var c = randomLetter;
                s += c;
                addedChars += c;
            }
            return (s + ext,addedChars);
        }
        public static async Task CopyToAsync(string oldPath,
            string newPath, int bufferSize = 1024 * 1024, Action<long> progress = null, CancellationToken? cancels = null)
        {
            try
            {
                using (var source = new FileStream(oldPath, FileMode.Open, FileAccess.Read))
                using (var target = new FileStream(newPath, FileMode.Create, FileAccess.Write))
                {
                    var buffer = new byte[bufferSize];
                    var total = 0L;
                    int amtRead;
                    do
                    {
                        amtRead = 0;
                        while (amtRead < bufferSize)
                        {
                            var numBytes = cancels == null ? await source.ReadAsync(buffer,
                                                                  amtRead,
                                                                  bufferSize - amtRead) : await source.ReadAsync(buffer,
                                                                  amtRead,
                                                                  bufferSize - amtRead, (CancellationToken)cancels);
                            if (numBytes == 0)
                            {
                                break;
                            }
                            amtRead += numBytes;
                        }
                        total += amtRead;
                        if (cancels == null) await target.WriteAsync(buffer, 0, amtRead);
                        else await target.WriteAsync(buffer, 0, amtRead, (CancellationToken)cancels);
                        if (progress != null)
                        {
                            progress(amtRead);
                        }
                    } while (amtRead == bufferSize);

                }
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                if (cancels?.IsCancellationRequested ?? false)
                {
                    if (File.Exists(newPath))
                        try
                        {
                            File.Delete(newPath);

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                }
            }

        }
    }
}
