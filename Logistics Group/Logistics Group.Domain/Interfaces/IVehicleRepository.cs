using LogisticsGroup.Domain.Entities;

namespace LogisticsGroup.Domain.Interfaces
{
    public interface IVehicleRepository : IRepository<Vehicle>
    {
        void Update(Vehicle entity);
    }
}