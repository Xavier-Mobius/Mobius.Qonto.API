using System.Text.Json.Serialization;

namespace Mobius.Qonto.API.Model;

internal class OrganizationQuery
{
    [JsonPropertyName("organization")]
    public Organization? Organization { get; set; }
}
