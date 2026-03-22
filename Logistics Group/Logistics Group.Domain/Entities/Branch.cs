namespace LogisticsGroup.Domain.Entities
{
    public class Branch
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; 
        public string? WorkingHours { get; set; }
        public decimal? MaxWeight { get; set; }

        public int CityId { get; set; }
        public City City { get; set; } = null!;
    }
}