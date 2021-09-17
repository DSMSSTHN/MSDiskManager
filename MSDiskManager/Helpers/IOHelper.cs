using MSDiskManager.Dialogs;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MSDiskManager.Helpers
{
    public static class IOHelper
    {
        public static (List<DirectoryViewModel> dirs, List<string> pathes) GetDirectories(this string path, DirectoryViewModel parent = null)
        {
           
            if (@path.IsFile()) throw new IOException("Get Items was called on a file instaed of  a folder for path :[" + path + "]");
            var directories = Directory.GetDirectories(@path);
            var dirs = new List<DirectoryViewModel>();
            var pathes = new List<string>();
            foreach (var d in directories)
            {
                var diresult = @d.GetFullDirectory(parent);
                if (diresult.directory == null) continue;
                dirs.Add(diresult.directory);
                pathes.AddRange(diresult.pathes);
            }
            return (dirs, pathes);
        }
        public static (List<FileViewModel> files, List<string> pathes) GetFiles(this string path, DirectoryViewModel parent = null)
        {
            if (path.IsFile()) throw new IOException("Get Items was called on a file instaed of  a folder for path :[" + path + "]");
            var files = Directory.GetFiles(path);
            var fs = new List<FileViewModel>();
            var pathes = new List<string>();
            foreach (var f in files)
            {
                var filesult = GetFile(f, parent);
                fs.Add(filesult.file);
                pathes.Add(filesult.path);
            }
            return (fs,pathes);
        }
        public static (FileViewModel file, string path) GetFile(this string path, DirectoryViewModel parent = null)
        {
            var p = path.FixSeperator();
            var sep = '\\';
            var ne = path.GetFileNameAndExtension();
            var file = new FileViewModel
            {
                Name = ne.name,
                OnDeskName = ne.name,
                OriginalPath = p,
                Path = (parent?.Path ?? "") + sep + ne.name + "." + ne.extension,
                Parent = parent,
                FileType = GetFileType(ne.extension),
                Extension = ne.extension
            };
            return (file, path);
        }
        public static (DirectoryViewModel directory, List<string> pathes) GetFullDirectory(this string path, DirectoryViewModel parent = null)
        {
            try
            {
                if (!Directory.Exists(@path))
                {
                    MSMessageBox.Show($"Couldn't find Directory[{path}]. Ignoring directory");
                    return (null, null);
                }
            }
            catch (Exception ex)
            {
                MSMessageBox.Show($"Couldn't add Directory[{path}]\nException:[{ex.Message + (ex.InnerException == null ? "" : "\n" + ex.InnerException)}]");
                return (null, null);
            }
            var p = @path.FixSeperator();
            var sep = '\\';
            var dir = new DirectoryViewModel
            {
                Name = p.GetDirectoryName(),
                OnDeskName = p.GetDirectoryName(),
                OriginalPath = @p,
                Path = (parent?.Path ?? "") + sep + p.GetDirectoryName(),
                Parent = parent,

            };
            var diresult = p.GetDirectories(dir);
            var filesult = p.GetFiles(dir);
            dir.Children = diresult.dirs;
            dir.Files = filesult.files;
            dir.ItemsCount = dir.Files.Count;
            foreach (var c in dir.Children)
            {
                dir.ItemsCount += c.ItemsCount;
            }
            var pathes = diresult.pathes.ToList();
            pathes.AddRange(filesult.pathes);
            pathes.Add(@path);
            return (dir, pathes);
        }
        public static bool IsFile(this string path)
        {
            if (Directory.Exists(@path)) return false;
            FileAttributes attr = File.GetAttributes(@path);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory) return false;
            return true;
        }
        public static bool IsDirectory(this string path)
        {
            FileAttributes attr = File.GetAttributes(@path);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory) return true;
            return false;
        }

        public static string GetDirectoryName(this string path)
        {

            var p = @path.Trim();
            if (p.Length == 0) return path;
            p = path.FixSeperator();
            var sep = '\\';
            if (p[p.Length - 1] == sep) p = p.Remove(p.Length - 1);
            if (!p.Contains(sep)) return @p;
            return p.Substring(p.LastIndexOf(sep) + 1);
        }
        public static (string name, string extension) GetFileNameAndExtension(this string path)
        {
            var p = path.Trim();
            if (p.Length == 0) return (p, "");
            p = path.FixSeperator();
            var sep = '\\';
            if (p[p.Length - 1] == sep) p = p.Remove(p.Length - 1);
            if (p.Contains(sep)) p = p.Substring(p.LastIndexOf(sep) + 1);
            var index = p.LastIndexOf('.');
            if (index > 0 && index < p.Length - 1) return (p.Substring(0, index), p.Substring(index + 1));
            return (@p, "");
        }
        public static FileType GetFileType(string extension)
        {
            switch (extension.Trim().ToLower())
            {
                case "jpg":
                case "png":
                case "bmp":
                case "tiff":
                case "gif":
                case "icon":
                    return FileType.Image;
                case "mp3":
                case "wav":
                case "ogg":
                case "wma":
                case "acc":
                case "amr":
                    return FileType.Music;
                case "mp4":
                case "ts":
                case "3gp":
                case "wmv":
                    return FileType.Video;
                case "pdf":
                case "docm":
                case "docx":
                case "dot":
                case "dotx":
                    return FileType.Document;
                case "txt":
                    return FileType.Text;
                case "zip":
                case "rar":
                case "tar":
                case "7z":
                    return FileType.Compressed;
            }
            return FileType.Unknown;
        }
        public static string FixSeperator(this string path)
        {
            return path.Replace('/', '\\');
        }
    }
}
