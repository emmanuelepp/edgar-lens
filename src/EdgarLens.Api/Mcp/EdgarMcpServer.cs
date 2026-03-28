using EdgarLens.Core.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace EdgarLens.Api.Mcp;

[McpServerToolType]
public class EdgarMcpServer
{
    private readonly IRagService _ragService;
    private readonly IEdgarClient _edgarClient;
    private readonly IFilingDownloader _filingDownloader;
    private readonly IChunkingService _chunkingService;

    public EdgarMcpServer(
        IRagService ragService,
        IEdgarClient edgarClient,
        IFilingDownloader filingDownloader,
        IChunkingService chunkingService)
    {
        _ragService = ragService;
        _edgarClient = edgarClient;
        _filingDownloader = filingDownloader;
        _chunkingService = chunkingService;
    }

    [McpServerTool, Description("Query a company's SEC 10-K filing using natural language. Returns an answer grounded in the filing.")]
    public async Task<string> QueryFiling(
        [Description("Stock ticker symbol, AAPL, MSFT, AMZN")] string ticker,
        [Description("Natural language question about the company's financials or operations")] string question)
    {
        return await _ragService.QueryAsync(ticker, question);
    }

    [McpServerTool, Description("Download and index a company's latest 10-K filing from SEC EDGAR into the knowledge base.")]
    public async Task<string> IndexFiling(
        [Description("Stock ticker symbol, AAPL, MSFT, AMZN")] string ticker)
    {
        var filing = await _edgarClient.GetFilingsAsync(ticker);
        if (filing is null) return $"Could not find 10-K filing for {ticker}.";

        var (content, filingId) = await _filingDownloader.DownloadAndSaveAsync(filing);
        if (content is null || filingId is null) return $"Failed to download filing for {ticker}.";

        await _chunkingService.ChunkAndEmbedAsync(filingId.Value, ticker, content);
        return $"Successfully indexed 10-K for {ticker} filed on {filing.FilingDate}.";
    }
}