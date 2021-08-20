using Microsoft.EntityFrameworkCore;
using MSDiskManagerData.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Repositories
{
    public class DriveRepository: BaseRepository
    {


        public async Task<MSDrive> GetDriver(string id)
        {
            try
            {
                var ctx = await context();
                var result = await ctx.MSDrives.FirstOrDefaultAsync(d => d.Id == id);
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();

                throw;
            }
        }
        public async Task<List<MSDrive>> GetAll()
        {
            try
            {
                var ctx = await context();
                var result = await ctx.MSDrives.ToListAsync();
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();

                throw;
            }
        }
        public async Task<MSDrive> AddDriver(MSDrive driver)
        {
            try
            {
                var ctx = await context();
                await ctx.MSDrives.AddAsync(driver);
                await ctx.SaveChangesAsync();
                repotFinished();
                return driver;
            }
            catch (Exception)
            {
                repotFinished();

                throw;
            }
        }
        public async Task<List<MSDrive>> LoadDrives(List<string> ids = null)
        {
            if (ids == null || ids.Count == 0) return await GetAll();
            try
            {
                var ctx = await context();
                var result = await ctx.MSDrives.Where(d => ids.Contains(d.Id)).ToListAsync();
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();

                throw;
            }
        }
        //public async Task<List<(string id,bool exists)>> FilterIds(List<string> ids)
        //{
        //    try
        //    {
        //        var ctx = await context();
        //        var present = await ctx.MSDrives.Where(d => ids.Contains(d.Id)).Select(d => d.Id).ToListAsync();
        //        List<(string id, bool exists)> result = ids.Select(id => 
        //        { return new { id = id, exists = present.Contains(id) }; });

        //        repotFinished();
        //        return result;
        //    }
        //    catch (Exception)
        //    {
        //        repotFinished();

        //        throw;
        //    }
        //}
    }
}
