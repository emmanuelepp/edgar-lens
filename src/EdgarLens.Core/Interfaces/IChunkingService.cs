namespace EdgarLens.Core.Interfaces;

public interface IChunkingService
{
    Task ChunkAndEmbedAsync(Guid filingId, string ticker, string content);
}