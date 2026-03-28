using EdgarLens.Core.Models;

namespace EdgarLens.Core.Interfaces;

public interface IFilingDownloader
{
   Task<(string? Content, Guid? FilingId)> DownloadAndSaveAsync(Filing filing);
}