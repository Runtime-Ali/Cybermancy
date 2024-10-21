// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using Grimoire.DatabaseQueryHelpers;

namespace Grimoire.Features.Logging.Trackers.Events;
public class TrackerMessageUpdateEvent
{
    public class EventHandler(IMediator mediator) : IEventHandler<MessageUpdatedEventArgs>
    {
        private readonly IMediator _mediator = mediator;

        public async Task HandleEventAsync(DiscordClient sender, MessageUpdatedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.Message.Content))
                return;

            var response = await this._mediator.Send(
            new Request
            {
                UserId = args.Author.Id,
                GuildId = args.Guild.Id,
                MessageId = args.Message.Id
            });

            if (response is null)
                return;

            var embed = new DiscordEmbedBuilder()
            .AddField("User", args.Author.Mention, true)
            .AddField("Channel", args.Channel.Mention, true)
            .AddField("Link", $"**[Jump URL]({args.Message.JumpLink})**", true)
            .WithFooter("Message Sent", args.Author.GetAvatarUrl(ImageFormat.Auto))
            .WithTimestamp(DateTime.UtcNow)
            .AddMessageTextToFields("Before", response.OldMessageContent)
            .AddMessageTextToFields("After", args.Message.Content);

            await sender.SendMessageToLoggingChannel(response.TrackerChannelId, embed);
        }
    }

    public sealed record Request : IRequest<Response?>
    {
        public ulong UserId { get; init; }
        public ulong GuildId { get; init; }
        public ulong MessageId { get; init; }
    }

    public sealed class Handler(GrimoireDbContext grimoireDbContext) : IRequestHandler<Request, Response?>
    {
        private readonly GrimoireDbContext _grimoireDbContext = grimoireDbContext;

        public async Task<Response?> Handle(Request request, CancellationToken cancellationToken)
            => await this._grimoireDbContext.Trackers
            .AsNoTracking()
            .WhereMemberHasId(request.UserId, request.GuildId)
            .Select(x => new Response
            {
                TrackerChannelId = x.LogChannelId,
                OldMessageContent = x.Member.Messages
                    .Where(x => x.Id == request.MessageId)
                    .Select(x => x.MessageHistory
                        .Where(x => x.Action != MessageAction.Deleted
                            && x.TimeStamp < DateTime.UtcNow.AddSeconds(-1))
                        .OrderByDescending(x => x.TimeStamp)
                        .First().MessageContent)
                    .First()
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public sealed record Response : BaseResponse
    {
        public ulong TrackerChannelId { get; init; }
        public string OldMessageContent { get; init; } = string.Empty;
    }
}
