namespace LogisticsGroup.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        ICargoCategoryRepository CargoCategory { get; }
        IRegionRepository Region { get; }
        ICityRepository City { get; }
        IBranchRepository Branch { get; }

        IVehicleRepository Vehicle { get; }
        IDriverRepository Driver { get; }

        void Save();
    }
}