using LogisticsGroup.Domain.Entities;

namespace LogisticsGroup.Domain.Interfaces
{
    public interface IDriverRepository : IRepository<Driver>
    {
        void Update(Driver entity);
    }
}