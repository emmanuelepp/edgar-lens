using EdgarLens.Core.Interfaces;
using EdgarLens.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Pgvector;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace EdgarLens.Infrastructure.Rag;

public class RagService : IRagService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly HttpClient _httpClient;
    private readonly ILogger<RagService> _logger;
    private readonly EdgarSettings _settings;

    public RagService(HttpClient httpClient, IOptions<EdgarSettings> settings, IConfiguration configuration, ILogger<RagService> logger)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_settings.OllamaBaseUrl);
        _logger = logger;

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(configuration.GetConnectionString("Default")!);
        dataSourceBuilder.UseVector();
        _dataSource = dataSourceBuilder.Build();
    }

    public async Task<string> QueryAsync(string ticker, string question)
    {
        var questionEmbedding = await GetEmbeddingAsync(question);
        if (questionEmbedding is null) return "Failed to process question.";

        var chunks = await SearchSimilarChunksAsync(ticker, questionEmbedding);
        if (chunks.Count == 0) return "No relevant information found for this ticker.";

        var context = string.Join("\n\n", chunks);
        return await GenerateAnswerAsync(question, context);
    }

    private async Task<float[]?> GetEmbeddingAsync(string text)
    {
        var request = new { model = _settings.EmbeddingModel, prompt = text };
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

    private async Task<List<string>> SearchSimilarChunksAsync(string ticker, float[] embedding)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();

        const string sql = """
            SELECT content
            FROM filing_chunks
            WHERE ticker = @ticker
            ORDER BY embedding <=> @embedding
            LIMIT 5
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("ticker", ticker);
        cmd.Parameters.AddWithValue("embedding", new Vector(embedding));

        var chunks = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            chunks.Add(reader.GetString(0));

        return chunks;
    }

    private async Task<string> GenerateAnswerAsync(string question, string context)
    {
        var prompt = $"""
            You are a financial analyst assistant. Use the following excerpts from a SEC 10-K filing to answer the question.
            Only use the information provided. If the answer is not in the context, say so.

            Context:
            {context}

            Question: {question}

            Answer:
            """;

        var request = new
        {
            model = _settings.ChatModel,
            prompt = prompt,
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync("/api/generate", request);
        if (!response.IsSuccessStatusCode) return "Failed to generate answer.";

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("response").GetString() ?? "No response.";
    }
}