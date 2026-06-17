namespace TornBot.Bot.Shared;

public class ApiKeyService
{
    private string? _apiKey;
    
    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
    }
    
    public string? GetApiKey() => _apiKey;
}