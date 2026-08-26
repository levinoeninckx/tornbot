namespace TornBot.Bot.Domain.Models;

public class TornStatApiKey(string apiKey, int tornPlayerId)
{
    private readonly ApiKey _apiKey = new(tornPlayerId, apiKey, Enums.AccessLevel.TornStats);

    public string Key => _apiKey.Key;
}
