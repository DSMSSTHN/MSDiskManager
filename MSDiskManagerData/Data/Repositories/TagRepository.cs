using Microsoft.EntityFrameworkCore;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Repositories
{
    public class TagRepository : BaseRepository
    {
        public TagRepository(bool isTest = false)
        {
            IsTest = isTest;
        }
        public async Task<bool> TagExists(string name)
        {
            try
            {
                var ctx = await context();
                var result = await ctx.Tags.AnyAsync(t => t.Name.Trim().ToLower() == name.Trim().ToLower());
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        public async Task<Tag> GetTag(long id)
        {
            if (id < 0) return null;
            try
            {
                var ctx = await context();
                var result = await ctx.Tags.FirstOrDefaultAsync(t => t.Id == id);
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }

        public async Task<List<Tag>> GetTags(string name = null, List<long> excludeIds = null, int page = 0, int limit = 0)
        {
            try
            {
                var ctx = await context();
                var que = ctx.Tags.AsQueryable();
                if (Globals.IsNotNullNorEmpty(name))
                {
                    var ns = name.Trim().ToLower().Split(" ").Where(n => n.Trim().IsNotEmpty());
                    foreach (var n in ns)
                    {
                        que = que.Where(t => t.Name.ToLower().Contains(n));
                    }

                }
                if (Globals.IsNotNullNorEmpty(excludeIds))
                {
                    que = que.Where(t => !excludeIds.Contains((long)t.Id));
                }
                if (limit > 0)
                {
                    var p = page < 0 ? 0 : page;
                    que = que.Skip(page * limit).Take(limit);
                }
                var result = await que.ToListAsync();
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        
        public async Task<Tag> AddTag(string name, int color)
        {
            return await AddTag(new Tag { Name = name, Color = color });
        }
        public async Task<Tag> AddTag(Tag tag)
        {
            if (tag == null) throw new ArgumentException("Null was given instead of a tag");
            if (Globals.IsNullOrEmpty(tag.Name)) throw new Exception($"Tag has no name");
            try
            {
                var ctx = await context();
                var exitst = await ctx.Tags.AnyAsync(t => t.Name == tag.Name);
                if (exitst)
                {
                    throw new TagAlreadyExists(tag.Name);
                }
                if (tag.Id != null)
                {
                    tag.Id = null;
                }
                await ctx.Tags.AddAsync(tag);
                await ctx.SaveChangesAsync();
                repotFinished();
                return tag;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }

        public async Task<Tag> UpdateTag(Tag tag)
        {
            if (tag == null) throw new ArgumentException("Null was given instead of a tag");
            if (tag.Id == null || tag.Id < 0) throw new ArgumentException("tag with no id was given for update");
            if (Globals.IsNullOrEmpty(tag.Name)) throw new Exception($"Tag has no name");
            try
            {
                var ctx = await context();
                var exitst = await ctx.Tags.AnyAsync(t => t.Name == tag.Name);
                if (exitst)
                {
                    throw new TagAlreadyExists(tag.Name);
                }
                ctx.Tags.Update(tag);
                await ctx.SaveChangesAsync();
                repotFinished();
                return tag;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        public async Task<bool> delete(long? id)
        {
            if (id == null) return false;
            try
            {
                var ctx = await context();
                var t = await ctx.Tags.FirstOrDefaultAsync(t => t.Id == id);
                if (t == null)
                {
                    return false;
                }
                ctx.Tags.Remove(t);
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
        public async Task<List<MSFile>> GetFiles(long id)
        {
            if (id < 0) return new List<MSFile>();
            try
            {
                var ctx = await context();
                var files = await ctx.FileTags.Include(f => f.File).Where(f => f.TagId == id).Select(f => f.File).ToListAsync();
                repotFinished();
                return files;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
        public async Task<List<MSDirecotry>> GetDirectories(long id)
        {
            if (id < 0) return new List<MSDirecotry>();
            try
            {
                var ctx = await context();
                var directories = await ctx.DirectoryTags.Include(f => f.Directory).Where(f => f.TagId == id).Select(f => f.Directory).ToListAsync();
                repotFinished();
                return directories;
            }
            catch (Exception)
            {
                repotFinished();
                throw;
            }
        }
    }
    public class TagAlreadyExists : Exception
    {
        public TagAlreadyExists(string name) : base($"tag [{name}] already exists")
        {

        }
    }
}
