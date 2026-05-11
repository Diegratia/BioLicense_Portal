using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using BioLicense_Portal.Domain.Enums;

namespace BioLicense_Portal.Domain.Entities
{
    public class Application : BaseEntity
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        [Column("application_type")]
        public ApplicationType ApplicationType { get; set; }
        [Column("slug")]
        public string Slug { get; set; } = string.Empty;
        [Column("description")]
        public string? Description { get; set; }
        [Column("private_key_encrypted")]
        public string? PrivateKeyEncrypted { get; set; }
        [Column("public_key")]
        public string? PublicKey { get; set; }
        [Column("key_passphrase")]
        public string? KeyPassphrase { get; set; }
        [Column("status")]
        public int Status { get; set; } = 1;
        public ICollection<ApplicationFeature> Features { get; set; } = new List<ApplicationFeature>();
    }

    public class ApplicationFeature : BaseEntity
    {
        [Column("application_id")]
        public Guid ApplicationId { get; set; }
        [Column("feature_key")]
        public string FeatureKey { get; set; } = string.Empty;
        [Column("display_name")]
        public string DisplayName { get; set; } = string.Empty;
        [Column("description")]
        public string? Description { get; set; }
        [Column("category")]
        public string Category { get; set; } = "core"; // core / module
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        public Application? Application { get; set; }
    }
}
