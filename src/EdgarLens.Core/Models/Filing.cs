namespace EdgarLens.Core.Models;

public class Filing
{
    public string Ticker {get; set; } = string.Empty;
    public string CompanyName {get; set; } = string.Empty;
    public string Cik {get; set; } = string.Empty;
    public string AccessionNumber {get; set; } = string.Empty;
    public string FilingDate {get; set; } = string.Empty;
    public string DocumentUrl {get; set; } = string.Empty;
}