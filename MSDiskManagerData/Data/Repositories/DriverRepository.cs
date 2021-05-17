using Microsoft.EntityFrameworkCore;
using MSDiskManagerData.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Data.Repositories
{
    public class DriverRepository: BaseRepository
    {


        public async Task<MSDriver> GetDriver(long id)
        {
            try
            {
                var ctx = await context();
                var result = await ctx.MSDrivers.FirstOrDefaultAsync(d => d.Id == id);
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();

                throw;
            }
        }
        public async Task<List<MSDriver>> GetAll()
        {
            try
            {
                var ctx = await context();
                var result = await ctx.MSDrivers.ToListAsync();
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();

                throw;
            }
        }
        public async Task<MSDriver> GetDriver(string uuid)
        {
            try
            {
                var ctx = await context();
                var result = await  ctx.MSDrivers.FirstOrDefaultAsync(d => d.DriverUUID == uuid);
                repotFinished();
                return result;
            }
            catch (Exception)
            {
                repotFinished();

                throw;
            }
        }
        public async Task<MSDriver> AddDriver(MSDriver driver)
        {
            try
            {
                var ctx = await context();
                await ctx.MSDrivers.AddAsync(driver);
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
        public async Task<MSDriver> ChangeLetter(long id, string newLetter)
        {
            try
            {
                var ctx = await context();
                var driver = await GetDriver(id);
                driver.DriverLetter = newLetter;
                ctx.Update(driver);
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
    }
}
