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
    public class DirectoryRepository : BaseRepository
    {

        public DirectoryRepository(bool isTest = false)
        {
            IsTest = isTest;
        }
        public async Task<DirectoryEntity> CreateDirectory(long? parentId, string name, string description = "", DirectoryExistsStrategy directoryExistsStrategy = DirectoryExistsStrategy.Rename, bool dontAddToDB = false)
        {
            if (name == null || name.IsEmpty()) return null;
            var parent = parentId == null ? null : await GetDirectory((long)parentId);
            var path = "";
            if (parent != null) path = parent.Path + (parent.Path[parent.Path.Length - 1] == '\\' ? "" : '\\') + name;
            var dn = name;


            var dir = new DirectoryEntity { Name = name, ParentId = parentId, Path = path, OnDeskName = dn, Description = name };
            return await CreateDirectory(dir, directoryExistsStrategy, dontAddToDB);
        }
        public async Task<DirectoryEntity> AddToDbOnly(DirectoryEntity directory)
        {
            directory.AddingDate = new NodaTime.Instant();
            directory.MovingDate = new NodaTime.Instant();
            if (directory.ParentId != null)
            {
                try
                {
                    var ctx = await context();
                    var parent = await ctx.Directories.FirstOrDefaultAsync(d => d.Id == directory.ParentId);
                    directory.AncestorIds = parent.AncestorIds.ToList();
                    directory.AncestorIds.Add((long)directory.ParentId);
                    repotFinished();
                }
                catch (Exception)
                {
                    repotFinished();
                    throw;
                }
            }
            var tags = directory.DirectoryTags;
            directory.Parent = null;
            directory.Parent = null; directory.DirectoryTags = null;
            directory.Children = null;
            directory.Files = null;

            try
            {
                var ctx = await context();
                await ctx.Directories.AddAsync(directory);
                await ctx.SaveChangesAsync();
                repotFinished();
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
            if (Globals.IsNotNullNorEmpty(tags))
            {
                foreach (var dt in tags)
                {
                    try
                    {
                        await AddTag((long)directory.Id, (long)dt.TagId);
                    }
                    catch (Exception)
                    {
                        return directory;
                    }
                }
            }

            return directory;
        }
        public async Task<DirectoryEntity> Update(DirectoryEntity directory)
        {
            try
            {
                var ctx = await context();
                ctx.Directories.Update(directory);
                await ctx.SaveChangesAsync();
                repotFinished();
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
            return directory;
        }
        public async Task<DirectoryEntity> CreateDirectory(DirectoryEntity directory, DirectoryExistsStrategy directoryExistsStrategy = DirectoryExistsStrategy.Rename, bool dontAddToDB = false)
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
            if (dontAddToDB) return directory;
            return await AddToDbOnly(directory);
        }
        public async Task<DirectoryEntity> AddDirectory(DirectoryEntity directory, string oldPath, DirectoryExistsStrategy directoryExistsStratigy = DirectoryExistsStrategy.Rename, bool move = false, bool dontAddToDB = false)
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
            if (dontAddToDB) return directory;
            directory.AddingDate = new NodaTime.Instant();
            directory.MovingDate = new NodaTime.Instant();
            try
            {
                var ctx = await context();

                if (directory.ParentId != null)
                {
                    if (directory.Parent == null)
                    {
                        directory.Parent = await ctx.Directories.FirstOrDefaultAsync(d => d.Id == directory.ParentId);
                    }
                    directory.AncestorIds = directory.Parent.AncestorIds.ToList();
                    directory.AncestorIds.Add((long)directory.ParentId);
                }
                var tags = directory.DirectoryTags;
                directory.Parent = null;
                directory.Parent = null; directory.DirectoryTags = null;
                directory.Children = null;
                directory.Files = null;
                await ctx.Directories.AddAsync(directory);
                await ctx.SaveChangesAsync();
                repotFinished();
                if (Globals.IsNotNullNorEmpty(tags))
                {
                    foreach (var dt in tags)
                    {
                        await AddTag((long)directory.Id, (long)dt.TagId);
                    }
                }

            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }

            return directory;
        }
        //public async Task<(Boolean success, string message)> Update(DirectoryEntity directory)
        //{
        //    if (directory.Id < 0) return (false, "Not Valid Id");
        //    if (directory.Name == null || directory.Name.Trim().Length == 0) return (false, "Not Valid Name");
        //    try
        //    {
        //        ctx.Update(directory);
        //        await ctx.SaveChangesAsync();
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
                var ctx = await context();
                var directory = await ctx.Directories.FirstOrDefaultAsync(d => d.Id == id);
                if (directory == null) return (false, "No directory has such an id");
                directory.Name = newName;
                ctx.Update(directory);
                await ctx.SaveChangesAsync();
                repotFinished();
                return (true, null);
            }
            catch (Exception e)
            {
                repotFinished();
                return (false, e.Message);
            }
        }

        public async Task<(Boolean success, string message, DirectoryEntity directory)> ChangeDescription(long id, string description)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var ctx = await context();
                var directory = await ctx.Directories.FirstOrDefaultAsync(d => d.Id == id);
                if (directory == null) return (false, "No directory has such an id", null);
                directory.Description = description;
                ctx.Update(directory);
                await ctx.SaveChangesAsync();
                repotFinished();
                return (true, null, directory);
            }
            catch (Exception e)
            {
                repotFinished();
                return (false, e.Message, null);
            }
        }
        public async Task<List<ImageThumbnail>> LoadThumbnails(long? parentId)
        {
            var result = new List<ImageThumbnail>();
            try
            {
                var ctx = await context();
                result = await ctx.Files.Include(f => f.Thumbnail).Where(f => f.FileType == FileType.Image && f.ParentId == parentId && f.Thumbnail != null).Select(f => f.Thumbnail).ToListAsync();
                repotFinished();
                var empty = result.Where(t => t.Thumbnail == null).ToList();
                if (Globals.IsNotNullNorEmpty(empty))
                {
                    var fRep = new FileRepository();
                    foreach (var e in empty) e.Thumbnail = await fRep.GetThumbnail(e.FileId);
                }
                return result;
            }
            catch (Exception)
            {
                repotFinished();
                return result;
            }
        }
        public async Task<(Boolean success, string message, DirectoryEntity directory)> ChangeIsHidden(long id, bool isHidden)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var ctx = await context();
                var directory = await ctx.Directories.FirstOrDefaultAsync(d => d.Id == id);
                if (directory == null) return (false, "No directory has such an id", null);
                directory.IsHidden = isHidden;
                ctx.Update(directory);
                await ctx.SaveChangesAsync();
                repotFinished();
                return (true, null, directory);
            }
            catch (Exception e)
            {
                repotFinished();
                return (false, e.Message, null);
            }
        }
        public async Task<bool> AddTag(long? id, long? tagId)
        {
            if (id == null || id < 0 || tagId == null || tagId < 0) return false;
            try
            {
                var ctx = await context();
                await ctx.DirectoryTags.AddAsync(new DirectoryTag { DirectoryId = (long)id, TagId = (long)tagId });
                await ctx.SaveChangesAsync();
                repotFinished();
                return true;
            }
            catch (Exception e)
            {

                repotFinished();
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
            try
            {
                var ctx = await context();
                var tag = await ctx.Tags.FirstOrDefaultAsync(t => t.Name.ToLower().Trim() == name.ToLower().Trim());
                if (tag == null)
                {
                    tag = new Tag { Name = name, Color = (color < 0 ? 0 : (color > 10 ? 10 : color)) };
                }
                repotFinished();
                var result = await AddTag(id, tag);

                return result;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        public async Task<(Boolean success, string message, DirectoryEntity directory)> Move(long id, DirectoryEntity newDirectory, DirectoryExistsStrategy directoryExistsStratigy = DirectoryExistsStrategy.Rename)
        {

            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var ctx = await context();
                var directory = await ctx.Directories.FirstOrDefaultAsync(d => d.Id == id);
                if (directory == null) return (false, "No directory has such an id", null);
                var oldFullPath = directory.FullPath;
                var oldpath = directory.Path;
                if (newDirectory == null)
                {
                    directory.Path = "";
                    directory.Parent = null;
                    directory.ParentId = null;
                    directory.AncestorIds = new List<long>();
                    Directory.Move(oldFullPath, directory.FullPath);
                }
                else
                {

                    directory.Path = newDirectory.Path + (newDirectory.Path[newDirectory.Path.Length - 1] == '\\' ? "" : '\\') + directory.OnDeskName;
                    directory.Parent = null;
                    directory.ParentId = newDirectory.Id;
                    directory.AncestorIds = newDirectory.AncestorIds.ToList();
                    directory.AncestorIds.Add((long)newDirectory.Id);
                }
                if (Directory.Exists(directory.FullPath))
                {
                    switch (directoryExistsStratigy)
                    {

                        case DirectoryExistsStrategy.Replace:
                            Directory.Delete(directory.FullPath);
                            break;
                        case DirectoryExistsStrategy.Rename:
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
                            break;
                        case DirectoryExistsStrategy.Skip:
                            return (false, "directory already exists", null);
                        default:
                            throw new DirectoryAlreadtExistsException(directory.FullPath);
                    }

                    Directory.Move(oldFullPath, directory.FullPath);
                }
                directory.MovingDate = new NodaTime.Instant();
                ctx.Update(directory);
                await updateDecendantsPath((long)directory.Id, oldpath, directory.Path);
                await ctx.SaveChangesAsync();
                repotFinished();
                return (true, null, directory);
            }
            catch (Exception e)
            {
                repotFinished();
                return (false, e.Message, null);
            }
        }
        private async Task updateDecendantsPath(long ancestorId, string oldAncestorPath, string newAncestorPath)
        {
            try
            {
                var ctx = await context();
                var dirs = await ctx.Directories.Where(d => d.AncestorIds.Contains(ancestorId)).Select(d => new DirectoryEntity { Id = d.Id, Path = d.Path }).ToListAsync();
                foreach (var d in dirs)
                {
                    ctx.Directories.Attach(d);
                    d.Path = d.Path.Replace(oldAncestorPath, newAncestorPath);
                }
                var files = await ctx.Files.Where(d => d.AncestorIds.Contains(ancestorId)).Select(d => new FileEntity { Id = d.Id, Path = d.Path }).ToListAsync();
                foreach (var f in files)
                {
                    ctx.Files.Attach(f);
                    f.Path = f.Path.Replace(oldAncestorPath, newAncestorPath);
                }
                repotFinished();
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        public async Task<(Boolean success, string message, DirectoryEntity directory)> Copy(long id, string copyPath, DirectoryExistsStrategy directoryExistsStratigy = DirectoryExistsStrategy.Rename)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            if (copyPath == null || copyPath.Trim().Length == 0) return (false, "New path is not valid", null);
            try
            {
                var ctx = await context();
                var directory = await ctx.Directories.FirstOrDefaultAsync(d => d.Id == id);
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
                repotFinished();
                return (true, null, directory);
            }
            catch (Exception e)
            {
                repotFinished();
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, DirectoryEntity directory)> DeleteDirectory(long id)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var ctx = await context();
                var directory = await ctx.Directories.FirstOrDefaultAsync(d => d.Id == id);
                if (directory == null) return (false, "No directory has such an id", null);

                Directory.Delete(directory.FullPath);
                ctx.Directories.Remove(directory);
                await ctx.SaveChangesAsync();
                repotFinished();
                return (true, null, directory);
            }
            catch (Exception e)
            {
                repotFinished();
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, DirectoryEntity directory)> DeleteReferenceOnly(long id)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var ctx = await context();
                var directory = await ctx.Directories.FirstOrDefaultAsync(d => d.Id == id);
                if (directory == null) return (false, "No directory has such an id", null);

                ctx.Directories.Remove(directory);
                await ctx.SaveChangesAsync();
                repotFinished();
                return (true, null, directory);
            }
            catch (Exception e)
            {
                repotFinished();
                return (false, e.Message, null);
            }
        }

        public async Task<DirectoryEntity> GetDirectory(long id)

        {
            try
            {
                var ctx = await context();
                var result = await ctx.Directories.FirstOrDefaultAsync(d => d.Id == id);
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        public async Task<DirectoryEntity> GetDirectoryFull(long id)
        {
            try
            {
                var ctx = await context();
                var result = await ctx.Directories.Include(d => d.Files).IgnoreAutoIncludes().Include(d => d.Children).IgnoreAutoIncludes().FirstOrDefaultAsync(d => d.Id == id);
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        public async Task<List<DirectoryEntity>> FilterDirectories(DirectoryFilter filter)
        {
            try
            {
                var ctx = await context();
                var que = ctx.Directories.AsQueryable().QDirectoryFilter(ctx, filter);
                var str = que.ToQueryString();
                repotFinished();
                return await que.ToListAsync();
            }
            catch (Exception e)
            {

                repotFinished();
                var str2 = e.Message;
                return new List<DirectoryEntity>();
            }
        }


        public async Task<List<FileEntity>> GetFiles(long? id)
        {
            try
            {
                var ctx = await context();
                if (id == null || id < 0) return await ctx.Files.Where(f => f.ParentId == null).ToListAsync();
                var result = await ctx.Files.Where(f => f.ParentId == id).ToListAsync();
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }

        }
        public async Task<List<DirectoryEntity>> GetChildren(long? id)
        {
            try
            {
                var ctx = await context();
                if (id == null || id < 0) return await ctx.Directories.Where(f => f.ParentId == null).ToListAsync();
                var result = await ctx.Directories.Where(f => f.ParentId == id).ToListAsync();
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }

        }
        public async Task<List<FileEntity>> GetDescendantFiles(long id)
        {
            try
            {
                var ctx = await context();
                if (id < 0) return new List<FileEntity>();
                var result = await ctx.Files.Where(f => f.ParentId == id || f.AncestorIds.Contains(id)).ToListAsync();
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        public async Task<List<DirectoryEntity>> GetDescendantDirectories(long id)
        {
            try
            {
                var ctx = await context();
                if (id < 0) return new List<DirectoryEntity>();
                var result = await ctx.Directories.Where(f => f.ParentId == id || f.AncestorIds.Contains(id)).ToListAsync();
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished(); throw;
            }
        }

    }

    public static class DirectoryQueryExtensions
    {
        public static IQueryable<DirectoryEntity> QName(this IQueryable<DirectoryEntity> que, string name, bool includeDirectoryName, bool IncludeDescription)
        {
            if (Globals.IsNullOrEmpty(name)) return que;
            var ns = name.Trim().ToLower().Split(" ").Where(n => n.IsNotEmpty()).ToList();
            var q = que;
            foreach (var n in ns)
            {
                q = que.Where(d => d.Name.ToLower().Contains(n));
            }
            if (includeDirectoryName)
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
            if (Globals.IsNotNullNorEmpty(tagIds)) return que.QTags(context, tagIds);
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
            if (filter.ParentId != null && filter.ParentId >= -1)
            {
                q = q.QDirectory(filter.ParentId);
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
