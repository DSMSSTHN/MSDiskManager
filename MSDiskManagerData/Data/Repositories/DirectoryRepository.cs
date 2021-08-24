using Microsoft.EntityFrameworkCore;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Entities.Relations;
using MSDiskManagerData.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Repositories
{
    public class DirectoryRepository : BaseRepository
    {

        public DirectoryRepository(bool isTest = false)
        {
            IsTest = isTest;
        }
        public async Task<MSDirecotry> CreateDirectory(long? parentId, string name, string description = "", DirectoryExistsStrategy directoryExistsStrategy = DirectoryExistsStrategy.Rename, bool dontAddToDB = false)
        {
            if (name == null || name.IsEmpty()) return null;
            var parent = parentId == null ? null : await GetDirectory((long)parentId);
            var path = "";
            if (parent != null) path = parent.Path + '\\' + name;
            else path = name;

            var dir = new MSDirecotry { Name = name, ParentId = parentId, Path = path, OnDeskName = name, Description = description };
            return await CreateDirectory(dir, directoryExistsStrategy, dontAddToDB);
        }
        public async Task<MSDirecotry> AddToDbOnly(MSDirecotry directory)
        {
            if (MSDM_DBContext.currentDriveId == null) throw new Exception("No Driver was configured yet");
            directory.AddingDate = new NodaTime.Instant();
            directory.MovingDate = new NodaTime.Instant();
            directory.DriveId = MSDM_DBContext.currentDriveId;
            try
            {
                var ctx = await context();
                var exists = await ctx.Directories.FirstOrDefaultAsync(d => d.Path == directory.Path);
                repotFinished();
                if (exists != null) return exists;

            }
            catch (Exception)
            {
                repotFinished();
            }
            if (directory.ParentId != null)
            {
                try
                {
                    var ctx = await context();
                    await directory.LoadParentWithAncestorIds(ctx);
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
        public async Task<MSDirecotry> Update(MSDirecotry directory)
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="directories"></param>
        /// <param name="directoryExistsStrategy"></param>
        /// <param name=""></param>
        /// <param name="dontAddToDB"></param>
        /// <returns></returns>
        public async Task<List<MSDirecotry>> CreateDirectories(List<MSDirecotry> directories,
            DirectoryExistsStrategy directoryExistsStrategy = DirectoryExistsStrategy.Rename
            , bool dontAddToDB = false, CancellationToken? cancels = null)
        {
            if (MSDM_DBContext.currentDriveId == null) throw new Exception("No Driver was configured yet");
            if (cancels?.IsCancellationRequested ?? false) return null;
            if (Globals.IsNullOrEmpty(directories)) throw new Exception("No data was given");
            var waiting = directories.ToList();

            if (cancels?.IsCancellationRequested ?? false) return null;
            List<long> pids = new List<long>();
            List<List<long>> paids = new List<List<long>>();
            try
            {
                var ctx = await context();
                pids = waiting.Where(f => f.ParentId != null && f.Parent == null).Select(f => (long)f.ParentId).OrderBy(id => id).Distinct().ToList();
                paids = await ctx.Directories.Where(d => pids.Contains((long)d.Id)).OrderBy(d => d.Id).Select(d => d.AncestorIds).ToListAsync();
                repotFinished();
            }
            catch (Exception)
            {
                repotFinished();

                throw;
            }
            foreach (var d in waiting)
            {
                if (cancels?.IsCancellationRequested ?? false) return null;
                if (d.ParentId != null)
                {
                    List<long> anc = paids[pids.IndexOf((long)d.ParentId)];
                    anc.Add((long)d.ParentId);
                    d.AncestorIds = anc;
                }
                else
                {
                    d.AncestorIds = new List<long>();
                }
                d.DriveId = MSDM_DBContext.currentDriveId;
            }
            try
            {
                var ctx = await context();
                await ctx.AddRangeAsync(waiting);
                await ctx.SaveChangesAsync();
            }
            catch (Exception e)
            {
                throw;
            }
            return waiting;
        }



        public async Task<MSDirecotry> CreateDirectory(MSDirecotry directory, DirectoryExistsStrategy directoryExistsStrategy = DirectoryExistsStrategy.Rename, bool dontAddToDB = false)
        {
            if (MSDM_DBContext.currentDriveId == null) throw new Exception("No Driver was configured yet");
            if (directory == null || directory.Path == null || Globals.IsNullOrEmpty(directory.Name)) throw new ArgumentException("Directory Or Path or name were null");
            if (Globals.IsNullOrEmpty(directory.OnDeskName)) directory.OnDeskName = directory.Name;
            directory.DriveId = MSDM_DBContext.currentDriveId;
            if (dontAddToDB) return directory;
            return await AddToDbOnly(directory);
        }
        
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

        public async Task<(Boolean success, string message, MSDirecotry directory)> ChangeDescription(long id, string description)
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
        public async Task<(Boolean success, string message, MSDirecotry directory)> ChangeIsHidden(long id, bool isHidden)
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
        public async Task<bool> AddTagRecursive(long? id, long? tagId, bool first = true, MSDM_DBContext cctx = null)
        {
            if (id == null || tagId == null || id < 0 || tagId < 0) return false;
            try
            {
                var nnid = (long)id;
                var nntid = (long)tagId;
                var ctx = cctx ?? await context();
                var fids = await ctx.Files.Include(f => f.FileTags).IgnoreAutoIncludes().Where(f => f.ParentId == id && !f.FileTags.Any(ft => ft.TagId == nntid)).Select(f => f.Id).ToListAsync();
                var dids = await ctx.Directories.Include(d => d.DirectoryTags).IgnoreAutoIncludes().Where(d => d.ParentId == id).Select(d => new { id = d.Id, dts = d.DirectoryTags}).ToListAsync();
                if (dids != null && dids.Count > 0)
                {
                    var dtags = dids.Where(ddt => !ddt.dts.Any(dt => dt.TagId == nntid)).Select(did => new DirectoryTag { DirectoryId = (long)did.id, TagId = (long)tagId }).ToList();
                    await ctx.DirectoryTags.AddRangeAsync(dtags);
                    foreach(var d in dids)
                    {
                        await AddTagRecursive(d.id, tagId,false,ctx);
                    }
                }
                if(first)await ctx.DirectoryTags.AddAsync(new DirectoryTag { DirectoryId = (long)id, TagId = (long)tagId });
                if(fids != null && fids.Count > 0)
                {
                    var ftags = fids.Select(fid => new FileTag { FileId = (long)fid, TagId = (long)tagId }).ToList();
                    await ctx.FileTags.AddRangeAsync(ftags);
                }
                if (first) await ctx.SaveChangesAsync();
                repotFinished();
                return true;
            }
            catch(Exception e)
            {
                repotFinished();
                return false;
            }
        }
        public async Task<bool> RemoveTagRecursive(long? id, long? tagId, bool first = true,MSDM_DBContext cctx = null)
        {
            if (id == null || tagId == null || id < 0 || tagId < 0) return false;
            try
            {
                var nnid = (long)id;
                var nntid = (long)tagId;
                var ctx = cctx ?? await context();
                var fids = await ctx.Files.Include(f => f.FileTags).IgnoreAutoIncludes().Where(f => f.ParentId == id).Select(f => f.Id).ToListAsync();
                var dids = await ctx.Directories.Include(d => d.DirectoryTags).IgnoreAutoIncludes().Where(d => d.ParentId == id).Select(d => d.Id).ToListAsync();
                if (dids != null && dids.Count > 0)
                {
                    var dtags = await ctx.DirectoryTags.Where(dt => dt.TagId == tagId && dids.Contains(dt.DirectoryId)).ToListAsync();
                        //dids.Where(ddt => !ddt.dts.Any(dt => dt.TagId == nntid)).Select(did => new DirectoryTag { DirectoryId = (long)did.id, TagId = (long)tagId }).ToList();
                    ctx.DirectoryTags.RemoveRange(dtags);
                    foreach (var d in dids)
                    {
                        await RemoveTagRecursive(d, tagId, false,ctx);
                    }
                }
                if (first) ctx.DirectoryTags.Remove(new DirectoryTag { DirectoryId = (long)id, TagId = (long)tagId });
                if (fids != null && fids.Count > 0)
                {
                    var ftags = await ctx.FileTags.Where( ft => ft.TagId == tagId && fids.Contains(ft.FileId)).ToListAsync();
                        //fids.Select(fid => new FileTag { FileId = (long)fid, TagId = (long)tagId }).ToList();
                    ctx.FileTags.RemoveRange(ftags);
                }
                if(first)await ctx.SaveChangesAsync();
                repotFinished();
                return true;
            }
            catch (Exception e)
            {
                repotFinished();
                return false;
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
        public async Task<bool> AddTags(long? id, List<long> tagIds)
        {
            if (id == null || Globals.IsNullOrEmpty(tagIds)) return false;
            try
            {
                var ctx = await context();
                var dirTags = tagIds.Select(tid => new DirectoryTag { DirectoryId = (long)id, TagId = tid });
                await ctx.DirectoryTags.AddRangeAsync(dirTags);
                await ctx.SaveChangesAsync();
                repotFinished();
                return true;
            }
            catch (Exception)
            {
                repotFinished();
                return false;
            }
        }
        public async Task<bool> RemoveTag(long? id, long? tagId)
        {
            if (id == null || tagId == null) return false;
            try
            {
                var ctx = await context();
                var fileTag = await ctx.DirectoryTags.FirstAsync(t => t.DirectoryId == id && t.TagId == tagId);
                ctx.DirectoryTags.Remove(fileTag);
                await ctx.SaveChangesAsync();
                repotFinished();
                return true;
            }
            catch (Exception)
            {
                repotFinished();
                return false;
            }
        }
        public async Task<bool> RemoveTags(long? id, List<long> tagIds)
        {
            if (id == null || Globals.IsNullOrEmpty(tagIds)) return false;
            try
            {
                var ctx = await context();
                var fileTags = await ctx.DirectoryTags.Where(t => t.DirectoryId == id && tagIds.Contains(t.TagId)).ToListAsync();
                ctx.DirectoryTags.RemoveRange(fileTags);
                await ctx.SaveChangesAsync();
                repotFinished();
                return true;
            }
            catch (Exception)
            {
                repotFinished();
                return false;
            }
        }

        private async Task updateDecendantsPath(long ancestorId, string oldAncestorPath, string newAncestorPath)
        {
            try
            {
                var ctx = await context();
                var dirs = await ctx.Directories.Where(d => d.AncestorIds.Contains(ancestorId)).Select(d => new MSDirecotry { Id = d.Id, Path = d.Path }).ToListAsync();
                foreach (var d in dirs)
                {
                    ctx.Directories.Attach(d);
                    d.Path = d.Path.Replace(oldAncestorPath, newAncestorPath);
                }
                var files = await ctx.Files.Where(d => d.AncestorIds.Contains(ancestorId)).Select(d => new MSFile { Id = d.Id, Path = d.Path }).ToListAsync();
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
        public async Task<bool> DeletePerPath(string path)
        {
            if (path == null || path.IsEmpty()) return false;
            try
            {
                var ctx = await context();
                var allFiles = await ctx.Files.Where(f => f.Path.Contains(path)).ToListAsync();
                var allDirs = await ctx.Directories.Where(d => d.Path.Contains(path)).ToListAsync();
                if(Globals.IsNotNullNorEmpty(allFiles))ctx.Files.RemoveRange(allFiles);
                if (Globals.IsNotNullNorEmpty(allDirs)) ctx.Directories.RemoveRange(allDirs);
                if (Globals.IsNotNullNorEmpty(allFiles) && Globals.IsNotNullNorEmpty(allDirs)) await ctx.SaveChangesAsync();
                repotFinished();
                return true;
            }
            catch (Exception)
            {
                repotFinished();
                return false;
            }
        }
        public async Task<(Boolean success, string message, MSDirecotry directory)> DeleteDirectory(long id)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var ctx = await context();
                var directory = await ctx.Directories.FirstOrDefaultAsync(d => d.Id == id);
                if (directory == null) return (false, "No directory has such an id", null);

                Directory.Delete(directory.FullPath, true);
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
        public async Task<(Boolean success, string message, MSDirecotry directory)> DeleteReferenceOnly(long id)
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
        public async Task<(Boolean success, string message, List<MSDirecotry> file)> DeleteInvalidReference(ICollection<long> ids)
        {
            if (ids == null || ids.IsEmpty()) return (false, "Not Valid Id", null);
            try
            {
                var ctx = await context();
                var dirs = await ctx.Directories.Where(d => ids.Contains((long)d.Id)).ToListAsync();
                dirs = dirs.Where(f => !Directory.Exists(f.FullPath)).ToList();
                if (Globals.IsNullOrEmpty(dirs)) return (false, "No file has such an id", null);

                ctx.Directories.RemoveRange(dirs);
                await ctx.SaveChangesAsync();
                repotFinished();
                return (true, null, dirs);

            }
            catch (Exception e)
            {
                repotFinished();
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, List<MSDirecotry> directories)> DeleteReferenceOnly(ICollection<long> ids)
        {
            if (ids == null || ids.IsEmpty()) return (false, "Not Valid Id", null);
            try
            {
                var ctx = await context();
                var directories = await ctx.Directories.Where(d => ids.Contains((long)d.Id)).ToListAsync();
                if (Globals.IsNullOrEmpty(directories)) return (false, "No directory has such an id", null);

                ctx.Directories.RemoveRange(directories);
                await ctx.SaveChangesAsync();
                repotFinished();
                return (true, null, directories);

            }
            catch (Exception e)
            {
                repotFinished();
                return (false, e.Message, null);
            }
        }
        
        public async Task<MSDirecotry> GetDirectory(long id)

        {
            try
            {
                var ctx = await context();
                var result = await ctx.Directories.AsNoTracking().Include(d => d.DirectoryTags).ThenInclude(dt => dt.Tag).IgnoreAutoIncludes().FirstOrDefaultAsync(d => d.Id == id);
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        public async Task<MSDirecotry> GetDirectoryFull(long id)
        {
            try
            {
                var ctx = await context();
                var result = await ctx.Directories.AsNoTracking().Include(d => d.DirectoryTags).ThenInclude(dt => dt.Tag).IgnoreAutoIncludes().Include(d => d.Files).IgnoreAutoIncludes().Include(d => d.Children).IgnoreAutoIncludes().FirstOrDefaultAsync(d => d.Id == id);
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        public async Task<List<MSDirecotry>> FilterDirectories(DirectoryFilter filter, string? driverId = null)
        {
            var did = driverId ?? MSDM_DBContext.currentDriveId;
            if (did == null) throw new Exception("No Driver was configured yet");
            try
            {
                var ctx = await context();
                var que = ctx.Directories.AsNoTracking().Include(d => d.DirectoryTags)
                    .ThenInclude(ft => ft.Tag).IgnoreAutoIncludes().Where(d => d.DriveId == did).QDirectoryFilter(ctx, filter);
                var str = que.ToQueryString();
                var result = await que.ToListAsync();

                repotFinished();
                return result;
            }
            catch (Exception e)
            {

                repotFinished();
                var str2 = e.Message;
                return new List<MSDirecotry>();
            }
        }

        public async Task<int> GetItemsCount(long? id)
        {
            try
            {
                var ctx = await context();

                var ds = await ctx.Directories.Where(d => d.ParentId == id).CountAsync();
                var fs = await ctx.Files.Where(d => d.ParentId == id).CountAsync();
                repotFinished();
                return ds + fs;

            }
            catch (Exception)
            {
                repotFinished();
                return 0;
            }
        }
        public async Task<List<MSFile>> GetFiles(long? id)
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
        public async Task<List<MSDirecotry>> GetChildren(long? id)
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
        public async Task<List<MSFile>> GetDescendantFiles(long id)
        {
            try
            {
                var ctx = await context();
                if (id < 0) return new List<MSFile>();
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
        public async Task<List<MSDirecotry>> GetDescendantDirectories(long id)
        {
            try
            {
                var ctx = await context();
                if (id < 0) return new List<MSDirecotry>();
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
        public static IQueryable<MSDirecotry> QName(this IQueryable<MSDirecotry> que, string name, bool includeDirectoryName, bool IncludeDescription)
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
        public static IQueryable<MSDirecotry> QTags(this IQueryable<MSDirecotry> que, MSDM_DBContext context, List<long> tagIds)
        {
            if (Globals.IsNullOrEmpty(tagIds)) return que;
            var tque = context.DirectoryTags.Where(s => tagIds.Contains(s.TagId))
                    .Select(q => new { gid = q.TagId, mid = q.DirectoryId })
                    .GroupBy(s => s.mid).Select(group => new { id = group.Key, count = group.Count() }).Where(ac => ac.count >= tagIds.Count())
                    .Select(ac => ac.id);

            return que.Where(d => tque.Contains(d.Id.Value));
        }
        public static IQueryable<MSDirecotry> QTags(this IQueryable<MSDirecotry> que, MSDM_DBContext context, string name)
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
        public static IQueryable<MSDirecotry> QTags(this IQueryable<MSDirecotry> que, MSDM_DBContext context, List<long> tagIds, string name)
        {
            if (Globals.IsNotNullNorEmpty(tagIds)) return que.QTags(context, tagIds);
            return que.QTags(context, name);
        }
        public static IQueryable<MSDirecotry> QDirectory(this IQueryable<MSDirecotry> que, long? directoryId)
        {
            if (directoryId == null || directoryId < -1) return que;
            if (directoryId == -1) return que.Where(e => e.ParentId == null);
            return que.Where(e => e.ParentId == directoryId);
        }
        public static IQueryable<MSDirecotry> QAncestors(this IQueryable<MSDirecotry> que, List<long> ancestorIds)
        {
            if (Globals.IsNullOrEmpty(ancestorIds)) return que;
            return que.Where(e => ancestorIds.Any(aid => e.AncestorIds.Contains(aid)));
        }
        public static IQueryable<MSDirecotry> QHidden(this IQueryable<MSDirecotry> que, bool withHidden = false)
        {
            if (withHidden) return que;
            return que.Where(d => !d.IsHidden);
        }

        public static IQueryable<MSDirecotry> QOrder(this IQueryable<MSDirecotry> que, DirectoryOrder order)
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
        public static IQueryable<MSDirecotry> QExclude(this IQueryable<MSDirecotry> que, List<long> idsToExclude)
        {
            if (Globals.IsNullOrEmpty(idsToExclude)) return que;
            return que.Where(e => !idsToExclude.Contains((long)e.Id));
        }
        public static IQueryable<MSDirecotry> QPageLimit(this IQueryable<MSDirecotry> que, int page, int limit)
        {
            if (limit <= 0) return que;
            var p = page < 0 ? 0 : page;
            return que.Skip(p * limit).Take(limit);
        }
        public static IQueryable<MSDirecotry> QDirectoryFilter(this IQueryable<MSDirecotry> que, MSDM_DBContext context, DirectoryFilter filter)
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
        Merge,//TODO: Check To Make It Possible
        Skip
    }
}
