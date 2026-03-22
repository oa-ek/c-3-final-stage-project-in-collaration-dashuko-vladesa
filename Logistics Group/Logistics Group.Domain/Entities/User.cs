namespace LogisticsGroup.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty; 
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;
    }
}