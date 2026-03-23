using LogisticsGroup.Domain.Entities;

namespace LogisticsGroup.Domain.Interfaces
{
    public interface IRegionRepository : IRepository<Region>
    {
        void Update(Region entity);
    }
}