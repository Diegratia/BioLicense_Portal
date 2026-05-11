using System.ComponentModel.DataAnnotations.Schema;

namespace BioLicense_Portal.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        [Column("user_id")]
        public Guid UserId { get; set; }
        [Column("token")]
        public string Token { get; set; } = string.Empty;
        [Column("expiry_date")]
        public DateTime ExpiryDate { get; set; }
        public User? User { get; set; }
    }

    public class AuditLog : BaseEntity
    {
        [Column("event_name")]
        public string EventName { get; set; } = string.Empty;
        [Column("entity_name")]
        public string? EntityName { get; set; }
        [Column("entity_id")]
        public Guid? EntityId { get; set; }
        [Column("actor_user_id")]
        public Guid ActorUserId { get; set; }
        [Column("actor_username")]
        public string? ActorUsername { get; set; }
        [Column("details")]
        public string? Details { get; set; } // JSON
        [Column("ip_address")]
        public string? IpAddress { get; set; }
        [Column("event_time")]
        public DateTime EventTime { get; set; } = DateTime.UtcNow;
    }
}
