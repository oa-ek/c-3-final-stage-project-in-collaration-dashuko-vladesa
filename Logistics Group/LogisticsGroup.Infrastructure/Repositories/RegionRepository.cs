using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using LogisticsGroup.Infrastructure.Data;
using System.Linq;

namespace LogisticsGroup.Infrastructure.Repositories
{
    public class RegionRepository : Repository<Region>, IRegionRepository
    {
        public RegionRepository(ApplicationDbContext ctx) : base(ctx) { }

        public void Update(Region entity)
        {
            var objFromDb = _ctx.Regions.FirstOrDefault(x => x.Id == entity.Id);

            if (objFromDb is not null)
            {
                objFromDb.Name = entity.Name;
            }
        }
    }
}