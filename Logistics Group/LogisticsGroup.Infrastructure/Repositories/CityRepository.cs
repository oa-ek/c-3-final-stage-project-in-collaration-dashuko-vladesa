using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using LogisticsGroup.Infrastructure.Data;
using System.Linq;

namespace LogisticsGroup.Infrastructure.Repositories
{
    public class CityRepository : Repository<City>, ICityRepository
    {
        public CityRepository(ApplicationDbContext ctx) : base(ctx) { }

        public void Update(City entity)
        {
            var objFromDb = _ctx.Cities.FirstOrDefault(x => x.Id == entity.Id);

            if (objFromDb is not null)
            {
                // Оновлюємо реальні поля твоєї моделі
                objFromDb.Name = entity.Name;
                objFromDb.Type = entity.Type;
                objFromDb.RegionId = entity.RegionId;
            }
        }
    }
}