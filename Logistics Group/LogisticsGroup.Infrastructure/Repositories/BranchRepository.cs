using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using LogisticsGroup.Infrastructure.Data;
using System.Linq;

namespace LogisticsGroup.Infrastructure.Repositories
{
    public class BranchRepository : Repository<Branch>, IBranchRepository
    {
        public BranchRepository(ApplicationDbContext ctx) : base(ctx) { }

        public void Update(Branch entity)
        {
            var objFromDb = _ctx.Branches.FirstOrDefault(x => x.Id == entity.Id);

            if (objFromDb is not null)
            {
                // Оновлюємо реальні поля твоєї моделі
                objFromDb.Number = entity.Number;
                objFromDb.Address = entity.Address;
                objFromDb.Type = entity.Type;
                objFromDb.WorkingHours = entity.WorkingHours;
                objFromDb.MaxWeight = entity.MaxWeight;
                objFromDb.CityId = entity.CityId;
            }
        }
    }
}