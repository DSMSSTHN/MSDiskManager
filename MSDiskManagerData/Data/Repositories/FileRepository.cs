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
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Repositories
{
    public class FileRepository : BaseRepository
    {


        public FileRepository(bool isTest = false)
        {
            IsTest = isTest;
        }
        public async Task<FileEntity> AddFileToDBOnly(FileEntity file)
        {
            file.AddingDate = new NodaTime.Instant();
            file.MovingDate = new NodaTime.Instant();

            try
            {
                var ctx = await context();
                if (file.ParentId != null)
                {
                    if (file.Parent == null)
                    {
                        file.Parent = await ctx.Directories.FirstOrDefaultAsync(d => d.Id == file.ParentId);
                    }
                    file.AncestorIds = file.Parent.AncestorIds.ToList();
                    file.AncestorIds.Add((long)file.ParentId);
                }
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
        private async Task saveThumbnails(List<FileEntity> files)
        {
            if (Globals.IsNullOrEmpty(files)) return;
            List<ImageThumbnail> thumbs = new List<ImageThumbnail>();
            using (var stream = new MemoryStream())
            {
                foreach(var f in files)
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
        public async Task<List<FileEntity>> AddFiles(List<FileEntity> files,
            Func<FileEntity, Exception, Task<int>> failureFunction,
            FileExistsStrategy fileExistsStratigy = FileExistsStrategy.Rename,
            bool move = false,
            bool dontAddToDB = false)
        {
            if (Globals.IsNullOrEmpty(files)) throw new ArgumentException("File Or Old Path were null");
            var fs = files.Where(f => f != null && f.Path != null && f.OldPath != null).ToList();
            var limit = 500;
            var images = new List<FileEntity>();
            var succeed = new List<FileEntity>();
            IDictionary<long, List<long>> idancs = new Dictionary<long, List<long>>();
            try
            {
                var ctx = await context();
                var parentIds = fs.Where(f => f.ParentId != null && f.Parent == null).Select(f => f.ParentId).Distinct();
                idancs = await ctx.Directories.Where(d => parentIds.Contains(d.Id)).Select(d => new KeyValuePair<long, List<long>>((long)d.Id, d.AncestorIds)).ToDictionaryAsync(k => k.Key, k => k.Value);
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
                if (f.FileType == FileType.Image) images.Add(f);
                var cancel = false;
                f.AddingDate = new NodaTime.Instant();
                f.MovingDate = new NodaTime.Instant();
                f.Parent = null;
                if (Globals.IsNotNullNorEmpty(f.FileTags)) fts.Add(new { op = f.OldPath, tids = f.FileTags.Select(ft => ft.TagId}));
                f.FileTags = null;
                f.Id = null;
                if (f.ParentId != null)
                {
                    List<long> anc = new List<long>();
                    idancs.TryGetValue((long)f.ParentId, out anc);
                    f.AncestorIds = anc;
                }
                while (!cancel)
                {
                    try
                    {
                        await AddFile(f, f.OldPath,move:move, dontAddToDB: true);
                        succeed.Add(f);
                        break;
                    }
                    catch (Exception e)
                    {
                        switch (await failureFunction(f, e))
                        {
                            case 0:
                                continue;
                            case 1:
                                break;
                            default:
                                cancel = true;
                                break;
                        }
                    }
                }
                if (cancel) break;
            }

            try
            {
                var ctx = await context();
                await ctx.Files.AddRangeAsync(succeed);
                await ctx.SaveChangesAsync();
                repotFinished();
                var tags = fts.SelectMany(ft => { var id = succeed.FirstOrDefault(f => f.OldPath == ft.op)?.Id; if (id == null) return new List<FileTag>(); return ft.tids.Select(tid => new FileTag { FileId = (long)id, TagId = tid })}).ToList();
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
        public async Task<FileEntity> AddFile(FileEntity file, string oldPath, FileExistsStrategy fileExistsStratigy = FileExistsStrategy.Rename, bool move = false, bool dontAddToDB = false)
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
            if (dontAddToDB) return file;
            return await AddFileToDBOnly(file);
        }
        //public async Task<(Boolean success, string message)> Update(FileEntity file)
        //{
        //    if (file.Id < 0) return (false, "Not Valid Id");
        //    if (file.Name == null || file.Name.Trim().Length == 0) return (false, "Not Valid Name");
        //    try
        //    {
        //        ctx.Update(file);
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
        public async Task<(Boolean success, string message, FileEntity file)> ChangeType(long id, FileType type)
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
        public async Task<(Boolean success, string message, FileEntity file)> ChangeDescription(long id, string description)
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


        public async Task<(Boolean success, string message, FileEntity file)> ChangeIsHidden(long id, bool isHidden)
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
        public async Task<(Boolean success, string message, FileEntity file)> Move(long id, DirectoryEntity newDirectory, FileExistsStrategy fileExistsStratigy = FileExistsStrategy.Rename)
        {

            if (id < 0) return (false, "Not Valid Id", null);
            try
            {
                var ctx = await context();
                var file = await ctx.Files.FirstOrDefaultAsync(f => f.Id == id);
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

                    file.Path = newDirectory.Path + (newDirectory.Path[newDirectory.Path.Length - 1] == '\\' ? "" : '\\') + file.Name + "." + file.Extension;
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
        public async Task<(Boolean success, string message, FileEntity file)> Copy(long id, string copyPath, FileExistsStrategy fileExistsStratigy = FileExistsStrategy.Rename)
        {
            if (id < 0) return (false, "Not Valid Id", null);
            if (copyPath == null || copyPath.Trim().Length == 0) return (false, "New path is not valid", null);
            try
            {
                var ctx = await context();
                var file = await ctx.Files.FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return (false, "No file has such an id", null);

                File.Copy(file.FullPath, copyPath);
                repotFinished();
                return (true, null, file);
            }
            catch (Exception e)
            {
                repotFinished();
                return (false, e.Message, null);
            }
        }
        public async Task<(Boolean success, string message, FileEntity file)> DeleteFile(long id)
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
        public async Task<(Boolean success, string message, FileEntity file)> DeleteReferenceOnly(long id)
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

        public async Task<FileEntity> GetFile(long id)
        {
            try
            {
                var ctx = await context();
                var result = await ctx.Files.FirstOrDefaultAsync(f => f.Id == id);
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        public async Task<List<FileEntity>> FilterFiles(FileFilter filter)
        {
            try
            {
                var ctx = await context();
                var que = ctx.Files.AsQueryable().QFileFilter(ctx, filter);
                var str = que.ToQueryString();
                repotFinished();
                return await que.ToListAsync();
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
            return que.Join((context).FileTags.Where(ft => tagIds.Contains(ft.TagId)), f => f.Id, ft => ft.FileId, (f, ft) => f);
        }
        public static IQueryable<FileEntity> QTags(this IQueryable<FileEntity> que, MSDM_DBContext context, string name)
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
        public static IQueryable<FileEntity> QTags(this IQueryable<FileEntity> que, MSDM_DBContext context, List<long> tagIds, string name)
        {
            if (Globals.IsNotNullNorEmpty(tagIds)) return que.QTags(context, tagIds);
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
