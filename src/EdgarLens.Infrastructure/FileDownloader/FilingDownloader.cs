using EdgarLens.Core.Interfaces;
using EdgarLens.Core.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace EdgarLens.Infrastructure.Edgar;

public class FilingDownloader : IFilingDownloader
{
    private readonly HttpClient _httpClient;
    private readonly string _connectionString;
    private readonly EdgarSettings _settings;

public FilingDownloader(HttpClient httpClient, IOptions<EdgarSettings> settings, IConfiguration configuration)
{
    _settings = settings.Value;
    _httpClient = httpClient;
    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", _settings.UserAgent);
    _connectionString = configuration.GetConnectionString("Default")!;
}

public async Task<string?> DownloadAndSaveAsync(Filing filing)
{
    var indexResponse = await _httpClient.GetAsync(filing.DocumentUrl);
    if (!indexResponse.IsSuccessStatusCode) return null;

    var indexHtml = await indexResponse.Content.ReadAsStringAsync();

    var documentUrl = ExtractMainDocumentUrl(indexHtml, filing);
    if (documentUrl is null) return null;

    var docResponse = await _httpClient.GetAsync(documentUrl);
    if (!docResponse.IsSuccessStatusCode) return null;

    var docHtml = await docResponse.Content.ReadAsStringAsync();
    var plainText = ExtractText(docHtml);
    await SaveToDatabase(filing, plainText);

    return plainText;
}

private string? ExtractMainDocumentUrl(string indexHtml, Filing filing)
{
    var doc = new HtmlDocument();
    doc.LoadHtml(indexHtml);

    var links = doc.DocumentNode.SelectNodes("//a[@href]");
    if (links is null) return null;

    foreach (var link in links)
    {
        var href = link.GetAttributeValue("href", "");
        
        if (href.Contains("/ix?doc="))
        {
            href = href.Replace("/ix?doc=", "");
        }

        if (href.EndsWith(".htm") && href.Contains("/Archives/"))
        {
            return $"{_settings.ArchivesBaseUrl}{href}";
        }
    }

    return null;
}

    private string ExtractText(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        foreach (var node in doc.DocumentNode.SelectNodes("//script|//style") ?? Enumerable.Empty<HtmlNode>())
            node.Remove();

        var text = doc.DocumentNode.InnerText;
        var lines = text.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l));

        return string.Join("\n", lines);
    }

    private async Task SaveToDatabase(Filing filing, string content)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = """
            INSERT INTO filings (ticker, company_name, cik, accession_number, filing_date, raw_content)
            VALUES (@ticker, @companyName, @cik, @accessionNumber, @filingDate, @rawContent)
            ON CONFLICT (accession_number) DO NOTHING
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("ticker", filing.Ticker);
        cmd.Parameters.AddWithValue("companyName", filing.CompanyName);
        cmd.Parameters.AddWithValue("cik", filing.Cik);
        cmd.Parameters.AddWithValue("accessionNumber", filing.AccessionNumber);
        cmd.Parameters.AddWithValue("filingDate", DateOnly.Parse(filing.FilingDate));
        cmd.Parameters.AddWithValue("rawContent", content);

        await cmd.ExecuteNonQueryAsync();
    }
}