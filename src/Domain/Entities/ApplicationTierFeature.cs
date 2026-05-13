using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLicense_Portal.Domain.Entities
{
    [Table("application_tier_features")]
    public class ApplicationTierFeature
    {
        [Column("tier_id")]
        public Guid TierId { get; set; }
        
        [Column("feature_id")]
        public Guid FeatureId { get; set; }

        public ApplicationTier? Tier { get; set; }
        public ApplicationFeature? Feature { get; set; }
    }
}
