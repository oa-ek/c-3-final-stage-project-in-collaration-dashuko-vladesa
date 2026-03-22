namespace LogisticsGroup.Domain.Entities
{
    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; 

        public int RegionId { get; set; }
        public Region Region { get; set; } = null!;

        public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    }
}