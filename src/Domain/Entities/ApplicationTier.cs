using System;
using System.ComponentModel.DataAnnotations.Schema;
using BioLicense_Portal.Domain.Enums;

namespace BioLicense_Portal.Domain.Entities
{
    public class ApplicationTier : BaseEntity
    {
        [Column("application_id")]
        public Guid ApplicationId { get; set; }
        
        [Column("tier")]
        public LicenseTier Tier { get; set; } // Core, Professional, Enterprise
        
        [Column("description")]
        public string? Description { get; set; }
        
        [Column("parameters")]
        public string? Parameters { get; set; } // JSON string for additional parameters
        
        public Application? Application { get; set; }
        public ICollection<ApplicationTierFeature> TierFeatures { get; set; } = new List<ApplicationTierFeature>();
    }
}
