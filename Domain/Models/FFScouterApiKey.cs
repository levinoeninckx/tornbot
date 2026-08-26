namespace TornBot.Bot.Domain.Models;

public class FFScouterApiKey(string apiKey, int tornPlayerId)
{
    private readonly ApiKey _apiKey = new(tornPlayerId, apiKey, Enums.AccessLevel.FfScouter);

    public string Key => _apiKey.Key;
}
