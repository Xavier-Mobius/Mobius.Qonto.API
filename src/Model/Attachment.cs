using System.Text.Json.Serialization;

namespace Mobius.Qonto.API.Model;

public class Attachment
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("file_name")]
    public string? Filename { get; set; }

    [JsonPropertyName("file_size")]
    public string? FileSize { get; set; }

    [JsonPropertyName("file_content_type")]
    public string? FileContentType { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
