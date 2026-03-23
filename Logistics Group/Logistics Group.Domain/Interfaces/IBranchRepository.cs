using LogisticsGroup.Domain.Entities;

namespace LogisticsGroup.Domain.Interfaces
{
    public interface IBranchRepository : IRepository<Branch>
    {
        void Update(Branch entity);
    }
}