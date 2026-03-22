using System;

namespace LogisticsGroup.Domain.Entities
{
    public class TransportOrder
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty; 
        public DateTime DepartureDate { get; set; }
        public string Status { get; set; } = string.Empty; 

        public int RouteId { get; set; }
        public Route Route { get; set; } = null!;

        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;

        public int DriverId { get; set; }
        public Driver Driver { get; set; } = null!;

        public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}