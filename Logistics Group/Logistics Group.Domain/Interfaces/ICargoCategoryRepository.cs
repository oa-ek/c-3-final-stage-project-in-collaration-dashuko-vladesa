using LogisticsGroup.Domain.Entities;

namespace LogisticsGroup.Domain.Interfaces
{
    public interface ICargoCategoryRepository : IRepository<CargoCategory>
    {
        void Update(CargoCategory entity);
    }
}