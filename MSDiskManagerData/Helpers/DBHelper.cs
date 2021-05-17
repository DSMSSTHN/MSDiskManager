using Microsoft.EntityFrameworkCore;
using MSDiskManagerData.Data;
using MSDiskManagerData.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManagerData.Helpers
{
    public static class DBHelper
    {
        public async static Task LoadParentWithAncestorIds(this BaseEntity entity, MSDM_DBContext context)
        {
            if (entity.ParentId == null)
            {
                entity.AncestorIds = new List<long>();
                return;
            }
            if (entity.Parent == null)
            {
                entity.Parent = await context.Directories.FirstOrDefaultAsync(d => d.Id == entity.ParentId);
            }
            entity.AncestorIds = entity.Parent.AncestorIds.ToList();
            entity.AncestorIds.Add((long)entity.ParentId);
        }
    }
}
