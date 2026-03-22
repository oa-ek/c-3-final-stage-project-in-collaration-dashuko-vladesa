namespace LogisticsGroup.Domain.Entities
{
    public class CargoCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; 
        public decimal MinWeight { get; set; }
        public decimal MaxWeight { get; set; }

        public ICollection<Parcel> Parcels { get; set; } = new List<Parcel>();
    }
}