using Nito.AsyncEx;
using System;
using System.Collections.Generic;
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
            if (Interlocked.Increment(ref MSDM_DBContext.ActiveConnections) > 90)
            {
                MSDM_DBContext.PauseSource.IsPaused = true;
            }
            if(_context == null)
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

        protected void repotFinished()
        {
            if(Interlocked.Decrement(ref MSDM_DBContext.ActiveConnections) < 80)
            {
                if (token.IsPaused)
                {
                    MSDM_DBContext.PauseSource.IsPaused = false;
                }
            }
            if(Interlocked.Decrement(ref MSDM_DBContext.ActiveConnections) < 0)
            {
                Interlocked.Increment(ref MSDM_DBContext.ActiveConnections);
            }
        }
    }
}
