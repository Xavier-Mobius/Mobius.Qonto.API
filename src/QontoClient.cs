using Mobius.Qonto.API.Model;
using System.Text.Json;

namespace Mobius.Qonto.API;

public class QontoClient : IDisposable
{
    private const string K_QONTO_API_BASE_ADDRESS = "https://thirdparty.qonto.com/";

    private HttpClient? _HttpClient;
    private bool disposedValue;

    public HttpClient? HttpClient
    {
        get => _HttpClient ?? throw new InvalidOperationException($"Please call {nameof(InitializeAuthorization)} before any other method.");
        set => _HttpClient = value;
    }

    public void InitializeAuthorization(string login, string secretKey)
    {
        _HttpClient = new HttpClient() { BaseAddress = new Uri(K_QONTO_API_BASE_ADDRESS) };
        _ = _HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"{login}:{secretKey}");
    }
    public async Task<List<Transaction>> GetBankStatementAsync(string iban, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        if (to == to.Date)
            to = to.AddDays(1).AddSeconds(-1);

        var allTransactions = new List<Transaction>();
        var currentPage = 1;
        bool hasNextPage;
        do
        {
#pragma warning disable CS8602 // Incorrect nullable flow analysis for indirect null check.
            var stream = await HttpClient.GetStreamAsync($"/v2/transactions?sort_by=settled_at:asc&iban={iban}" +
                $"&current_page={currentPage}" +
                $"&settled_at_from={from:o}" +
                $"&settled_at_to={to:o}", cancellationToken); // 2021-03-03T16:06:38.000Z
#pragma warning restore CS8602 // Incorrect nullable flow analysis for indirect null check.

            var transactions = await JsonSerializer.DeserializeAsync<TransactionsQuery>(stream, cancellationToken: cancellationToken);

            if (transactions == null)
                break;

            allTransactions.AddRange(transactions?.Transactions ?? Enumerable.Empty<Transaction>());
            currentPage++;
            hasNextPage = transactions?.Meta?.NextPage != null;
        } while (hasNextPage);

        return allTransactions;
    }
    public async Task<(string? LegalName, BankAccount? BankAccount)> GetCompanyInfoAsync(string iban, CancellationToken cancellationToken)
    {
#pragma warning disable CS8602 // Incorrect nullable flow analysis for indirect null check.
        var stream = await HttpClient.GetStreamAsync("/v2/organization", cancellationToken);
#pragma warning restore CS8602 // Incorrect nullable flow analysis for indirect null check.

        var organization = await JsonSerializer.DeserializeAsync<OrganizationQuery>(stream, cancellationToken: cancellationToken);
        var account = organization?.Organization?.BankAccounts?.FirstOrDefault(a => a.IBAN?.ToLower() == iban.ToLower());

        return account == null ?
            (null, null) :
            (organization?.Organization?.LegalName, account);
    }
    public async Task<List<Attachment>> GetNewAttachmentsSinceAsync(DateTime since, string iban, CancellationToken cancellationToken)
    {
        var attachments = new List<Attachment>();
        var currentPage = 1;
        bool hasNextPage;
        do
        {
#pragma warning disable CS8602 // Incorrect nullable flow analysis for indirect null check.
            var stream = await HttpClient.GetStreamAsync($"/v2/transactions?iban={iban}" +
                $"&current_page={currentPage}" +
                $"&updated_at_from={since:o}" + // NOTE: Format is 2021-03-03T16:06:38.000Z
                "&with_attachments=true" +
                "&includes[]=attachments" +
                "&status[]=completed&status[]=declined&status[]=pending", cancellationToken);
#pragma warning restore CS8602 // Incorrect nullable flow analysis for indirect null check.

            var transactions = await JsonSerializer.DeserializeAsync<TransactionsQuery>(stream, cancellationToken: cancellationToken);

            if (transactions == null)
                break;

#pragma warning disable CS8603 // Possible null reference return. False positive. 
            attachments.AddRange((transactions?.Transactions ?? Enumerable.Empty<Transaction>())
                .Where(t => t.Attachments != null)
                .SelectMany(t => t.Attachments)
                .Where(a => a?.CreatedAt > since)
                .ToList());
#pragma warning restore CS8603 // Possible null reference return.

            currentPage++;
            hasNextPage = transactions?.Meta?.NextPage != null;
        } while (hasNextPage);

        return attachments
            .DistinctBy(f => f.Url)
            .ToList();
    }
    public static async Task DownloadAttachmentsAsync(IEnumerable<Attachment> attachments, string directory, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();

        foreach (var groupByDay in attachments.GroupBy(a => a.CreatedAt?.ToString("yyyy-MM-dd")))
        {
            var suffix = String.Empty;
            var i = 0;
            foreach (var attachment in groupByDay)
            {
                if (attachment?.Filename == null)
                    continue;

                if (groupByDay.Count() > 1)
                    suffix = $"{++i}_";

                var path = Path.Combine(directory, $"{groupByDay.Key}_{suffix}{attachment.Filename}");
                using var stream = await httpClient.GetStreamAsync(attachment.Url, cancellationToken);
                using var file = File.OpenWrite(path);

                await stream.CopyToAsync(file, cancellationToken);
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _HttpClient?.Dispose();
            }
            disposedValue = true;
        }
    }
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
