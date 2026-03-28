using EdgarLens.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EdgarLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilingsController : ControllerBase
{
    private readonly IEdgarClient _edgarClient;
    private readonly IFilingDownloader _filingDownloader;

    public FilingsController(IEdgarClient edgarClient, IFilingDownloader filingDownloader)
    {
        _edgarClient = edgarClient;
        _filingDownloader = filingDownloader;
    }

    [HttpGet("{ticker}")]
    public async Task<IActionResult> GetFilings(string ticker)
    {
        var filing = await _edgarClient.GetFilingsAsync(ticker);
        if (filing is null) return NotFound();

        var content = await _filingDownloader.DownloadAndSaveAsync(filing);
        if (content is null) return StatusCode(500, "Failed to download filing");

        return Ok(filing);
    }
}