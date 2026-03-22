using System;

namespace LogisticsGroup.Domain.Entities
{
    public class ParcelStatusHistory
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty; 
        public DateTime Timestamp { get; set; } 

        public int ParcelId { get; set; }
        public Parcel Parcel { get; set; } = null!;

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }
    }
}