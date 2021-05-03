using Microsoft.EntityFrameworkCore;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Entities.Relations;
using MSDiskManagerData.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Repositories
{
    public class FileRepository
    {

        private MSDM_DBContext context;
        private bool IsTest { get; set; }
        public FileRepository(bool isTest = false)
        {
            IsTest = isTest;
            this.context = new MSDM_DBContext(IsTest);
            this.context.Database.EnsureCreated();
        }

        public async Task<FileEntity> AddFile(FileEntity file, string oldPath, FileExistsStrategy fileExistsStratigy = FileExistsStrategy.Rename, bool move = false)
        {
            if (file == null || file.Path == null || oldPath == null) throw new ArgumentException("File Or Old Path were null");
            if (!File.Exists(oldPath))
            {
                throw new FileNotFoundException($"no file was found at the given location: ${oldPath}");
            }
            if (File.Exists(file.FullPath))
            {
                switch (fileExistsStratigy)
                {
                    case FileExistsStrategy.Replace:
                        File.Delete(file.FullPath);
                        break;
                    case FileExistsStrategy.Rename:
                        while (File.Exists(file.FullPath))
                        {
                            var extensionIndex = file.Path.LastIndexOf(".");
                            file.Path = file.Path.Insert(extensionIndex, "_");
                            file.OnDeskName += "_";
                        }
                        break;
                    case FileExistsStrategy.Skip:
                        return null;
                    default:
                        throw new FileAlreadtExistsException(file.FullPath);
                }
            }
            if (move)
            {

                File.Move(oldPath, file.FullPath);
            }
            else
            {
                File.Copy(oldPath, file.FullPath);
            }
            file.AddingDate = new NodaTime.Instant();
            file.MovingDate = new NodaTime.Instant();
            if (file.ParentId != null)
            {
                if (file.Parent == null)
                {
                    file.Parent = await context.Directories.FirstOrDefaultAsync(d => d.Id == file.ParentId);
                }
                file.AncestorIds = file.Parent.AncestorIds.ToList();
                file.AncestorIds.Add((long)file.ParentId);
            }
            await context.Files.AddAsync(file);
            await context.SaveChangesAsync();
            return file;
        }
        //public async Task<(Boolean success, string message)> Update(FileEntity file)
        //{
        //    if (file.Id < 0) return (false, "Not Valid Id");
        //    if (file.Name == null || file.Name.Trim().Length == 0) return (false, "Not Valid Name");
        //    try
        //    {
        //        context.Update(file);
        //        await context.SaveChangesAsync();
        //        return (true, null);
        //    }
        //    catch (Exception e)
        //    {
        //        return (false, e.Message);
        //    }
        //}
        public async Task<(Boolean success, string message)> ChangeName(long id, string newName)
        {
            if (id < 0) return (false, "Not Valid Id");
            if (newName == null || newName.Trim().Length == 0) return (false, "Not Valid Name");
            try
            {
                var file = await context.Files.FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return (false, "No file has such an id");
                file.Name = newName;
                context.Update(file);
                await context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }
        public async Task<(Boolean success, string message, FileEntity file)> ChangeType(long id, FileType type)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var file = await context.Files.FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return (false, "No file has such an id", null);
                file.FileType = type;
                context.Update(file);
                await context.SaveChangesAsync();
                return (true, null, file);
            }
            catch (Exception e)
            {
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, FileEntity file)> ChangeDescription(long id, string description)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var file = await context.Files.FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return (false, "No file has such an id", null);
                file.Description = description;
                context.Update(file);
                await context.SaveChangesAsync();
                return (true, null, file);
            }
            catch (Exception e)
            {
                return (false, e.Message, null);
            }
        }
        public async Task<bool> AddTag(long? id, long? tagId)
        {
            if (id == null || id < 0 || tagId == null || tagId < 0) return false;
            try
            {
                await context.FileTags.AddAsync(new FileTag { FileId = (long)id, TagId = (long)tagId });
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {

                return false;
            }
        }
        public async Task<bool> AddTag(long? id, Tag tag)
        {
            if (id == null || id < 0) return false;
            if (tag == null) return false;
            if (tag.Id == null || tag.Id < 0)
            {
                tag.Id = null;
                try
                {
                    var t = await new TagRepository(IsTest).AddTag(tag);
                    if (t == null) return false;
                    tag.Id = t.Id;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return await AddTag(id, tag);
        }
        public async Task<bool> AddTag(long? id, string name, int color)
        {
            if (Globals.IsNullOrEmpty(name)) return false;

            var tag = await context.Tags.FirstOrDefaultAsync(t => t.Name.ToLower().Trim() == name.ToLower().Trim());
            if(tag == null)
            {
                tag = new Tag { Name = name, Color = (color < 0 ? 0 : (color > 10 ? 10 : color)) };
            }
            return await AddTag(id, tag);
        }


        public async Task<(Boolean success, string message, FileEntity file)> ChangeIsHidden(long id, bool isHidden)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var file = await context.Files.FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return (false, "No file has such an id", null);
                file.IsHidden = isHidden;
                context.Update(file);
                await context.SaveChangesAsync();
                return (true, null, file);
            }
            catch (Exception e)
            {
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, FileEntity file)> Move(long id, DirectoryEntity newDirectory, FileExistsStrategy fileExistsStratigy = FileExistsStrategy.Rename)
        {

            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var file = await context.Files.FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return (false, "No file has such an id", null);
                var oldPath = file.FullPath;
                if (newDirectory == null)
                {
                    file.Path = "";
                    file.Parent = null;
                    file.ParentId = null;
                    file.AncestorIds = new List<long>();
                    File.Move(oldPath, file.FullPath);
                }
                else
                {

                    file.Path = newDirectory.Path + (newDirectory.Path[newDirectory.Path.Length - 1] == '/' ? "" : "/") + file.Name + "." + file.Extension;
                    file.Parent = newDirectory;
                    file.ParentId = newDirectory.Id;
                    file.AncestorIds = newDirectory.AncestorIds.ToList();
                    file.AncestorIds.Add((long)newDirectory.Id);
                    if (File.Exists(file.FullPath))
                    {
                        switch (fileExistsStratigy)
                        {

                            case FileExistsStrategy.Replace:
                                File.Delete(file.FullPath);
                                break;
                            case FileExistsStrategy.Rename:
                                while (File.Exists(file.FullPath))
                                {
                                    var extensionIndex = file.Path.LastIndexOf(".");
                                    file.Path = file.Path.Insert(extensionIndex, "_");
                                    file.OnDeskName += "_";
                                }

                                break;
                            case FileExistsStrategy.Skip:
                                return (false, "file already exists", null);
                            default:
                                throw new FileAlreadtExistsException(file.FullPath);
                        }
                    }
                    File.Move(oldPath, file.FullPath);
                }
                file.MovingDate = new NodaTime.Instant();
                context.Update(file);
                await context.SaveChangesAsync();
                return (true, null, file);
            }
            catch (Exception e)
            {
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, FileEntity file)> Copy(long id, string copyPath, FileExistsStrategy fileExistsStratigy = FileExistsStrategy.Rename)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            if (copyPath == null || copyPath.Trim().Length == 0) return (false, "New path is not valid", null);
            try
            {
                var file = await context.Files.FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return (false, "No file has such an id", null);

                File.Copy(file.FullPath, copyPath);
                return (true, null, file);
            }
            catch (Exception e)
            {
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, FileEntity file)> DeleteFile(long id)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var file = await context.Files.FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return (false, "No file has such an id", null);

                File.Delete(file.FullPath);
                context.Files.Remove(file);
                await context.SaveChangesAsync();
                return (true, null, file);
            }
            catch (Exception e)
            {
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, FileEntity file)> DeleteReferenceOnly(long id)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var file = await context.Files.FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return (false, "No file has such an id", null);

                context.Files.Remove(file);
                await context.SaveChangesAsync();
                return (true, null, file);
            }
            catch (Exception e)
            {
                return (false, e.Message, null);
            }
        }

        public async Task<FileEntity> GetFile(long id)
        {
            return await context.Files.FirstOrDefaultAsync(f => f.Id == id);
        }
        public async Task<List<FileEntity>> FilterFiles(FileFilter filter)
        {
            var que = context.Files.AsQueryable().QFileFilter(context, filter);
            try
            {
                var str = que.ToQueryString();
            }
            catch (Exception e)
            {

                var str2 = e.Message;
            }
            return await que.ToListAsync();
        }




    }

    public static class FileQueryExtensions
    {
        public static IQueryable<FileEntity> QName(this IQueryable<FileEntity> que, string name, bool includeFileName, bool IncludeDescription)
        {
            if (Globals.IsNullOrEmpty(name)) return que;
            var ns = name.Trim().ToLower().Split(" ").Where(n => n.IsNotEmpty());
            var q = que;
            foreach (var n in ns)
            {
                q = que.Where(d => d.Name.ToLower().Contains(n));
            }
            if (includeFileName)
            {
                foreach (var n in ns)
                {
                    q = que.Where(d => d.OnDeskName.ToLower().Contains(n));
                }
            }
            if (IncludeDescription)
            {
                foreach (var n in ns)
                {
                    q = que.Where(d => d.Description.ToLower().Contains(n));
                }
            }
            return q;
        }
        public static IQueryable<FileEntity> QTags(this IQueryable<FileEntity> que, MSDM_DBContext context, List<long> tagIds)
        {
            if (Globals.IsNullOrEmpty(tagIds)) return que;
            return que.Join(context.FileTags.Where(ft => tagIds.Contains(ft.TagId)), f => f.Id, ft => ft.FileId, (f, ft) => f);
        }
        public static IQueryable<FileEntity> QTags(this IQueryable<FileEntity> que, MSDM_DBContext context, string name)
        {
            if (Globals.IsNullOrEmpty(name)) return que;
            var ns = name.Trim().ToLower().Split(" ").Where(n => n.IsNotEmpty());
            var que2 = context.Tags.Include(t => t.FileTags).IgnoreAutoIncludes();
            foreach(var n in ns)
            {
                que2 = que2.Where(t => t.Name.ToLower().Contains(n));
            }
            return que.Join(que2.Distinct().SelectMany(t => t.FileTags), f => f.Id, ft => ft.FileId, (f, ft) => f);
        }
        public static IQueryable<FileEntity> QTags(this IQueryable<FileEntity> que, MSDM_DBContext context, List<long> tagIds, string name)
        {
            if (Globals.IsNullOrEmpty(tagIds)) return que.QTags(context, tagIds);
            return que.QTags(context, name);
        }
        public static IQueryable<FileEntity> QDirectory(this IQueryable<FileEntity> que, long? directoryId)
        {
            if (directoryId == null || directoryId < -1) return que;
            if (directoryId == -1) return que.Where(e => e.ParentId == null);
            return que.Where(e => e.ParentId == directoryId);
        }
        public static IQueryable<FileEntity> QAncestors(this IQueryable<FileEntity> que, List<long> ancestorIds)
        {
            if (Globals.IsNullOrEmpty(ancestorIds)) return que;
            return que.Where(e => ancestorIds.Any(aid => e.AncestorIds.Contains(aid)));
        }
        public static IQueryable<FileEntity> QHidden(this IQueryable<FileEntity> que, bool withHidden = false)
        {
            if (withHidden) return que;
            return que.Where(f => !f.IsHidden);
        }
        public static IQueryable<FileEntity> QTypes(this IQueryable<FileEntity> que, List<FileType> types)
        {
            if (Globals.IsNullOrEmpty(types)) return que;
            return que.Where(e => types.Contains(e.FileType));
        }
        public static IQueryable<FileEntity> QOrder(this IQueryable<FileEntity> que, FileOrder order)
        {
            switch (order)
            {
                case FileOrder.Name:
                    return que.OrderBy(q => q.Name);
                case FileOrder.NameDesc:
                    return que.OrderByDescending(q => q.Name);
                case FileOrder.AddTime:
                    return que.OrderBy(q => q.AddingDate);
                case FileOrder.AddTimeDesc:
                    return que.OrderByDescending(q => q.AddingDate);
                case FileOrder.MoveTime:
                    return que.OrderBy(q => q.MovingDate);
                case FileOrder.MoveTimeDesc:
                    return que.OrderByDescending(q => q.MovingDate);
                case FileOrder.Type:
                    return que.OrderBy(q => q.FileType);
                case FileOrder.TypeDesc:
                    return que.OrderByDescending(q => q.FileType);
            }
            return que.OrderBy(q => q.Name);
        }
        public static IQueryable<FileEntity> QExclude(this IQueryable<FileEntity> que, List<long> idsToExclude)
        {
            if (Globals.IsNullOrEmpty(idsToExclude)) return que;
            return que.Where(e => !idsToExclude.Contains((long)e.Id));
        }
        public static IQueryable<FileEntity> QPageLimit(this IQueryable<FileEntity> que, int page, int limit)
        {
            if (limit <= 0) return que;
            var p = page < 0 ? 0 : page;
            return que.Skip(p * limit).Take(limit);
        }
        public static IQueryable<FileEntity> QFileFilter(this IQueryable<FileEntity> que, MSDM_DBContext context, FileFilter filter)
        {
            if (filter == null) return que;
            var q = que.QName(filter.Name, filter.IncludeFileNameInSearch, filter.IncludeDescriptionInSearch);
            q = q.QHidden(filter.IncludeHidden);
            q = q.QTags(context, filter.tagIds, filter.TagName);
            if (filter.DirectoryId != null && filter.DirectoryId >= -1)
            {
                q = q.QDirectory(filter.DirectoryId);
            }
            else
            {
                q = q.QAncestors(filter.AncestorIds);
            }
            q = q.QExclude(filter.ExcludeIds);
            q = q.QTypes(filter.Types);
            q = q.QOrder(filter.Order);
            return q.QPageLimit(filter.Page, filter.Limit);
        }

    }




    public class FileAlreadtExistsException : IOException
    {
        public FileAlreadtExistsException()
        {

        }
        public FileAlreadtExistsException(string message) : base(message)
        {

        }
        public FileAlreadtExistsException(string path, string message = "File with same name exists at the given path") : base(message)
        {
            this.Path = path;
        }
        public String Path { get; set; } = "";
    }
    public enum FileExistsStrategy
    {
        ThrowException,
        Replace,
        Rename,
        Skip
    }
}
