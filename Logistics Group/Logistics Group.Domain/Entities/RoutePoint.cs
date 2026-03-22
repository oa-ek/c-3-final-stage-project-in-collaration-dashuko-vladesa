namespace LogisticsGroup.Domain.Entities
{
    public class RoutePoint
    {
        public int Id { get; set; }
        public int Sequence { get; set; } 
        public string OperationType { get; set; } = string.Empty; 

        public int RouteId { get; set; }
        public Route Route { get; set; } = null!;

        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
    }
}