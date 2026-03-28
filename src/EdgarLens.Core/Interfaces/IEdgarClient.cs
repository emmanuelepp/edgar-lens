using EdgarLens.Core.Models;

namespace EdgarLens.Core.Interfaces;

public interface IEdgarClient
{
    Task<Filing?> GetFilingsAsync(string ticker);
}