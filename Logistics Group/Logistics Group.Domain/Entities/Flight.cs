using System;
using System.Collections.Generic;

namespace LogisticsGroup.Domain.Entities
{
    public class Flight
    {
        public int Id { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime? ArrivalDate { get; set; } // Може бути порожнім, поки не приїхав
        public string Status { get; set; } = "В дорозі";

        // Зв'язок з Транспортом
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;

        // Зв'язок з Водієм
        public int DriverId { get; set; }
        public Driver Driver { get; set; } = null!;

        // Список посилок у цьому рейсі
        public ICollection<Parcel> Parcels { get; set; } = new List<Parcel>();
    }
}