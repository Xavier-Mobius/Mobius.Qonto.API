using System.Text.Json.Serialization;

namespace Mobius.Qonto.API.Model;
internal class Meta
{
    [JsonPropertyName("next_page")]
    public int? NextPage { get; set; }
}
