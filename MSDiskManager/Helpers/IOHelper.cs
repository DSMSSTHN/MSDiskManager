using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManager.Helpers
{
    public static class IOHelper
    {
        public static List<DirectoryViewModel> GetDirectories(this string path, DirectoryViewModel parent = null)
        {
            if (path.IsFile()) throw new IOException("Get Items was called on a file instaed of  a folder for path :[" + path + "]");
            var directories = Directory.GetDirectories(path);
            var diresult = directories.Select(d => d.GetFullDirectory(parent));
            return diresult.ToList();
        }
        public static List<FileViewModel> GetFiles(this string path, DirectoryViewModel parent = null)
        {
            if (path.IsFile()) throw new IOException("Get Items was called on a file instaed of  a folder for path :[" + path + "]");
            var files = Directory.GetFiles(path);
            var firlesult = files.Select(f =>
            {
                return GetFile(f, parent);
            });
            return firlesult.ToList();
        }
        public static FileViewModel GetFile(this string path, DirectoryViewModel parent = null)
        {
            var p = path.FixSeperator();
            var sep = '\\';
            var ne = path.GetFileNameAndExtension();
            return new FileViewModel
            {
                Name = ne.name,
                OnDeskName = ne.name,
                OriginalPath = p,
                Path = (parent?.Path ?? "") + sep + ne.name + "." + ne.extension,
                Parent = parent,
                FileType = GetFileType(ne.extension),
                Extension = ne.extension
            };
        }
        public static DirectoryViewModel GetFullDirectory(this string path, DirectoryViewModel parent = null)
        {
            var p = path.FixSeperator();
            var sep = '\\';
            var dir = new DirectoryViewModel
            {
                Name = p.GetDirectoryName(),
                OnDeskName = p.GetDirectoryName(),
                OriginalPath = p,
                Path = (parent?.Path ?? "") + sep + p.GetDirectoryName(),
                Parent = parent,

            };
            dir.Children = p.GetDirectories(dir);
            dir.Files = p.GetFiles(dir);
            dir.ItemsCount = dir.Files.Count;
            foreach(var c  in dir.Children)
            {
                dir.ItemsCount += c.ItemsCount;
            }
            return dir;
        }
        public static bool IsFile(this string path)
        {
            FileAttributes attr = File.GetAttributes(path);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory) return false;
            return true;
        }
        public static bool IsDirectory(this string path)
        {
            FileAttributes attr = File.GetAttributes(path);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory) return true;
            return false;
        }

        public static string GetDirectoryName(this string path)
        {
            
            var p = path.Trim();
            if (p.Length == 0) return path;
            p = path.FixSeperator();
            var sep = '\\';
            if (p[p.Length - 1] == sep) p = p.Remove(p.Length - 1);
            if (!p.Contains(sep)) return p;
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
            return (p, "");
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
