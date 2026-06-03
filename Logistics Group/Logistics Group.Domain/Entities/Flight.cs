using System;
using System.Collections.Generic;

namespace LogisticsGroup.Domain.Entities
{
    public class Flight
    {
        public int Id { get; set; }

        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

        public int DriverId { get; set; }
        public Driver Driver { get; set; }

        public int? RouteId { get; set; }
        public Route Route { get; set; }

        public DateTime DepartureDate { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public string Status { get; set; }

        public string? IssueMessage { get; set; }

        public ICollection<Parcel> Parcels { get; set; }
    }
}