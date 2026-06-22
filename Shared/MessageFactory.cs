using NetCord;
using NetCord.Rest;

namespace TornBot.Bot.Shared;

public static class MessageFactory
{
    public static T CreateDefaultMessage<T>(string title, string content) where T : IMessageProperties, new()
    {
        return new T
        {
            Embeds =
            [
                new EmbedProperties
                {
                    Title = title,
                    Description = content
                }
            ]
        };
    }

    public static T CreateErrorMessage<T>(string? error = null) where T : IMessageProperties, new()
    {
        return new T
        {
            Embeds =
            [
                new EmbedProperties
                {
                    Title = "Oops!",
                    Description = error ?? "Something went wrong. Please try again later."
                }
            ]
        };
    }

    public static T CreateEphermalMessage<T>(string title, string message) where T : IMessageProperties, new()
    {
        return new T
        {
            Embeds =
            [
                new EmbedProperties
                {
                    Title = title,
                    Description = message
                }
            ],
            Flags = MessageFlags.Ephemeral
        };
    }
}