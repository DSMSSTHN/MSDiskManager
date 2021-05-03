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
    public class TagRepository
    {
        private MSDM_DBContext context;
        private bool IsTest { get; set; }
        public TagRepository(bool isTest = false)
        {
            IsTest = isTest;
            this.context = new MSDM_DBContext(IsTest);
            this.context.Database.EnsureCreated();
        }
        public async Task<bool> TagExists(string name)
        {
            return await context.Tags.AnyAsync(t => t.Name.Trim().ToLower() == name.Trim().ToLower());
        }
        public async Task<Tag> GetTag(long id)
        {
            if (id < 0) return null;
            return await context.Tags.FirstOrDefaultAsync(t => t.Id == id);
        }
        
        public async Task<List<Tag>> GetTags(string name = null,List<long> excludeIds = null, int page = 0, int limit = 0)
        {
            var que = context.Tags.AsQueryable();
            if (Globals.IsNotNullNorEmpty(name))
            {
                var ns = name.Trim().ToLower().Split(" ").Where(n => n.Trim().IsNotEmpty());
                que = que.Where(t => ns.All(n => t.Name.ToLower().Contains(n)));
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
            return await que.ToListAsync();
        }
        public async Task<Tag> AddTag(string name, int color)
        {
            return await AddTag(new Tag { Name = name, Color = color });
        }
        public async Task<Tag> AddTag(Tag tag)
        {
            if (tag == null) throw new ArgumentException("Null was given instead of a tag");
            if (Globals.IsNullOrEmpty(tag.Name)) throw new Exception($"Tag has no name"); ;
            var exitst = await context.Tags.AnyAsync(t => t.Name == tag.Name);
            if (exitst)
            {
                throw new TagAlreadyExists(tag.Name);
            }
            if (tag.Id != null)
            {
                tag.Id = null;
            }
            await context.Tags.AddAsync(tag);
            await context.SaveChangesAsync();
            return tag;
        }
        public async Task<Tag> UpdateTag(Tag tag)
        {
            if (tag == null) throw new ArgumentException("Null was given instead of a tag");
            if (tag.Id == null || tag.Id < 0) throw new ArgumentException("tag with no id was given for update"); ;
            if (Globals.IsNullOrEmpty(tag.Name)) throw new Exception($"Tag has no name"); ;
            var exitst = await context.Tags.AnyAsync(t => t.Name == tag.Name);
            if (exitst)
            {
                throw new TagAlreadyExists(tag.Name);
            }
            context.Tags.Update(tag);
            await context.SaveChangesAsync();
            return tag;
        }
        public async Task<bool> delete(long? id)
        {
            if (id == null) return false;
            try
            {
                var t = await context.Tags.FirstOrDefaultAsync(t => t.Id == id);
                if (t == null)
                {
                    return false;
                }
                context.Tags.Remove(t);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        public async Task<List<FileEntity>> GetFiles(long id)
        {
            if (id < 0) return new List<FileEntity>();
            var files = await this.context.FileTags.Include(f => f.File).Where(f => f.TagId == id).Select(f => f.File).ToListAsync();
            return files;
        }
        public async Task<List<DirectoryEntity>> GetDirectories(long id)
        {
            if (id < 0) return new List<DirectoryEntity>();
            var directories = await this.context.DirectoryTags.Include(f => f.Directory).Where(f => f.TagId == id).Select(f => f.Directory).ToListAsync();
            return directories;
        }
    }
    public class TagAlreadyExists : Exception
    {
        public TagAlreadyExists(string name) : base($"tag [{name}] already exists")
        {

        }
    }
}
