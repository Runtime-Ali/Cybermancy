// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using Grimoire.Core.Exceptions;
using Grimoire.Core.Features.Moderation.Queries.GetModActionsCounts;
using Grimoire.Core.Features.Moderation.Queries.GetModLogsForUser;

namespace Grimoire.Discord.ModerationModule
{
    [SlashRequireGuild]
    [SlashRequireModuleEnabled(Module.Moderation)]
    public class SinLogCommands : ApplicationCommandModule
    {
        private readonly IMediator _mediator;

        public SinLogCommands(IMediator mediator)
        {
            this._mediator = mediator;
        }

        [SlashCommand("SinLog", "Looks up the sin logs for the provided user.")]
        public async Task SinLogAsync(
            InteractionContext ctx,
            [Option("Type", "The Type of logs to lookup.")] SinQueryType sinQueryType,
            [Option("User", "The user to look up the logs for. Leave blank for self.")] DiscordUser? user = null)
        {
            user ??= ctx.User;


            if ((!ctx.Member.Permissions.HasPermission(Permissions.ManageMessages)) && ctx.User != user)
                throw new AnticipatedException("Only moderators can look up logs for someone else.");
            if (sinQueryType == SinQueryType.Mod)
            {
                var modResponse = await this._mediator.Send(new GetModActionCountsQuery
                {
                    UserId = user.Id,
                    GuildId = ctx.Guild.Id
                });
                if (modResponse is null)
                {
                    await ctx.ReplyAsync(GrimoireColor.Red, "Did not find a moderator with that id.");
                    return;
                }
                await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
                    .WithAuthor($"Moderation log for {user.GetUsernameWithDiscriminator()}")
                    .AddField("Bans", modResponse.BanCount.ToString(), true)
                    .AddField("Mutes", modResponse.MuteCount.ToString(), true)
                    .AddField("Warns", modResponse.WarnCount.ToString(), true)
                    .WithColor(GrimoireColor.Purple));
                return;
            }
            var response = await this._mediator.Send(new GetUserSinsQuery
            {
                UserId = user.Id,
                GuildId = ctx.Guild.Id,
                SinQueryType = sinQueryType
            });
            if (!response.SinList.Any())
                await ctx.ReplyAsync(GrimoireColor.Green, message: "That user does not have any logs",
                    ephemeral: !ctx.Member.Permissions.HasPermission(Permissions.ManageMessages));
            foreach (var message in response.SinList)
                await ctx.ReplyAsync(GrimoireColor.Green, message: message,
                    ephemeral: !ctx.Member.Permissions.HasPermission(Permissions.ManageMessages));

        }
    }
}
