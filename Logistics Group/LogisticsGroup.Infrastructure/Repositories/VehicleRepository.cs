using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using LogisticsGroup.Infrastructure.Data;
using System.Linq;

namespace LogisticsGroup.Infrastructure.Repositories
{
    public class VehicleRepository : Repository<Vehicle>, IVehicleRepository
    {
        public VehicleRepository(ApplicationDbContext ctx) : base(ctx) { }

        public void Update(Vehicle entity)
        {
            var objFromDb = _ctx.Vehicles.FirstOrDefault(x => x.Id == entity.Id);

            if (objFromDb is not null)
            {
                // Оновлюємо саме твої поля
                objFromDb.LicensePlate = entity.LicensePlate;
                objFromDb.Brand = entity.Brand;
                objFromDb.Capacity = entity.Capacity;
                objFromDb.Status = entity.Status;
            }
        }
    }
}