using Xunit;

namespace EdgarLens.Tests;

public class ChunkingServiceTests
{
    private List<string> SplitIntoChunks(string content, int chunkSize = 500, int overlap = 50)
    {
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();

        int i = 0;
        while (i < words.Length)
        {
            var chunk = string.Join(" ", words.Skip(i).Take(chunkSize));
            chunks.Add(chunk);
            i += chunkSize - overlap;
        }

        return chunks;
    }

    [Fact]
    public void SplitIntoChunks_ShortText_ReturnsSingleChunk()
    {
        var text = string.Join(" ", Enumerable.Repeat("word", 100));
        var chunks = SplitIntoChunks(text);
        Assert.Single(chunks);
    }

    [Fact]
    public void SplitIntoChunks_LongText_ReturnsMultipleChunks()
    {
        var text = string.Join(" ", Enumerable.Repeat("word", 1000));
        var chunks = SplitIntoChunks(text);
        Assert.True(chunks.Count > 1);
    }

    [Fact]
    public void SplitIntoChunks_EmptyText_ReturnsNoChunks()
    {
        var chunks = SplitIntoChunks("");
        Assert.Empty(chunks);
    }

    [Fact]
    public void SplitIntoChunks_ChunksHaveOverlap()
    {
        var words = Enumerable.Range(1, 600).Select(i => $"word{i}").ToList();
        var text = string.Join(" ", words);
        var chunks = SplitIntoChunks(text, chunkSize: 500, overlap: 50);

        var firstChunkWords = chunks[0].Split(' ');
        var secondChunkWords = chunks[1].Split(' ');
        var overlapWords = firstChunkWords.TakeLast(50);
        var secondChunkStart = secondChunkWords.Take(50);

        Assert.Equal(overlapWords, secondChunkStart);
    }

    [Fact]
    public void SplitIntoChunks_ExactChunkSize_ReturnsSingleChunk()
    {
        var text = string.Join(" ", Enumerable.Repeat("word", 500));
        var chunks = SplitIntoChunks(text);
        Assert.Single(chunks);
    }
}