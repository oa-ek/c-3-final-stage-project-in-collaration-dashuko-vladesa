using LogisticsGroup.Domain.Interfaces;
using LogisticsGroup.Infrastructure.Data;

namespace LogisticsGroup.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _ctx;

        public ICargoCategoryRepository CargoCategory { get; private set; }
        public IRegionRepository Region { get; private set; }
        public ICityRepository City { get; private set; }
        public IBranchRepository Branch { get; private set; }

        public IVehicleRepository Vehicle { get; private set; }
        public IDriverRepository Driver { get; private set; }

        public UnitOfWork(ApplicationDbContext ctx)
        {
            _ctx = ctx;
            CargoCategory = new CargoCategoryRepository(_ctx);
            Region = new RegionRepository(_ctx);
            City = new CityRepository(_ctx);
            Branch = new BranchRepository(_ctx);

            Vehicle = new VehicleRepository(_ctx);
            Driver = new DriverRepository(_ctx);
        }

        public void Save()
        {
            _ctx.SaveChanges();
        }
    }
}