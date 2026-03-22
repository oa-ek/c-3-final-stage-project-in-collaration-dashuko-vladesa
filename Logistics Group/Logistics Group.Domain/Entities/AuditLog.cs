using System;

namespace LogisticsGroup.Domain.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty; 
        public string Entity { get; set; } = string.Empty; 
        public DateTime Timestamp { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}