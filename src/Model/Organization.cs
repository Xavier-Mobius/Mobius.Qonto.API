using System.Text.Json.Serialization;

namespace Mobius.Qonto.API.Model;
public class Organization
{
    [JsonPropertyName("legal_name")]
    public string? LegalName { get; set; }

    [JsonPropertyName("bank_accounts")]
    public BankAccount[]? BankAccounts { get; set; }
}
