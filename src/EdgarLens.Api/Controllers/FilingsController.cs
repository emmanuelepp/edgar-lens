using EdgarLens.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EdgarLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilingsController : ControllerBase
{
    private readonly IEdgarClient _edgarClient;
    private readonly IFilingDownloader _filingDownloader;
    private readonly IChunkingService _chunkingService;

    public FilingsController(IEdgarClient edgarClient, IFilingDownloader filingDownloader, IChunkingService chunkingService)
    {
        _edgarClient = edgarClient;
        _filingDownloader = filingDownloader;
        _chunkingService = chunkingService;
    }

    [HttpGet("{ticker}")]
    public async Task<IActionResult> GetFilings(string ticker)
    {
        var filing = await _edgarClient.GetFilingsAsync(ticker);
        if (filing is null) return NotFound();

        var (content, filingId) = await _filingDownloader.DownloadAndSaveAsync(filing);
        if (content is null || filingId is null) return StatusCode(500, "Failed to download filing");

        await _chunkingService.ChunkAndEmbedAsync(filingId.Value, filing.Ticker, content);

        return Ok(filing);
    }
}