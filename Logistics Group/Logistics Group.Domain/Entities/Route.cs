namespace LogisticsGroup.Domain.Entities
{
    public class Route
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; 
        public string Type { get; set; } = string.Empty; 
        public decimal Distance { get; set; } 
        public int EstimatedTime { get; set; } 

        public ICollection<RoutePoint> RoutePoints { get; set; } = new List<RoutePoint>();
    }
}