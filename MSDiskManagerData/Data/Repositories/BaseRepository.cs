using Microsoft.EntityFrameworkCore;
using MSDiskManagerData.Data.Entities;
using Nito.AsyncEx;
using NodaTime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Repositories
{
    public abstract class BaseRepository
    {
        private MSDM_DBContext _context;
        protected PauseToken token;

        protected async Task<MSDM_DBContext> context()
        {
            await token.WaitWhilePausedAsync();
            var limit = 90;
            //#if DEBUG
            //            limit = 10;
            //#endif
            if (Interlocked.Increment(ref MSDM_DBContext.ActiveConnections) > limit)
            {
                MSDM_DBContext.PauseSource.IsPaused = true;
            }
            if (_context == null)
            {
                _context = new MSDM_DBContext(IsTest);
                _context.Database.EnsureCreated();
            }
            return _context;
        }
        protected bool IsTest { get; set; }
        public BaseRepository(bool isTest = false)
        {
            token = MSDM_DBContext.PauseSource.Token;
            IsTest = isTest;
        }
        /// <summary>
        /// addes the given path <Must be a directory> to the db if not exists and returns it
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public async Task<MSDirecotry> AddPathToDB(string fullPath)
        {
            if (!Directory.Exists(fullPath)) throw new FileNotFoundException("No Directory Exists at given path");
            var p = fullPath.Replace(";", "\\").Replace("/", "\\").Trim();
            var split = p.Split("\\");
            var currentPath = "";
            var driverId = MSDM_DBContext.currentDriveId;
            var currentPathNoDrive = "";
            var fp = fullPath.Replace(split[0] + "\\", "");
            if (fp.Last() == '\\') fp = fp.Substring(0, fp.Length - 1);
            var que = (await context()).Directories.Where(d => d.Path == fp);
            var result = await que.FirstOrDefaultAsync();
            long? parentId = null;
            if (result != null) return result;
            var parentExists = false;
            for (int i = 0; i < split.Length; i++)
            {
                var s = split[i];
                currentPath += s + "\\";
                if (i > 0) currentPathNoDrive += s;
                else continue;
                if (i == 1 || parentExists)
                {
                    result = await (await context()).Directories.Where(d => d.ParentId == parentId && d.Path == currentPathNoDrive).FirstOrDefaultAsync();
                    if (result != null)
                    {
                        parentExists = true;
                        parentId = result.Id;
                        continue;
                    }
                }
                parentExists = false;
                var ancestorIds = new List<long>();
                if (result != null) ancestorIds.AddRange(result.AncestorIds);
                if (parentId != null) ancestorIds.Add(parentId.Value);
                //var tags = new List<Tag>();
                var id = await (await context()).Directories.AddAsync(new MSDirecotry
                {
                    ParentId = parentId,
                    AddingDate = Instant.FromDateTimeUtc(DateTime.Now.ToUniversalTime()),
                    MovingDate = Instant.FromDateTimeUtc(DateTime.Now.ToUniversalTime()),
                    AncestorIds = ancestorIds,
                    DriveId = MSDM_DBContext.currentDriveId,
                    Name = split[i],
                    OnDeskName = split[i],
                    Path = currentPathNoDrive
                });
                await (await context()).SaveChangesAsync();
                if (result != null && result.DirectoryTags.Count > 0)
                {
                    await (await context()).DirectoryTags.AddRangeAsync(result.DirectoryTags.Select(dt => new Entities.Relations.DirectoryTag
                    {
                        DirectoryId = id.Entity.Id.Value,
                        TagId = dt.TagId
                    }));
                    await (await context()).SaveChangesAsync();
                }
                result = id.Entity;
                parentId = result.Id;
                currentPathNoDrive += "\\";
            }
            return result;
        }
        public async Task<ICollection<BaseEntity>> AddToDBAsIs(ICollection<BaseEntity> items)
        {
            var files = items.Where(i => i is MSFile).Cast<MSFile>();
            var dirs = items.Where(i => i is MSDirecotry).Cast<MSDirecotry>();
            var ctx = await context();
            await ctx.Files.AddRangeAsync(files);
            await ctx.Directories.AddRangeAsync(dirs);
            await ctx.SaveChangesAsync();
            return items;
        }
        public async Task<(ICollection<MSFile> files, ICollection<MSDirecotry> dirs)> AddToDBAsIs(ICollection<MSFile> files, ICollection<MSDirecotry> dirs, bool fixHirarcies = false)
        {
            foreach (var item in files.Cast<BaseEntity>().Union(dirs))
            {
                item.DriveId = MSDM_DBContext.currentDriveId;
            }
                var ctx = await context();
            try
            {
                
                if (files.Count > 0) await ctx.Files.AddRangeAsync(files);
                if (dirs.Count > 0) await ctx.Directories.AddRangeAsync(dirs);
                await ctx.SaveChangesAsync();

            }
            catch (Exception)
            {

                throw;
            }
            if (fixHirarcies)
            {
                try
                {
                    var pathes = new HashSet<string>();
                    var itemIdPathes = new Dictionary<long, HashSet<string>>();
                    var pathId = new Dictionary<string, long>();
                    foreach (var d in dirs)
                    {
                        pathes.Add(d.Path);
                        pathId.Add(d.Path, d.Id.Value);
                    }
                    foreach (var item in files.Cast<BaseEntity>().Union(dirs))
                    {
                        itemIdPathes.Add(item.Id.Value, new HashSet<string>());
                        var p = item.Path;
                        if (p.First() == '\\') p = p.Substring(1);
                        if (p.Last() == '\\') p = p.Substring(0, p.Length - 1);
                        if (!p.Contains("\\"))
                        {
                            continue;
                        }
                        p = p.Substring(0, p.LastIndexOf("\\"));
                        var path = "";
                        var split = p.Split("\\");
                        for (int i = 0; i < split.Length; i++)
                        {
                            if (split[i].Length == 0) continue;
                            path += (i == 0 ? "" : "\\") +  split[i];
                            if (!pathId.ContainsKey(path))
                            {
                                var id = await ctx.Directories.Where(d => d.Path == path).Select(d => d.Id.Value).FirstAsync();
                                pathes.Add(path);
                                pathId.Add(path, id);
                            }
                            var aid = pathId[path];
                            if (!item.AncestorIds.Contains(aid)) item.AncestorIds.Add(aid);
                        }
                        item.ParentId = pathId[path];
                    }
                    if (files.Count > 0) ctx.Files.UpdateRange(files);
                    if (dirs.Count > 0) ctx.Directories.UpdateRange(dirs);
                    await ctx.SaveChangesAsync();
                }
                catch (Exception)
                {

                    throw;
                }
            }
            return (files, dirs);
        }
        protected void repotFinished()
        {
            var limit = 80;
            //#if DEBUG
            //            limit = 8;
            //#endif
            if (Interlocked.Decrement(ref MSDM_DBContext.ActiveConnections) < limit)
            {
                if (token.IsPaused)
                {
                    MSDM_DBContext.PauseSource.IsPaused = false;
                }
            }
            if (Interlocked.Decrement(ref MSDM_DBContext.ActiveConnections) < 0)
            {
                Interlocked.Increment(ref MSDM_DBContext.ActiveConnections);
            }
        }
    }
}
