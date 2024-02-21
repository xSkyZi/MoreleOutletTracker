using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using MoreleOutletTracker.MoreleTracker.JSONObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoreleOutletTracker.MoreleTracker
{
    [SlashCommandGroup("Config", "Commands for changing config")]
    public class MoreleCommands : ApplicationCommandModule
    {
        [SlashCommand("Setup", "Setup in which channel bot is gonna send messages + which role to ping", false)]
        [SlashRequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [GuildOnly]
        public async Task SetupBot(InteractionContext ctx, [Option("TextChannel", "In which channel should I text?", true)] DiscordChannel textChannel, [Option("Role", "Which role should I ping while new product is found?", true)] DiscordRole role, [Option("Cooldown", "Every X minutes should I fetch data from morele?")] long cooldownFetch)
        {
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            var response = new DiscordWebhookBuilder();

            if (textChannel.Type == ChannelType.Voice || textChannel.Type == ChannelType.Category || textChannel.Type == ChannelType.Unknown)
            {
                response.Content = "Please input correct Text Channel!";

                await ctx.EditResponseAsync(response);
                return;
            }
            if (cooldownFetch == 0)
            {
                response.Content = "Cooldown time should be minimum 1!";

                await ctx.EditResponseAsync(response);
                return;
            }

            if (cooldownFetch % 1 != 0)
            {
                response.Content = "Please provide full number!";

                await ctx.EditResponseAsync(response);
                return;
            }

            await JsonFM.SaveToConfig(textChannel.Id, role.Id, cooldownFetch);
            response.Content = "Succesfully saved config! to change separate values, use `/config textchannel`, `/config role` or `/config cooldown`";
            await ctx.EditResponseAsync(response);
        }

        [SlashCommand("TextChannel", "Set in which channel bot is supposed to send messages", false)]
        [SlashRequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [GuildOnly]
        public async Task TextChannelSetup(InteractionContext ctx, [Option("TextChannel", "In which channel should I text?", true)] DiscordChannel textChannel)
        {
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            var response = new DiscordWebhookBuilder();

            if (Config.channelId == 0)
            {
                response.Content = "Please set up the bot first by using `/config setup` before changing individual parameters!";

                await ctx.EditResponseAsync(response);
                return;
            }

            if (textChannel.Type == ChannelType.Voice || textChannel.Type == ChannelType.Category || textChannel.Type == ChannelType.Unknown)
            {
                response.Content = "Please input correct Text Channel!";

                await ctx.EditResponseAsync(response);
                return;
            }

            await JsonFM.SaveToConfig(textChannel.Id, 0, 0);
            
            response.Content = $"Sucessfully changed text channel to <#{textChannel.Id}>!";
            await ctx.EditResponseAsync(response);
        }

        [SlashCommand("Role", "Set which role should bot ping on new offer", false)]
        [SlashRequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [GuildOnly]
        public async Task RoleSetup(InteractionContext ctx, [Option("Role", "Which role should I ping while new product is found?", true)] DiscordRole role)
        {
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            var response = new DiscordWebhookBuilder();

            if (Config.mentionRoleId == 0)
            {
                response.Content = "Please set up the bot first by using `/config setup` before changing individual parameters!";

                await ctx.EditResponseAsync(response);
                return;
            }

            await JsonFM.SaveToConfig(0, role.Id, 0);

            response.Content = $"Successfully changed to mention {role.Mention} when posting new offer!";
        }

        [SlashCommand("Cooldown", "Set cooldown for fetching data from Morele", false)]
        [SlashRequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [GuildOnly]
        public async Task CooldownSetup(InteractionContext ctx, [Option("Cooldown", "Every X minutes should I fetch data from morele?")] long cooldownFetch)
        {
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            var response = new DiscordWebhookBuilder();

            if (Config.fetchCooldown == 0)
            {
                response.Content = "Please set up the bot first by using `/config setup` before changing individual parameters!";

                await ctx.EditResponseAsync(response);
                return;
            }

            if (cooldownFetch % 1 != 0)
            {
                response.Content = "Please provide full number!";

                await ctx.EditResponseAsync(response);
                return;
            }

            await JsonFM.SaveToConfig(0, 0, cooldownFetch);

            response.Content = $"Successfully changed fetch cooldown to `{(int)cooldownFetch} minutes`!";
            await ctx.EditResponseAsync(response);
        }
    }
}
