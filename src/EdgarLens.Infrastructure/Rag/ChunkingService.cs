using EdgarLens.Core.Interfaces;
using EdgarLens.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Pgvector;
using System.Net.Http.Json;
using System.Text.Json;

namespace EdgarLens.Infrastructure.Rag;

public class ChunkingService : IChunkingService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChunkingService> _logger;
    private readonly EdgarSettings _settings;
    private const int ChunkSize = 500;
    private const int ChunkOverlap = 50;

    public ChunkingService(HttpClient httpClient, IOptions<EdgarSettings> settings, IConfiguration configuration, ILogger<ChunkingService> logger)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_settings.OllamaBaseUrl);
        _logger = logger;

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(configuration.GetConnectionString("Default")!);
        dataSourceBuilder.UseVector();
        _dataSource = dataSourceBuilder.Build();
    }

    public async Task ChunkAndEmbedAsync(Guid filingId, string ticker, string content)
    {
        var chunks = SplitIntoChunks(content);
        _logger.LogInformation("Split filing {FilingId} into {Count} chunks", filingId, chunks.Count);

        for (int i = 0; i < chunks.Count; i++)
        {
            var embedding = await GetEmbeddingAsync(chunks[i]);
            if (embedding is null)
            {
                _logger.LogWarning("Failed to get embedding for chunk {Index}", i);
                continue;
            }

            await SaveChunkAsync(filingId, ticker, i, chunks[i], embedding);
        }

        _logger.LogInformation("Finished embedding {Count} chunks for {Ticker}", chunks.Count, ticker);
    }

    private List<string> SplitIntoChunks(string content)
    {
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();

        int i = 0;
        while (i < words.Length)
        {
            var chunk = string.Join(" ", words.Skip(i).Take(ChunkSize));
            chunks.Add(chunk);
            i += ChunkSize - ChunkOverlap;
        }

        return chunks;
    }

    private async Task<float[]?> GetEmbeddingAsync(string text)
    {
        var request = new { model = "nomic-embed-text", prompt = text };
        var response = await _httpClient.PostAsJsonAsync("/api/embeddings", request);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(e => e.GetSingle())
            .ToArray();
    }

    private async Task SaveChunkAsync(Guid filingId, string ticker, int index, string content, float[] embedding)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();

        const string sql = """
            INSERT INTO filing_chunks (filing_id, ticker, chunk_index, content, embedding)
            VALUES (@filingId, @ticker, @chunkIndex, @content, @embedding)
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("filingId", filingId);
        cmd.Parameters.AddWithValue("ticker", ticker);
        cmd.Parameters.AddWithValue("chunkIndex", index);
        cmd.Parameters.AddWithValue("content", content);
        cmd.Parameters.AddWithValue("embedding", new Vector(embedding));

        await cmd.ExecuteNonQueryAsync();
    }
}