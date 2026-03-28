using EdgarLens.Core.Models;

namespace EdgarLens.Core.Interfaces;

public interface IFilingDownloader
{
    Task<string?> DownloadAndSaveAsync(Filing filing);
}