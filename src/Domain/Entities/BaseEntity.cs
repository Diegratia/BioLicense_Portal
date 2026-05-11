using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLicense_Portal.Domain.Entities
{
    public abstract class BaseEntity
    {
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
        [Column("created_by")]
        public string CreatedBy { get; set; } = string.Empty;
        [Column("updated_by")]
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
