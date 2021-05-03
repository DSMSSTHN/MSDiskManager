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
    public class DirectoryRepository
    {

        private MSDM_DBContext context;
        private bool IsTest { get; set; }
        public DirectoryRepository(bool isTest = false)
        {
            IsTest = isTest;
            this.context = new MSDM_DBContext(IsTest);
            this.context.Database.EnsureCreated();
        }
        public async Task<DirectoryEntity> CreateDirectory(long? parentId, string name, string description = "", DirectoryExistsStrategy directoryExistsStrategy = DirectoryExistsStrategy.Rename)
        {
            if (name == null || name.IsEmpty()) return null;
            var parent = parentId == null ? null : await GetDirectory((long)parentId);
            var path = "";
            if (parent != null) path = parent.Path + (parent.Path[parent.Path.Length - 1] == '/' ? "" : "/") + name;
            var dn = name;
            
            
            var dir = new DirectoryEntity { Name = name,ParentId = parentId, Path = path,OnDeskName = dn, Description = name };
            return await CreateDirectory(dir, directoryExistsStrategy);
        }
        public async Task<DirectoryEntity> CreateDirectory(DirectoryEntity directory, DirectoryExistsStrategy directoryExistsStrategy = DirectoryExistsStrategy.Rename)
        {

            if (directory == null || directory.Path == null || Globals.IsNullOrEmpty(directory.Name)) throw new ArgumentException("Directory Or Path or name were null");
            if (Globals.IsNullOrEmpty(directory.OnDeskName)) directory.OnDeskName = directory.Name;
            if (Directory.Exists(directory.FullPath))
            {
                switch (directoryExistsStrategy)
                {
                    case DirectoryExistsStrategy.Replace:
                        Directory.Delete(directory.FullPath);
                        break;
                    case DirectoryExistsStrategy.Rename:

                        while (Directory.Exists(directory.FullPath))
                        {
                            directory.Path += "_";
                            directory.OnDeskName += "_";
                        }
                        break;
                    case DirectoryExistsStrategy.Skip:
                        return null;
                    default:
                        throw new DirectoryAlreadtExistsException(directory.FullPath);
                }
            }
            
                Directory.CreateDirectory(directory.FullPath);
           
            directory.AddingDate = new NodaTime.Instant();
            directory.MovingDate = new NodaTime.Instant();
            if (directory.ParentId != null)
            {
                if (directory.Parent == null)
                {
                    directory.Parent = await context.Directories.FirstOrDefaultAsync(d => d.Id == directory.ParentId);
                }
                directory.AncestorIds = directory.Parent.AncestorIds.ToList();
                directory.AncestorIds.Add((long)directory.ParentId);
            }
            await context.Directories.AddAsync(directory);
            await context.SaveChangesAsync();
            return directory;
        }
        public async Task<DirectoryEntity> AddDirectory(DirectoryEntity directory, string oldPath, DirectoryExistsStrategy directoryExistsStratigy = DirectoryExistsStrategy.Rename, bool move = false)
        {
            if (directory == null || directory.Path == null || oldPath == null) throw new ArgumentException("Directory Or Old Path were null");
            if (!Directory.Exists(oldPath))
            {
                throw new DirectoryNotFoundException($"no directory was found at the given location: ${oldPath}");
            }
            if (Directory.Exists(directory.FullPath))
            {
                switch (directoryExistsStratigy)
                {
                    case DirectoryExistsStrategy.Replace:
                        Directory.Delete(directory.FullPath);
                        break;
                    case DirectoryExistsStrategy.Rename:

                        while (Directory.Exists(directory.FullPath))
                        {
                            directory.Path += "_";
                            directory.OnDeskName += "_";

                        }
                        break;
                    case DirectoryExistsStrategy.Skip:
                        return null;
                    default:
                        throw new DirectoryAlreadtExistsException(directory.FullPath);
                }
            }
            if (move)
            {
                Directory.Move(oldPath, directory.FullPath);
            }
            else
            {
                //foreach (string dirPath in Directory.GetDirectories(oldPath, "*", SearchOption.AllDirectories))
                //{
                //    Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
                //}

                ////Copy all the files & Replaces any files with the same name
                //foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                //{
                //    File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
                //}
            }
            directory.AddingDate = new NodaTime.Instant();
            directory.MovingDate = new NodaTime.Instant();
            if (directory.ParentId != null)
            {
                if (directory.Parent == null)
                {
                    directory.Parent = await context.Directories.FirstOrDefaultAsync(d => d.Id == directory.ParentId);
                }
                directory.AncestorIds = directory.Parent.AncestorIds.ToList();
                directory.AncestorIds.Add((long)directory.ParentId);
            }
            await context.Directories.AddAsync(directory);
            await context.SaveChangesAsync();
            return directory;
        }
        //public async Task<(Boolean success, string message)> Update(DirectoryEntity directory)
        //{
        //    if (directory.Id < 0) return (false, "Not Valid Id");
        //    if (directory.Name == null || directory.Name.Trim().Length == 0) return (false, "Not Valid Name");
        //    try
        //    {
        //        context.Update(directory);
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
                var directory = await context.Directories.FirstOrDefaultAsync(d => d.Id == id);
                if (directory == null) return (false, "No directory has such an id");
                directory.Name = newName;
                context.Update(directory);
                await context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }
        
        public async Task<(Boolean success, string message, DirectoryEntity directory)> ChangeDescription(long id, string description)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var directory = await context.Directories.FirstOrDefaultAsync(d => d.Id == id);
                if (directory == null) return (false, "No directory has such an id", null);
                directory.Description = description;
                context.Update(directory);
                await context.SaveChangesAsync();
                return (true, null, directory);
            }
            catch (Exception e)
            {
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, DirectoryEntity directory)> ChangeIsHidden(long id, bool isHidden)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var directory = await context.Directories.FirstOrDefaultAsync(d => d.Id == id);
                if (directory == null) return (false, "No directory has such an id", null);
                directory.IsHidden = isHidden;
                context.Update(directory);
                await context.SaveChangesAsync();
                return (true, null, directory);
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
                await context.DirectoryTags.AddAsync(new DirectoryTag { DirectoryId = (long)id, TagId = (long)tagId });
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
            if (tag == null)
            {
                tag = new Tag { Name = name, Color = (color < 0 ? 0 : (color > 10 ? 10 : color)) };
            }
            return await AddTag(id, tag);
        }
        public async Task<(Boolean success, string message, DirectoryEntity directory)> Move(long id, DirectoryEntity newDirectory, DirectoryExistsStrategy directoryExistsStratigy = DirectoryExistsStrategy.Rename)
        {

            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var directory = await context.Directories.FirstOrDefaultAsync(d => d.Id == id);
                if (directory == null) return (false, "No directory has such an id", null);
                var oldPath = directory.FullPath;
                if (newDirectory == null)
                {
                    directory.Path = "";
                    directory.Parent = null;
                    directory.ParentId = null;
                    directory.AncestorIds = new List<long>();
                    Directory.Move(oldPath, directory.FullPath);
                }
                else
                {

                    directory.Path = newDirectory.Path + (newDirectory.Path[newDirectory.Path.Length - 1] == '/' ? "" : "/") + directory.OnDeskName;
                    directory.Parent = newDirectory;
                    directory.ParentId = newDirectory.Id;
                    directory.AncestorIds = newDirectory.AncestorIds.ToList();
                    directory.AncestorIds.Add((long)newDirectory.Id);
                    if (Directory.Exists(directory.FullPath))
                    {
                        switch (directoryExistsStratigy)
                        {

                            case DirectoryExistsStrategy.Replace:
                                Directory.Delete(directory.FullPath);
                                break;
                            case DirectoryExistsStrategy.Rename:
                                while (Directory.Exists(directory.FullPath))
                                {
                                    directory.Path += "_";
                                    directory.OnDeskName += "_";

                                }
                                break;
                            case DirectoryExistsStrategy.Skip:
                                return (false, "directory already exists", null);
                            default:
                                throw new DirectoryAlreadtExistsException(directory.FullPath);
                        }
                    }
                    Directory.Move(oldPath, directory.FullPath);
                }
                directory.MovingDate = new NodaTime.Instant();
                context.Update(directory);
                await context.SaveChangesAsync();
                return (true, null, directory);
            }
            catch (Exception e)
            {
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, DirectoryEntity directory)> Copy(long id, string copyPath, DirectoryExistsStrategy directoryExistsStratigy = DirectoryExistsStrategy.Rename)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            if (copyPath == null || copyPath.Trim().Length == 0) return (false, "New path is not valid", null);
            try
            {
                var directory = await context.Directories.FirstOrDefaultAsync(d => d.Id == id);
                if (directory == null) return (false, "No directory has such an id", null);

                foreach (string dirPath in Directory.GetDirectories(directory.FullPath, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(directory.FullPath, copyPath));
                }

                //Copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(directory.FullPath, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(directory.FullPath, copyPath), true);
                }
                return (true, null, directory);
            }
            catch (Exception e)
            {
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, DirectoryEntity directory)> DeleteDirectory(long id)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var directory = await context.Directories.FirstOrDefaultAsync(d => d.Id == id);
                if (directory == null) return (false, "No directory has such an id", null);

                Directory.Delete(directory.FullPath);
                context.Directories.Remove(directory);
                await context.SaveChangesAsync();
                return (true, null, directory);
            }
            catch (Exception e)
            {
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, DirectoryEntity directory)> DeleteReferenceOnly(long id)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var directory = await context.Directories.FirstOrDefaultAsync(d => d.Id == id);
                if (directory == null) return (false, "No directory has such an id", null);

                context.Directories.Remove(directory);
                await context.SaveChangesAsync();
                return (true, null, directory);
            }
            catch (Exception e)
            {
                return (false, e.Message, null);
            }
        }

        public async Task<DirectoryEntity> GetDirectory(long id)
        {
            return await context.Directories.FirstOrDefaultAsync(d => d.Id == id);
        }
        public async Task<DirectoryEntity> GetDirectoryFull(long id)
        {
            return await context.Directories.Include(d => d.Files).IgnoreAutoIncludes().Include(d => d.Children).IgnoreAutoIncludes().FirstOrDefaultAsync(d => d.Id == id);
        }
        public async Task<List<DirectoryEntity>> FilterDirectories(DirectoryFilter filter)
        {
            var que = context.Directories.AsQueryable().QDirectoryFilter(context, filter);
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


        public async Task<List<FileEntity>> GetFiles(long? id)
        {
            if (id == null || id < 0) return await context.Files.Where(f => f.ParentId == null).ToListAsync();
            return await context.Files.Where(f => f.ParentId == id).ToListAsync();
        }
        public async Task<List<DirectoryEntity>> GetChildren(long? id)
        {
            if (id == null || id < 0) return await context.Directories.Where(f => f.ParentId == null).ToListAsync();
            return await context.Directories.Where(f => f.ParentId == id).ToListAsync();
        }
        public async Task<List<FileEntity>> GetDescendantFiles(long id)
        {
            if (id < 0) return new List<FileEntity>();
            return await context.Files.Where(f => f.ParentId == id || f.AncestorIds.Contains(id)).ToListAsync();
        }
        public async Task<List<DirectoryEntity>> GetDescendantDirectories(long id)
        {
            if (id < 0) return new List<DirectoryEntity>();
            return await context.Directories.Where(f => f.ParentId == id || f.AncestorIds.Contains(id)).ToListAsync();
        }

    }

    public static class DirectoryQueryExtensions
    {
        public static IQueryable<DirectoryEntity> QName(this IQueryable<DirectoryEntity> que, string name, bool includeDirectoryName, bool IncludeDescription)
        {
            if (Globals.IsNullOrEmpty(name)) return que;
            var ns = name.Trim().ToLower().Split(" ").Where(n => n.IsNotEmpty()).ToList();
            var q = que;
            foreach(var n in ns)
            {
                q = que.Where(d => d.Name.ToLower().Contains(n));
            }
            if (includeDirectoryName) {
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
        public static IQueryable<DirectoryEntity> QTags(this IQueryable<DirectoryEntity> que, MSDM_DBContext context, List<long> tagIds)
        {
            if (Globals.IsNullOrEmpty(tagIds)) return que;
            return que.Join(context.DirectoryTags.Where(ft => tagIds.Contains(ft.TagId)), d => d.Id, ft => ft.DirectoryId, (f, ft) => f);
        }
        public static IQueryable<DirectoryEntity> QTags(this IQueryable<DirectoryEntity> que, MSDM_DBContext context, string name)
        {
            if (Globals.IsNullOrEmpty(name)) return que;
            var ns = name.Trim().ToLower().Split(" ").Where(n => n.IsNotEmpty());
            var que2 = context.Tags.Include(t => t.FileTags).IgnoreAutoIncludes();
            foreach (var n in ns)
            {
                que2 = que2.Where(t => t.Name.ToLower().Contains(n));
            }
            return que.Join(que2.Distinct().SelectMany(t => t.DirectoryTags), d => d.Id, ft => ft.DirectoryId, (f, ft) => f);
        }
        public static IQueryable<DirectoryEntity> QTags(this IQueryable<DirectoryEntity> que, MSDM_DBContext context, List<long> tagIds, string name)
        {
            if (Globals.IsNullOrEmpty(tagIds)) return que.QTags(context, tagIds);
            return que.QTags(context, name);
        }
        public static IQueryable<DirectoryEntity> QDirectory(this IQueryable<DirectoryEntity> que, long? directoryId)
        {
            if (directoryId == null || directoryId < -1) return que;
            if (directoryId == -1) return que.Where(e => e.ParentId == null);
            return que.Where(e => e.ParentId == directoryId);
        }
        public static IQueryable<DirectoryEntity> QAncestors(this IQueryable<DirectoryEntity> que, List<long> ancestorIds)
        {
            if (Globals.IsNullOrEmpty(ancestorIds)) return que;
            return que.Where(e => ancestorIds.Any(aid => e.AncestorIds.Contains(aid)));
        }
        public static IQueryable<DirectoryEntity> QHidden(this IQueryable<DirectoryEntity> que, bool withHidden = false)
        {
            if (withHidden) return que;
            return que.Where(d => !d.IsHidden);
        }
        
        public static IQueryable<DirectoryEntity> QOrder(this IQueryable<DirectoryEntity> que, DirectoryOrder order)
        {
            switch (order)
            {
                case DirectoryOrder.Name:
                    return que.OrderBy(q => q.Name);
                case DirectoryOrder.NameDesc:
                    return que.OrderByDescending(q => q.Name);
                case DirectoryOrder.AddTime:
                    return que.OrderBy(q => q.AddingDate);
                case DirectoryOrder.AddTimeDesc:
                    return que.OrderByDescending(q => q.AddingDate);
                case DirectoryOrder.MoveTime:
                    return que.OrderBy(q => q.MovingDate);
                case DirectoryOrder.MoveTimeDesc:
                    return que.OrderByDescending(q => q.MovingDate);
            }
            return que.OrderBy(q => q.Name);
        }
        public static IQueryable<DirectoryEntity> QExclude(this IQueryable<DirectoryEntity> que, List<long> idsToExclude)
        {
            if (Globals.IsNullOrEmpty(idsToExclude)) return que;
            return que.Where(e => !idsToExclude.Contains((long)e.Id));
        }
        public static IQueryable<DirectoryEntity> QPageLimit(this IQueryable<DirectoryEntity> que, int page, int limit)
        {
            if (limit <= 0) return que;
            var p = page < 0 ? 0 : page;
            return que.Skip(p * limit).Take(limit);
        }
        public static IQueryable<DirectoryEntity> QDirectoryFilter(this IQueryable<DirectoryEntity> que, MSDM_DBContext context, DirectoryFilter filter)
        {
            if (filter == null) return que;
            var q = que.QName(filter.Name, filter.IncludeDirectoryNameInSearch, filter.IncludeDescriptionInSearch);
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
            q = q.QOrder(filter.Order);
            return q.QPageLimit(filter.Page, filter.Limit);
        }

    }




    public class DirectoryAlreadtExistsException : IOException
    {
        public DirectoryAlreadtExistsException()
        {

        }
        public DirectoryAlreadtExistsException(string message) : base(message)
        {

        }
        public DirectoryAlreadtExistsException(string path, string message = "Directory with same name exists at the given path") : base(message)
        {
            this.Path = path;
        }
        public String Path { get; set; } = "";
    }
    public enum DirectoryExistsStrategy
    {
        ThrowException,
        Replace,
        Rename,
        //Merge,//TODO: Check To Make It Possible
        Skip
    }
}
