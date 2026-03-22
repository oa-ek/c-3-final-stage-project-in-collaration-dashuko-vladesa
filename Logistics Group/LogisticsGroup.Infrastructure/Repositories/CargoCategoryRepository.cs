using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using LogisticsGroup.Infrastructure.Data;
using System.Linq;

namespace LogisticsGroup.Infrastructure.Repositories
{
    public class CargoCategoryRepository : Repository<CargoCategory>, ICargoCategoryRepository
    {
        public CargoCategoryRepository(ApplicationDbContext ctx) : base(ctx)
        {
        }

        public void Update(CargoCategory entity)
        {
            var categoryFromDb = _ctx.CargoCategories.FirstOrDefault(x => x.Id == entity.Id);

            if (categoryFromDb is not null)
            {
                categoryFromDb.Name = entity.Name;
            }
        }
    }
}