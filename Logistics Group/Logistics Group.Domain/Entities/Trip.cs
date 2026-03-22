using System;

namespace LogisticsGroup.Domain.Entities
{
    public class Trip
    {
        public int Id { get; set; }
        public DateTime LoadingTime { get; set; }
        public DateTime UnloadingTime { get; set; }

        public int TransportOrderId { get; set; }
        public TransportOrder TransportOrder { get; set; } = null!;

        public int DepartureBranchId { get; set; }
        public Branch DepartureBranch { get; set; } = null!;

        public int ArrivalBranchId { get; set; }
        public Branch ArrivalBranch { get; set; } = null!;
    }
}