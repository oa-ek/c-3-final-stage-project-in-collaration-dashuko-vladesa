using LogisticsGroup.Domain.Interfaces;
using LogisticsGroup.Infrastructure.Data;

namespace LogisticsGroup.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _ctx;
        public ICargoCategoryRepository CargoCategory { get; private set; }

        public UnitOfWork(ApplicationDbContext ctx)
        {
            _ctx = ctx;
            CargoCategory = new CargoCategoryRepository(_ctx);
        }

        public void Save()
        {
            _ctx.SaveChanges();
        }
    }
}