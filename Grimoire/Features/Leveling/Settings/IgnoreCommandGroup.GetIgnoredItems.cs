// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using System.Text;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Grimoire.DatabaseQueryHelpers;
using ChannelExtensions = Grimoire.Extensions.ChannelExtensions;

namespace Grimoire.Features.Leveling.Settings;

public sealed partial class IgnoreCommandGroup
{
    [SlashCommand("View", "View all currently ignored users, channels and roles for the server.")]
    public async Task ShowIgnoredAsync(InteractionContext ctx)
    {
        var response = await this._mediator.Send(new GetIgnoredItems.Query { GuildId = ctx.Guild.Id });

        var embed = new DiscordEmbedBuilder()
            .WithTitle("Ignored Channels Roles and Users.")
            .WithTimestamp(DateTime.UtcNow);
        var embedPages = InteractivityExtension.GeneratePagesInEmbed(response.Message, SplitType.Line, embed);
        await ctx.Interaction.SendPaginatedResponseAsync(false, ctx.User, embedPages);
    }
}

public sealed class GetIgnoredItems
{
    public sealed record Query : IRequest<BaseResponse>
    {
        public ulong GuildId { get; init; }
    }

    public sealed class Handler(GrimoireDbContext grimoireDbContext) : IRequestHandler<Query, BaseResponse>
    {
        private readonly GrimoireDbContext _grimoireDbContext = grimoireDbContext;

        public async Task<BaseResponse> Handle(Query request, CancellationToken cancellationToken)
        {
            var ignoredItems = await this._grimoireDbContext.Guilds
                .AsNoTracking()
                .AsSplitQuery()
                .WhereIdIs(request.GuildId)
                .Select(guild => new
                {
                    IgnoredRoles = guild.IgnoredRoles.Select(ignoredRole => ignoredRole.RoleId),
                    IgnoredChannels = guild.IgnoredChannels.Select(ignoredChannel => ignoredChannel.ChannelId),
                    IgnoredMembers = guild.IgnoredMembers.Select(ignoredMember => ignoredMember.UserId)
                }).FirstOrDefaultAsync(cancellationToken);


            if (ignoredItems is null)
                throw new AnticipatedException("Could not find the settings for this server.");

            if (!ignoredItems.IgnoredRoles.Any() && !ignoredItems.IgnoredChannels.Any() &&
                !ignoredItems.IgnoredMembers.Any())
                throw new AnticipatedException("This server does not have any ignored channels, roles or users.");

            var ignoredMessageBuilder = new StringBuilder().Append("**Channels**\n");

            foreach (var channel in ignoredItems.IgnoredChannels)
                ignoredMessageBuilder.Append(ChannelExtensions.Mention(channel)).Append('\n');

            ignoredMessageBuilder.Append("\n**Roles**\n");

            foreach (var role in ignoredItems.IgnoredRoles)
                ignoredMessageBuilder.Append(RoleExtensions.Mention(role)).Append('\n');

            ignoredMessageBuilder.Append("\n**Users**\n");

            foreach (var member in ignoredItems.IgnoredMembers)
                ignoredMessageBuilder.Append(UserExtensions.Mention(member)).Append('\n');

            return new BaseResponse { Message = ignoredMessageBuilder.ToString() };
        }
    }
}
