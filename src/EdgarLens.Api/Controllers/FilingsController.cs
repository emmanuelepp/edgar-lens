using EdgarLens.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EdgarLens.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilingsController : ControllerBase
    {
        private readonly IEdgarClient _edgarClient;

        public FilingsController(IEdgarClient edgarClient)
        {
            _edgarClient = edgarClient;
        }

        [HttpGet("{ticker}")]
        public async Task<IActionResult> GetFilings(string ticker)
        {
            var filing = await _edgarClient.GetFilingsAsync(ticker);
            return filing is null ? NotFound() : Ok(filing);
        }

    }
}