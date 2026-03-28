namespace EdgarLens.Core.Interfaces;

public interface IRagService
{
    Task<string> QueryAsync(string ticker, string question);
}