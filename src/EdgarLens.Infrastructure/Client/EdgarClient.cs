using System.Text.Json;
using EdgarLens.Core.Interfaces;
using EdgarLens.Core.Models;
using Microsoft.Extensions.Options;

namespace EdgarLens.Infrastructure.Edgar;

public class EdgarClient : IEdgarClient
{
    private readonly HttpClient _httpClient;
    private readonly EdgarSettings _settings;

    public EdgarClient(HttpClient httpClient, IOptions<EdgarSettings> settings)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", _settings.UserAgent);
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
    }

    public async Task<Filing?> GetFilingsAsync(string ticker)
    {
        var cik = await GetCikAsync(ticker);
        if (cik is null) return null;

        var filing = await GetLatestFilingAsync(cik, ticker);
        return filing;
    }

    private async Task<string?> GetCikAsync(string ticker)
    {
        var response = await _httpClient.GetAsync(_settings.TickersUrl);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        foreach (var company in doc.RootElement.EnumerateObject())
        {
            var tickerValue = company.Value.GetProperty("ticker").GetString();
            if (string.Equals(tickerValue, ticker, StringComparison.OrdinalIgnoreCase))
            {
                return company.Value.GetProperty("cik_str").GetInt32().ToString();
            }
        }

        return null;
    }


    private async Task<Filing?> GetLatestFilingAsync(string cik, string ticker)
    {
        var paddedCik = cik.PadLeft(10, '0');
        var url = $"/submissions/CIK{paddedCik}.json";
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        var filings = doc.RootElement
            .GetProperty("filings")
            .GetProperty("recent");

        var forms = filings.GetProperty("form").EnumerateArray().ToList();
        var dates = filings.GetProperty("filingDate").EnumerateArray().ToList();
        var accessions = filings.GetProperty("accessionNumber").EnumerateArray().ToList();
        var companyName = doc.RootElement.GetProperty("name").GetString() ?? ticker;

        for (int i = 0; i < forms.Count; i++)
        {
            if (forms[i].GetString() == "10-K")
            {
                var accession = accessions[i].GetString()!.Replace("-", "");
                var accessionFormatted = accessions[i].GetString()!;
                var documentUrl = $"https://www.sec.gov/Archives/edgar/data/{cik}/{accession}/{accessionFormatted}-index.htm";

                return new Filing
                {
                    Ticker = ticker.ToUpper(),
                    CompanyName = companyName,
                    Cik = cik,
                    AccessionNumber = accessionFormatted,
                    FilingDate = dates[i].GetString()!,
                    DocumentUrl = documentUrl
                };
            }
        }

        return null;
    }
}