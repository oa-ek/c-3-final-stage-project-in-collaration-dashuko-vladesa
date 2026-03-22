namespace LogisticsGroup.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        ICargoCategoryRepository CargoCategory { get; }
        void Save();
    }
}