namespace LogisticsGroup.Domain.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal Capacity { get; set; } 
        public string Status { get; set; } = string.Empty; 
    }
}