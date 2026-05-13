using System.ComponentModel.DataAnnotations.Schema;
using BioLicense_Portal.Domain.Enums;

namespace BioLicense_Portal.Domain.Entities
{
    public class User : BaseEntity
    {
        [Column("username")]
        public string Username { get; set; } = string.Empty;
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;
        [Column("email")]
        public string Email { get; set; } = string.Empty;
        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;
        [Column("role")]
        public UserRole Role { get; set; }
        [Column("status")]
        public int Status { get; set; } = 1; // 1=active, 0=inactive
        [Column("last_login_at")]
        public DateTime? LastLoginAt { get; set; }
    }
}
