using BioLicense_Portal.Domain.Enums;

namespace BioLicense_Portal.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public int Status { get; set; } = 1; // 1=active, 0=inactive
        public DateTime? LastLoginAt { get; set; }
    }
}
