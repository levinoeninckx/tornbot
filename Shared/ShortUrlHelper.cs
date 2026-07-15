namespace TornBot.Bot.Shared;

public static class ShortUrlHelper
{
    private static Uri GetPayloadUrl(int id, string payload) => new ($"https://tcy.sh/{payload}/{id}");
    public static Uri GetProfileUrl(int playerId) => GetPayloadUrl(playerId, "p");
    public static Uri GetAttackUrl(int playerId) => GetPayloadUrl(playerId, "a");
}