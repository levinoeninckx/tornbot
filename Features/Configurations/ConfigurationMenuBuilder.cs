using NetCord;
using NetCord.Rest;
using TornBot.Bot.Domain.Enums;

namespace TornBot.Bot.Features.Configurations;

public class ConfigurationMenuBuilder
{
    private InteractionMessageProperties _message = new();

    public ConfigurationMenuBuilder AddEnableModuleMenu(string customId, ModuleState state)
    {
        _message
            .AddComponents(
                new TextDisplayProperties("Enable/disable"), 
                new StringMenuProperties(customId)
                .WithOptions([
                    new StringMenuSelectOptionProperties("Enabled", nameof(ModuleState.Enabled))
                        { Default = state == ModuleState.Enabled },
                    new StringMenuSelectOptionProperties("Disabled", nameof(ModuleState.Disabled))
                        { Default = state == ModuleState.Disabled }
                ])
                .WithMinValues(1)
                .WithMaxValues(1));

        return this;
    }
    
    public ConfigurationMenuBuilder AddRequiredRolesMenu(string customId, IEnumerable<ulong>? defaultValues = null)
    {
        _message
            .AddComponents(
                new TextDisplayProperties("Required roles (anyone can use if empty)"),
                new RoleMenuProperties(customId)
                    .WithPlaceholder("Select roles that are allowed to access these commands")
                    .WithDefaultValues(defaultValues)
                    .WithMinValues(0)
                    .WithMaxValues(25)
            );

        return this;
    }

    public ConfigurationMenuBuilder AddRestrictedChannelsMenu(string customId, IEnumerable<ulong>? defaultValues = null)
    {
        _message
            .AddComponents(
                new TextDisplayProperties("Restricted channels (can be used anywhere if empty)"),
                new ChannelMenuProperties(customId)
                    .WithPlaceholder("Select channels where these commands can be used")
                    .WithMinValues(0)
                    .WithMaxValues(25)
                    .WithDefaultValues(defaultValues)
            );

        return this;
    }
    
    public InteractionMessageProperties Build()
    {
        _message
            .WithFlags(MessageFlags.Ephemeral | MessageFlags.IsComponentsV2);
        return _message;
    }
}