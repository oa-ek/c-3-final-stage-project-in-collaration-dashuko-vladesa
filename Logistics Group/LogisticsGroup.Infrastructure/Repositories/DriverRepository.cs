using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using LogisticsGroup.Infrastructure.Data;
using System.Linq;

namespace LogisticsGroup.Infrastructure.Repositories
{
    public class DriverRepository : Repository<Driver>, IDriverRepository
    {
        public DriverRepository(ApplicationDbContext ctx) : base(ctx) { }

        public void Update(Driver entity)
        {
            var objFromDb = _ctx.Drivers.FirstOrDefault(x => x.Id == entity.Id);

            if (objFromDb is not null)
            {
                // Оновлюємо саме твої поля
                objFromDb.FullName = entity.FullName;
                objFromDb.Phone = entity.Phone;
                objFromDb.LicenseNumber = entity.LicenseNumber;
                objFromDb.Status = entity.Status;
            }
        }
    }
}