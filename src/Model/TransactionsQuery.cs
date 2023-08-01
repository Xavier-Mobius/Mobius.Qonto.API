using System.Text.Json.Serialization;

namespace Mobius.Qonto.API.Model;

internal class TransactionsQuery
{
    [JsonPropertyName("transactions")]
    public Transaction[]? Transactions { get; set; }

    [JsonPropertyName("meta")]
    public Meta? Meta { get; set; }
}
