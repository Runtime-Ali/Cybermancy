// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using DSharpPlus.Exceptions;
using Grimoire.Core.Features.Leveling.Commands.ManageXpCommands.GainUserXp;

namespace Grimoire.Discord.LevelingModule;

[DiscordMessageCreatedEventSubscriber]
public class LevelingEvents : IDiscordMessageCreatedEventSubscriber
{
    private readonly IMediator _mediator;

    public LevelingEvents(IMediator mediator)
    {
        this._mediator = mediator;
    }

    public async Task DiscordOnMessageCreated(DiscordClient sender, MessageCreateEventArgs args)
    {
        if (args.Message.MessageType is not MessageType.Default or MessageType.Reply ||
            args.Author is not DiscordMember member) return;
        if (member.IsBot) return;
        var response = await this._mediator.Send(new GainUserXpCommand
        {
            ChannelId = args.Channel.Id,
            GuildId = args.Guild.Id,
            UserId = member.Id,
            RoleIds = member.Roles.Select(x => x.Id).ToArray()
        });
        if (!response.Success) return;

        var newRewards = response.EarnedRewards
            .Where(x => !member.Roles.Any(y => y.Id == x))
            .ToArray();

        var rolesToAdd = newRewards
            .Join(args.Guild.Roles, x => x, y => y.Key, (x, y) => y.Value)
            .Concat(member.Roles)
            .Distinct()
            .ToArray();

        if (rolesToAdd.Except(member.Roles).Any())
        {
            try
            {
                await member.ReplaceRolesAsync(rolesToAdd);
            }
            catch (UnauthorizedException)
            {
                await SendErrorLogs(
                    args.Guild.Channels,
                    args.Guild.CurrentMember.DisplayName,
                    newRewards,
                    response.LogChannelId,
                    response.LevelLogChannel);
            }
        }

        if (response.LevelLogChannel is null) return;

        if (!args.Guild.Channels.TryGetValue(response.LevelLogChannel.Value,
            out var loggingChannel)) return;

        if (response.PreviousLevel < response.CurrentLevel)
            await loggingChannel.SendMessageAsync(new DiscordEmbedBuilder()
                .WithColor(GrimoireColor.Purple)
                .WithAuthor(member.GetUsernameWithDiscriminator())
                .WithDescription($"{member.Mention} has leveled to level {response.CurrentLevel}.")
                .WithFooter($"{member.Id}")
                .WithTimestamp(DateTime.UtcNow)
                .Build());

        if (newRewards.Any())
            await loggingChannel.SendMessageAsync(new DiscordEmbedBuilder()
                .WithColor(GrimoireColor.DarkPurple)
                .WithAuthor($"{member.Username}#{member.Discriminator}")
                .WithDescription($"{member.Mention} has earned " +
                $"{string.Join(' ', newRewards.Select(x => RoleExtensions.Mention(x)))}")
                .WithFooter($"{member.Id}")
                .WithTimestamp(DateTime.UtcNow)
                .Build());
    }

    private static async Task SendErrorLogs(
        IReadOnlyDictionary<ulong, DiscordChannel> channels,
        string displayName,
        ulong[] rewards,
        ulong? modLogChannelId,
        ulong? levelLogChannelId)
    {
        if (modLogChannelId is not null)
        {
            if (channels.TryGetValue(modLogChannelId.Value, out var modLogChannel))
                await modLogChannel.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithColor(GrimoireColor.Red)
                    .WithDescription($"{displayName} tried to grant roles " +
                    $"{string.Join(' ', rewards.Select(RoleExtensions.Mention))} but did not have sufficent permissions."));
        }
        if (levelLogChannelId is not null)
        {
            if (channels.TryGetValue(levelLogChannelId.Value, out var levelLogChannel))
                await levelLogChannel.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithColor(GrimoireColor.Red)
                    .WithDescription($"{displayName} tried to grant roles " +
                    $"{string.Join(' ', rewards.Select(RoleExtensions.Mention))} but did not have sufficent permissions."));
        }
    }
}
