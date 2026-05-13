using System.Text.Json.Serialization;

namespace BioLicense_Portal.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserRole
    {
        Owner,
        TechnicalEngineer,
        Distributor
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LicenseType
    {
        Trial,
        Annual,
        Perpetual
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LicenseTier
    {
        Core,
        Professional,
        Enterprise,
        Custom
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LicenseStatus
    {
        Active,
        Revoked,
        Expired
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApplicationType
    {
        PeopleTracking,
        VMS,
        Parking,
        SMR,
        Signage
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LicenseRequestStatus
    {
        Pending,
        Approved,
        Rejected,
        Completed
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FeatureCategory
    {
        Core,
        Module
    }
}
