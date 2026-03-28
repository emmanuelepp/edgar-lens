using EdgarLens.Core.Interfaces;
using EdgarLens.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace EdgarLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueryController : ControllerBase
{
    private readonly IRagService _ragService;

    public QueryController(IRagService ragService)
    {
        _ragService = ragService;
    }

    [HttpPost]
    public async Task<IActionResult> Query([FromBody] QueryRequest request)
    {
        var answer = await _ragService.QueryAsync(request.Ticker, request.Question);
        return Ok(new QueryResponse
        {
            Ticker = request.Ticker,
            Question = request.Question,
            Answer = answer
        });
    }
}