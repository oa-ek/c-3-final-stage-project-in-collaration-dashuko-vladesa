using LogisticsGroup.Domain.Entities;

namespace LogisticsGroup.Domain.Interfaces
{
    public interface ICityRepository : IRepository<City>
    {
        void Update(City entity);
    }
}