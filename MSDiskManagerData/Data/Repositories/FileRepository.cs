using Microsoft.EntityFrameworkCore;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Entities.Relations;
using MSDiskManagerData.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Repositories
{
    public class FileRepository : BaseRepository
    {


        public FileRepository(bool isTest = false)
        {
            IsTest = isTest;
        }
        public  async Task<List<MSFile>> AddToDbOnly(List<MSFile> files)
        {
            var limit = 1000;
            if(files.Count < limit)
            {
                return await _addToDbOnly(files);
            }
            var times = files.Count / limit;
            List<MSFile> success = new List<MSFile>();
            for (int i = 0; i < times; i++)
            {
                success.AddRange(await _addToDbOnly(files.Skip(i * limit).Take(limit).ToList()));
            }
            return success;
        }
        private async Task<List<MSFile>> _addToDbOnly(ICollection<MSFile> files)
        {
            List<MSFile> exists = new List<MSFile>();
            try
            {
                var ctx = await context();
                var pathes = files.Select(f => f.Path);
                exists = await ctx.Files.Where(d => pathes.Contains(d.Path)).ToListAsync();
                repotFinished();
                if (exists.Count == files.Count) return exists;

            }

            catch (Exception)
            {
                repotFinished();
            }
            var existsPathes = exists.Select(e => e.Path);
            var waiting = files.Where(f => !existsPathes.Contains(f.Path)).ToList();
           

            var sucess = await AddFiles(waiting);
            sucess.AddRange(exists);
            return sucess;
        }
        public async Task<MSFile> AddToDbOnly(MSFile file)
        {
            if (MSDM_DBContext.currentDriveId == null) throw new Exception("No Driver Is Selected Yey!!");
            try
            {
                var ctx = await context();
                var exists = await ctx.Files.FirstOrDefaultAsync(d => d.Path == file.Path);
                repotFinished();
                if (exists != null) return exists;

            }

            catch (Exception)
            {
                repotFinished();
            }
            file.AddingDate = new NodaTime.Instant();
            file.MovingDate = new NodaTime.Instant();
            file.DriveId = MSDM_DBContext.currentDriveId;
            try
            {
                var ctx = await context();
                await file.LoadParentWithAncestorIds(ctx);
                var tags = file.FileTags;
                file.Parent = null;
                file.FileTags = null;
                await ctx.Files.AddAsync(file);

                await ctx.SaveChangesAsync();
                repotFinished();
                if (file.FileType == FileType.Image)
                {
                    await saveThumbnail((long)file.Id, file.FullPath);
                }
                if (Globals.IsNotNullNorEmpty(tags))
                {
                    foreach (var ft in tags)
                    {
                        await AddTag((long)file.Id, (long)ft.TagId);
                    }
                }

                return file;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        public async Task<byte[]> GetThumbnail(long id)
        {
            try
            {
                var ctx = await context();
                var thumb = (await ctx.Thumbnails.FirstOrDefaultAsync(t => t.FileId == id))?.Thumbnail ?? null;
                repotFinished();
                if (thumb == null)
                {
                    var file = await GetFile(id);
                    var t = await saveThumbnail((long)file.Id, file.FullPath);
                    thumb = t.Thumbnail;
                }
                return thumb;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        private async Task saveThumbnails(List<MSFile> files)
        {
            if (Globals.IsNullOrEmpty(files)) return;
            List<ImageThumbnail> thumbs = new List<ImageThumbnail>();
            using (var stream = new MemoryStream())
            {
                foreach (var f in files)
                {
                    var thumb = new ImageThumbnail { FileId = (long)f.Id };
                    var img = Image.FromFile(f.FullPath);
                    var width = img.Width;
                    var height = img.Height;
                    var ration = width > height ? (60 / width) : (60 / height);
                    width = width * ration;
                    height = height * ration;
                    var t = img.GetThumbnailImage(width, height, () => false, IntPtr.Zero);
                    t.Save(stream, ImageFormat.Jpeg);
                    thumb.Thumbnail = stream.ToArray();
                    thumbs.Add(thumb);
                    stream.Position = 0;
                    stream.SetLength(0);
                }
            }
            try
            {
                var ctx = await context();
                await ctx.Thumbnails.AddRangeAsync(thumbs);
                await ctx.SaveChangesAsync();
                repotFinished();
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        private async Task<ImageThumbnail> saveThumbnail(long fileId, string fullpath)
        {

            var thumb = new ImageThumbnail { FileId = fileId };
            var img = Image.FromFile(fullpath);
            var width = img.Width;
            var height = img.Height;
            var ration = width > height ? (60 / width) : (60 / height);
            width = width * ration;
            height = height * ration;
            var t = img.GetThumbnailImage(width, height, () => false, IntPtr.Zero);
            using (var stream = new MemoryStream())
            {

                t.Save(stream, ImageFormat.Jpeg);
                thumb.Thumbnail = stream.ToArray();
                stream.Close();
            }
            try
            {
                var ctx = await context();
                await ctx.Thumbnails.AddAsync(thumb);
                await ctx.SaveChangesAsync();
                repotFinished();
                return thumb;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        //failureFunction 0 retry 1 skip 2 break;
        public async Task<List<MSFile>> AddFiles(List<MSFile> files,
            CancellationToken? cancels = null)
        {
            if (MSDM_DBContext.currentDriveId == null) throw new Exception("No Driver Was Configured yet");
            if (cancels?.IsCancellationRequested ?? false) return null;
            if (Globals.IsNullOrEmpty(files)) throw new ArgumentException("File Or Old Path were null");
            var fs = files.Where(f => f != null && f.Path != null && f.OldPath != null).ToList();
            var succeed = new List<MSFile>();
            var images = new List<MSFile>();
            List<long> pids = new List<long>();
            List<List<long>> paids = new List<List<long>>();
            try
            {
                var ctx = await context();
                pids = fs.Where(f => f.ParentId != null && f.Parent == null).Select(f => (long)f.ParentId).OrderBy(id => id).Distinct().ToList();
                paids = await ctx.Directories.Where(d => pids.Contains((long)d.Id)).OrderBy(d => d.Id).Select(d => d.AncestorIds).ToListAsync();
                repotFinished();
            }
            catch (Exception)
            {
                repotFinished();

                throw;
            }
            List<(string op, List<long> tids)> fts = new List<(string op, List<long> tids)>();
            foreach (var f in fs)
            {
                if (cancels?.IsCancellationRequested ?? false) return null;

                var cancel = false;
                f.AddingDate = new NodaTime.Instant();
                f.MovingDate = new NodaTime.Instant();
                f.Parent = null;
                if (Globals.IsNotNullNorEmpty(f.FileTags)) fts.Add((f.OldPath, f.FileTags.Select(ft => ft.TagId).ToList()));
                f.FileTags = null;
                f.Id = null;
                if (f.ParentId != null)
                {
                    List<long> anc = paids[pids.IndexOf((long)f.ParentId)];
                    anc.Add((long)f.ParentId);
                    f.AncestorIds = anc;
                }
                else
                {
                    f.AncestorIds = new List<long>();
                }
                while (!cancel)
                {
                    try
                    {
                        if (cancels?.IsCancellationRequested ?? false) return null;
                        f.DriveId = MSDM_DBContext.currentDriveId;
                        if (f.FileType == FileType.Image) images.Add(f);
                        succeed.Add(f);
                        break;
                    }
                    catch (Exception e)
                    {
                        throw;
                    }
                }
                if (cancel) break;
            }
            if (cancels?.IsCancellationRequested ?? false) return null;
            try
            {
                var ctx = await context();
                await ctx.Files.AddRangeAsync(succeed);
                await ctx.SaveChangesAsync();
                repotFinished();
                var tags = fts.SelectMany(ft => { var id = succeed.FirstOrDefault(f => f.OldPath == ft.op)?.Id; if (id == null) return new List<FileTag>(); return ft.tids.Select(tid => new FileTag { FileId = (long)id, TagId = tid }); }).ToList();
                await saveThumbnails(images);
                await AddFileTags(tags);
                return succeed;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        public async Task AddFileTags(List<FileTag> fileTags)
        {
            if (Globals.IsNullOrEmpty(fileTags)) return;
            try
            {
                var ctx = await context();
                await ctx.FileTags.AddRangeAsync(fileTags);
                await ctx.SaveChangesAsync();
                repotFinished();
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }

        }

        public async Task<(Boolean success, string message)> ChangeName(long id, string newName)
        {
            if (id < 0) return (false, "Not Valid Id");
            if (newName == null || newName.Trim().Length == 0) return (false, "Not Valid Name");
            try
            {
                var ctx = await context();
                var file = await ctx.Files.FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return (false, "No file has such an id");
                file.Name = newName;
                ctx.Update(file);
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
        public async Task<(Boolean success, string message, MSFile file)> ChangeType(long id, FileType type)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var ctx = await context();
                var file = await ctx.Files.FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return (false, "No file has such an id", null);
                file.FileType = type;
                ctx.Update(file);
                await ctx.SaveChangesAsync();
                repotFinished();
                return (true, null, file);
            }
            catch (Exception e)
            {
                repotFinished();
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, MSFile file)> ChangeDescription(long id, string description)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var ctx = await context();
                var file = await ctx.Files.FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return (false, "No file has such an id", null);
                file.Description = description;
                ctx.Update(file);
                await ctx.SaveChangesAsync();
                repotFinished();
                return (true, null, file);
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
                await ctx.FileTags.AddAsync(new FileTag { FileId = (long)id, TagId = (long)tagId });
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
        public async Task<bool> AddTags(long? id, ICollection<long> tagIds)
        {
            if (id == null || Globals.IsNullOrEmpty(tagIds)) return false;
            try
            {
                var ctx = await context();
                var fileTags = tagIds.Select(tid => new FileTag { FileId = (long)id, TagId = tid });
                await ctx.FileTags.AddRangeAsync(fileTags);
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
                var fileTag = await ctx.FileTags.FirstAsync(t => t.FileId == id && t.TagId == tagId);
                ctx.FileTags.Remove(fileTag);
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
                var fileTags = await ctx.FileTags.Where(t => t.FileId == id && tagIds.Contains(t.TagId)).ToListAsync();
                ctx.FileTags.RemoveRange(fileTags);
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


        public async Task<(Boolean success, string message, MSFile file)> ChangeIsHidden(long id, bool isHidden)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var ctx = await context();
                var file = await ctx.Files.FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return (false, "No file has such an id", null);
                file.IsHidden = isHidden;
                ctx.Update(file);
                await ctx.SaveChangesAsync();
                repotFinished();
                return (true, null, file);
            }
            catch (Exception e)
            {
                repotFinished();
                return (false, e.Message, null);
            }
        }

        public async Task<bool> DeletePerPath(string path)
        {
            if (path == null || path.IsEmpty()) return false;
            try
            {
                var ctx = await context();
                var file = await ctx.Files.FirstOrDefaultAsync(f => f.Path == path);
                if (file == null) return true;
                ctx.Files.RemoveRange(file);
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
        public async Task<(Boolean success, string message, MSFile file)> DeleteFile(long id)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var ctx = await context();
                var file = await ctx.Files.FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return (false, "No file has such an id", null);

                File.Delete(file.FullPath);
                ctx.Files.Remove(file);
                await ctx.SaveChangesAsync();
                repotFinished();
                return (true, null, file);
            }
            catch (Exception e)
            {
                repotFinished();
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, MSFile file)> DeleteReferenceOnly(long id)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var ctx = await context();
                var file = await ctx.Files.FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return (false, "No file has such an id", null);

                ctx.Files.Remove(file);
                await ctx.SaveChangesAsync();
                repotFinished();
                return (true, null, file);
            }
            catch (Exception e)
            {
                repotFinished();
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, List<MSFile> file)> DeleteInvalidReference(ICollection<long> ids)
        {
            if (ids == null || ids.IsEmpty()) return (false, "Not Valid Id", null);
            try
            {
                var ctx = await context();
                var files = await ctx.Files.Where(d => ids.Contains((long)d.Id)).ToListAsync();
                files = files.Where(f => !File.Exists(f.FullPath)).ToList();
                if (Globals.IsNullOrEmpty(files)) return (false, "No file has such an id", null);

                ctx.Files.RemoveRange(files);
                await ctx.SaveChangesAsync();
                repotFinished();
                return (true, null, files);

            }
            catch (Exception e)
            {
                repotFinished();
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, List<MSFile> file)> DeleteReferenceOnly(ICollection<long> ids)
        {
            if (ids == null || ids.IsEmpty()) return (false, "Not Valid Id", null);
            try
            {
                var ctx = await context();
                var files = await ctx.Files.Where(d => ids.Contains((long)d.Id)).ToListAsync();
                if (Globals.IsNullOrEmpty(files)) return (false, "No file has such an id", null);

                ctx.Files.RemoveRange(files);
                await ctx.SaveChangesAsync();
                repotFinished();
                return (true, null, files);

            }
            catch (Exception e)
            {
                repotFinished();
                return (false, e.Message, null);
            }
        }

        public async Task<MSFile> GetFile(long id)
        {
            try
            {
                var ctx = await context();
                var result = await ctx.Files.AsNoTracking().Include(f => f.FileTags).ThenInclude(ft => ft.Tag).IgnoreAutoIncludes().FirstOrDefaultAsync(f => f.Id == id);
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        public async Task<List<MSFile>> FilterFiles(FileFilter filter, string? driverId = null)
        {
            var d = driverId ?? MSDM_DBContext.currentDriveId;
            if (d == null) throw new Exception("No Driver is configured yet");
            try
            {
                var ctx = await context();
                var que = ctx.Files.AsNoTracking().Include(f => f.FileTags).ThenInclude(ft => ft.Tag).IgnoreAutoIncludes().Where(f => f.DriveId == d).QFileFilter(ctx, filter);
                var str = que.ToQueryString();
                repotFinished();
                var result = await que.ToListAsync();
                return result;
            }
            catch (Exception e)
            {
                repotFinished();
                var str2 = e.Message;
                throw;
            }

        }




    }

    public static class FileQueryExtensions
    {
        public static IQueryable<MSFile> QName(this IQueryable<MSFile> que, string name, bool includeFileName, bool IncludeDescription)
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
        public static IQueryable<MSFile> QTags(this IQueryable<MSFile> que, MSDM_DBContext context, List<long> tagIds)
        {
            if (Globals.IsNullOrEmpty(tagIds)) return que;
            return que.Join((context).FileTags.Where(ft => tagIds.Contains(ft.TagId)), f => f.Id, ft => ft.FileId, (f, ft) => f);
        }
        public static IQueryable<MSFile> QTags(this IQueryable<MSFile> que, MSDM_DBContext context, string name)
        {
            if (Globals.IsNullOrEmpty(name)) return que;
            var ns = name.Trim().ToLower().Split(" ").Where(n => n.IsNotEmpty());
            var que2 = (context.Tags.Include(t => t.FileTags).IgnoreAutoIncludes());
            foreach (var n in ns)
            {
                que2 = que2.Where(t => t.Name.ToLower().Contains(n));
            }
            return que.Join(que2.Distinct().SelectMany(t => t.FileTags), f => f.Id, ft => ft.FileId, (f, ft) => f);
        }
        public static IQueryable<MSFile> QTags(this IQueryable<MSFile> que, MSDM_DBContext context, List<long> tagIds, string name)
        {
            if (Globals.IsNotNullNorEmpty(tagIds)) return que.QTags(context, tagIds);
            return que.QTags(context, name);
        }
        public static IQueryable<MSFile> QDirectory(this IQueryable<MSFile> que, long? directoryId)
        {
            if (directoryId == null || directoryId < -1) return que;
            if (directoryId == -1) return que.Where(e => e.ParentId == null);
            return que.Where(e => e.ParentId == directoryId);
        }
        public static IQueryable<MSFile> QAncestors(this IQueryable<MSFile> que, List<long> ancestorIds)
        {
            if (Globals.IsNullOrEmpty(ancestorIds)) return que;
            return que.Where(e => ancestorIds.Any(aid => e.AncestorIds.Contains(aid)));
        }
        public static IQueryable<MSFile> QHidden(this IQueryable<MSFile> que, bool withHidden = false)
        {
            if (withHidden) return que;
            return que.Where(f => !f.IsHidden);
        }
        public static IQueryable<MSFile> QTypes(this IQueryable<MSFile> que, List<FileType> types)
        {
            if (Globals.IsNullOrEmpty(types)) return que;
            return que.Where(e => types.Contains(e.FileType));
        }
        public static IQueryable<MSFile> QOrder(this IQueryable<MSFile> que, FileOrder order)
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
        public static IQueryable<MSFile> QExclude(this IQueryable<MSFile> que, List<long> idsToExclude)
        {
            if (Globals.IsNullOrEmpty(idsToExclude)) return que;
            return que.Where(e => !idsToExclude.Contains((long)e.Id));
        }
        public static IQueryable<MSFile> QPageLimit(this IQueryable<MSFile> que, int page, int limit)
        {
            if (limit <= 0) return que;
            var p = page < 0 ? 0 : page;
            return que.Skip(p * limit).Take(limit);
        }
        public static IQueryable<MSFile> QFileFilter(this IQueryable<MSFile> que, MSDM_DBContext context, FileFilter filter)
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
