using MSDiskManagerData.Data.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MSDiskManagerData.Helpers
{
    public static class IOHelper
    {


        public async static Task AddIO(this BaseEntity entity, bool move, Action<long> progress = null, CancellationToken? cancels = null)
        {
            if (Globals.IsNullOrEmpty(entity.FullPath)) throw new ArgumentException("No distanation was given");

            if (entity is MSFile)
            {
                if (Globals.IsNullOrEmpty(entity.OldPath)) throw new ArgumentException("No source was given");
                if (!File.Exists(entity.OldPath)) throw new IOException("No file was found at source");
                var file = entity as MSFile;
                if (!file.Path.Contains(file.OnDeskName))
                {
                    var slash = file.Path.LastIndexOf('\\') + 1;
                    var dot = file.Path.LastIndexOf('.');
                    var length = 0;
                    length = (dot > 0 && dot > slash) ? (dot - slash) : file.Path.Length - slash;
                    file.OnDeskName = file.Path.Substring(slash, length);
                }
                var e = file.Extension;
                while (File.Exists(file.FullPath))
                {

                    var n = file.OnDeskName;
                    var f = n + (Globals.IsNotNullNorEmpty(e) ? ("." + e) : "");
                    var r = n + "_" + (Globals.IsNotNullNorEmpty(e) ? ("." + e) : "");
                    file.Path = file.Path.Replace(f, r);
                    file.OnDeskName = n + "_";
                }
                await file.OldPath.CopyToWithProgressAsync(file.FullPath, 1024 * 1024, progress, cancels);
                if (move && File.Exists(file.OldPath)) File.Delete(file.OldPath);
            }
            else
            {
                var directory = entity as MSDirecotry;
                if (!directory.Path.Contains(directory.OnDeskName))
                {
                    var p = directory.Path;
                    if (p[p.Length - 1] == '\\' || p[p.Length - 1] == '/') p = p.Substring(0, p.Length - 1);
                    var slash = p.LastIndexOf('\\') + 1;
                    var length = p.Length - slash;
                    directory.OnDeskName = directory.Path.Substring(slash, length);
                }
                while (Directory.Exists(directory.FullPath))
                {
                    directory.Path += "_";
                    directory.OnDeskName += "_";
                }
                Directory.CreateDirectory(directory.FullPath);
            }
        }





        public static async Task CopyToWithProgressAsync(this String oldPath,
            string newPath,
                                                 int bufferSize = 4096,
                                                 Action<long> progress = null, CancellationToken? cancels = null)
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
