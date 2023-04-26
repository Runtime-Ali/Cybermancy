// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using System.Net;
using Grimoire.Core.Features.Logging.Commands.UpdateAvatar;
using Grimoire.Core.Features.Logging.Commands.UpdateNickname;
using Grimoire.Core.Features.Logging.Commands.UpdateUsername;
using Grimoire.Core.Features.Logging.Queries.GetUserLogSettings;
using Microsoft.Extensions.Logging;

namespace Grimoire.Discord.LoggingModule
{
    [DiscordGuildMemberAddedEventSubscriber]
    [DiscordGuildMemberUpdatedEventSubscriber]
    [DiscordGuildMemberRemovedEventSubscriber]
    internal class MemberLogEvents :
        IDiscordGuildMemberAddedEventSubscriber,
        IDiscordGuildMemberUpdatedEventSubscriber,
        IDiscordGuildMemberRemovedEventSubscriber
    {
        private readonly IMediator _mediator;
        private readonly IInviteService _inviteService;
        private readonly HttpClient _httpClient;
        private readonly IDiscordClientService _clientService;

        public MemberLogEvents(IMediator mediator, IInviteService inviteService, IHttpClientFactory httpFactory, IDiscordClientService clientService)
        {
            this._mediator = mediator;
            this._inviteService = inviteService;
            this._httpClient = httpFactory.CreateClient();
            this._clientService = clientService;
        }

        public async Task DiscordOnGuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs args)
        {
            var settings = await this._mediator.Send(new GetUserLogSettingsQuery{ GuildId = args.Guild.Id });
            if (!settings.IsLoggingEnabled) return;
            if (settings.JoinChannelLog is null) return;
            var logChannel = args.Guild.Channels.GetValueOrDefault(settings.JoinChannelLog.Value);
            if (logChannel is null) return;

            var accountAge = DateTime.UtcNow - args.Member.CreationTimestamp;
            var invites = await args.Guild.GetInvitesAsync();
            var inviteUsed = this._inviteService.CalculateInviteUsed(
                invites.Select(x =>
                new Domain.Invite
                {
                    Code = x.Code,
                    Inviter = x.Inviter.GetUsernameWithDiscriminator(),
                    Url = x.ToString(),
                    Uses = x.Uses
                }).ToList());

            var embed = new DiscordEmbedBuilder()
                .WithTitle("User Joined")
                .WithDescription($"**Name:** {args.Member.Mention}\n" +
                    $"**Created on:** {args.Member.CreationTimestamp:MMM dd, yyyy}\n" +
                    $"**Account age:** {accountAge.Days} days old\n" +
                    $"**Invite used:** {inviteUsed.Url} ({inviteUsed.Uses} uses)\n" +
                    $"**Created By:** {inviteUsed.Inviter}")
                .WithColor(accountAge < TimeSpan.FromDays(7) ? GrimoireColor.Orange : GrimoireColor.Green)
                .WithAuthor($"{args.Member.GetUsernameWithDiscriminator()} ({args.Member.Id})")
                .WithThumbnail(args.Member.GetGuildAvatarUrl(ImageFormat.Auto))
                .WithFooter($"Total Members: {args.Guild.MemberCount}")
                .WithTimestamp(DateTimeOffset.UtcNow);

            if (accountAge < TimeSpan.FromDays(7))
                embed.AddField("New Account", $"Created {accountAge.CustomTimeSpanString()}");
            await logChannel.SendMessageAsync(embed);
        }

        public async Task DiscordOnGuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs args)
        {
            var settings = await this._mediator.Send(new GetUserLogSettingsQuery{ GuildId = args.Guild.Id });
            if (!settings.IsLoggingEnabled) return;
            if (settings.LeaveChannelLog is null) return;
            var logChannel = args.Guild.Channels.GetValueOrDefault(settings.LeaveChannelLog.Value);
            if (logChannel is null) return;

            var accountAge = DateTimeOffset.UtcNow - args.Member.CreationTimestamp;
            var timeOnServer = DateTimeOffset.UtcNow - args.Member.JoinedAt;

            var embed = new DiscordEmbedBuilder()
                .WithTitle("User Left")
                .WithDescription($"**Name:** {args.Member.Mention}\n" +
                    $"**Created on:** {args.Member.CreationTimestamp:MMM dd, yyyy}\n" +
                    $"**Account age:** {accountAge.Days} days old\n" +
                    $"**Joined on:** {args.Member.JoinedAt:MMM dd, yyyy} ({timeOnServer.Days} days ago)")
                .WithColor(GrimoireColor.Purple)
                .WithAuthor($"{args.Member.GetUsernameWithDiscriminator()} ({args.Member.Id})")
                .WithThumbnail(args.Member.GetGuildAvatarUrl(ImageFormat.Auto))
                .WithFooter($"Total Members: {args.Guild.MemberCount}")
                .WithTimestamp(DateTimeOffset.UtcNow)
                .AddField($"Roles[{args.Member.Roles.Count()}]",
                args.Member.Roles.Any()
                ? string.Join(' ', args.Member.Roles.Where(x => x.Id != args.Guild.Id).Select(x => x.Mention))
                : "None");
            await logChannel.SendMessageAsync(embed);
        }
        public async Task DiscordOnGuildMemberUpdated(DiscordClient sender, GuildMemberUpdateEventArgs args)
        {
            var nicknameTask = this.ProcessNicknameChanges(args);
            var usernameTask = this.ProcessUsernameChanges(args);
            var avatarTask = this.ProcessAvatarChanges(args);

            await Task.WhenAll(nicknameTask, usernameTask, avatarTask);
        }

        private async Task ProcessNicknameChanges(GuildMemberUpdateEventArgs args)
        {
            var nicknameResponse = await this._mediator.Send(new UpdateNicknameCommand
            {
                GuildId = args.Guild.Id,
                UserId = args.Member.Id,
                Nickname = args.NicknameAfter
            });
            if (nicknameResponse is not null && nicknameResponse.BeforeNickname != nicknameResponse.AfterNickname)
            {
                var logChannel = args.Guild.Channels.GetValueOrDefault(nicknameResponse.NicknameChannelLogId);
                if (logChannel is not null)
                {
                    var embed = new DiscordEmbedBuilder()
                    .WithTitle("Nickname Updated")
                    .WithDescription($"**User:** <@!{args.Member.Id}>\n\n" +
                        $"**Before:** {(string.IsNullOrWhiteSpace(nicknameResponse.BeforeNickname)? "None" : nicknameResponse.BeforeNickname)}\n" +
                        $"**After:** {(string.IsNullOrWhiteSpace(nicknameResponse.AfterNickname)? "None" : nicknameResponse.AfterNickname)}")
                    .WithAuthor($"{args.Member.GetUsernameWithDiscriminator()} ({args.Member.Id})")
                    .WithThumbnail(args.Member.GetGuildAvatarUrl(ImageFormat.Auto))
                    .WithTimestamp(DateTimeOffset.UtcNow);
                    await logChannel.SendMessageAsync(embed);
                }
            }
        }

        private async Task ProcessUsernameChanges(GuildMemberUpdateEventArgs args)
        {
            var usernameResponse = await this._mediator.Send(new UpdateUsernameCommand
            {
                GuildId = args.Guild.Id,
                UserId = args.Member.Id,
                Username = args.MemberAfter.GetUsernameWithDiscriminator()
            });
            if (usernameResponse is not null && usernameResponse.BeforeUsername != usernameResponse.AfterUsername)
            {
                var logChannel = args.Guild.Channels.GetValueOrDefault(usernameResponse.UsernameChannelLogId);
                if (logChannel is not null)
                {
                    var embed = new DiscordEmbedBuilder()
                            .WithTitle("Username Updated")
                            .WithDescription($"**User:** <@!{args.MemberAfter.Id}>\n\n" +
                                $"**Before:** {usernameResponse.BeforeUsername}\n" +
                                $"**After:** {usernameResponse.AfterUsername}")
                            .WithAuthor($"{args.MemberAfter.GetUsernameWithDiscriminator()} ({args.MemberAfter.Id})")
                            .WithThumbnail(args.MemberAfter.GetAvatarUrl(ImageFormat.Auto))
                            .WithTimestamp(DateTimeOffset.UtcNow);
                    await logChannel.SendMessageAsync(embed);
                }
            }
        }

        private async Task ProcessAvatarChanges(GuildMemberUpdateEventArgs args)
        {
            var avatarResponse = await this._mediator.Send(new UpdateAvatarCommand
            {
                GuildId = args.Guild.Id,
                UserId = args.Member.Id,
                AvatarUrl = args.MemberAfter.GetGuildAvatarUrl(ImageFormat.Auto)
            });
            if (avatarResponse is not null && avatarResponse.BeforeAvatar != avatarResponse.AfterAvatar)
            {
                var logChannel = args.Guild.Channels.GetValueOrDefault(avatarResponse.AvatarChannelLogId);
                if (logChannel is not null)
                {
                    var url = args.Member.GetAvatarUrl(ImageFormat.Auto, 128);
                    var afterStream = await this._httpClient.GetStreamAsync(url);
                    var afterFileName = $"attachment0.{args.Member.GetAvatarUrl(ImageFormat.Auto).Split('.')[^1].Split('?')[0]}";
                    Stream? beforeStream = null;
                    var beforeFileName = $"attachment1.{avatarResponse.BeforeAvatar.Split('.')[^1].Split('?')[0]}";
                    try
                    {
                        beforeStream = await this._httpClient.GetStreamAsync(avatarResponse.BeforeAvatar);
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        _clientService.Client.Logger.LogInformation("Was not able to retrieve the old avatar image.");
                    }
                    var embed = new DiscordEmbedBuilder()
                    .WithTitle("Avatar Updated")
                    .WithDescription($"**User:** <@!{args.Member.Id}>\n\n" +
                        $"New Avatar is first image. Old avatar is second image.")
                    .WithAuthor($"{args.Member.GetUsernameWithDiscriminator()} ({args.Member.Id})")
                    .WithThumbnail(avatarResponse.BeforeAvatar)
                    .WithTimestamp(DateTimeOffset.UtcNow)
                    .WithImageUrl($"attachment://{afterFileName}");

                    var message = new DiscordMessageBuilder()
                        .AddEmbed(embed)
                        .AddFile(afterFileName, afterStream);
                    if (beforeStream is not null)
                    {
                        message.AddEmbed(new DiscordEmbedBuilder()
                            .WithTitle("Avatar Updated")
                            .WithDescription($"**User:** <@!{args.Member.Id}>\n\n" +
                                $"New Avatar is first image. Old avatar is second.")
                            .WithAuthor($"{args.Member.GetUsernameWithDiscriminator()} ({args.Member.Id})")
                            .WithThumbnail(avatarResponse.BeforeAvatar)
                            .WithTimestamp(DateTimeOffset.UtcNow)
                            .WithImageUrl($"attachment://{beforeFileName}"));
                        message.AddFile(beforeFileName, beforeStream);
                    }
                    await logChannel.SendMessageAsync(message);
                }
            }
        }
    }
}
