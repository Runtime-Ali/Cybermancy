// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using Grimoire.Discord.Notifications;
using Grimoire.Features.MessageLogging.Queries;

namespace Grimoire.LoggingModule;

internal sealed class TrackerUsernameEvent(DiscordClient clientService, IMediator mediator) : INotificationHandler<UsernameTrackerNotification>
{
    private readonly DiscordClient _clientService = clientService;
    private readonly IMediator _mediator = mediator;

    public async ValueTask Handle(UsernameTrackerNotification notification, CancellationToken cancellationToken)
    {
        var response = await this._mediator.Send(new GetTrackerQuery{ UserId = notification.UserId, GuildId = notification.GuildId }, cancellationToken);

        if (response is null) return;
        if (!this._clientService.Guilds.TryGetValue(notification.GuildId, out var guild)) return;
        if (!guild.Channels.TryGetValue(response.TrackerChannelId, out var logChannel)) return;

        var embed = new DiscordEmbedBuilder()
                        .WithAuthor("Username Updated")
                        .AddField("User", UserExtensions.Mention(notification.UserId))
                        .AddField("Before", string.IsNullOrWhiteSpace(notification.BeforeUsername)? "`Unknown`" : notification.BeforeUsername, true)
                        .AddField("After", string.IsNullOrWhiteSpace(notification.AfterUsername)? "`Unknown`" : notification.AfterUsername, true)
                        .WithTimestamp(DateTimeOffset.UtcNow)
                        .WithColor(GrimoireColor.Mint);
        await logChannel.SendMessageAsync(embed);
    }
}