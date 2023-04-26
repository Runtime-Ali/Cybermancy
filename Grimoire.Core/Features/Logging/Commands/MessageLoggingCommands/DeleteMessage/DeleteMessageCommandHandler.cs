// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using Grimoire.Core.DatabaseQueryHelpers;

namespace Grimoire.Core.Features.Logging.Commands.MessageLoggingCommands.DeleteMessage
{
    public class DeleteMessageCommandHandler : ICommandHandler<DeleteMessageCommand, DeleteMessageCommandResponse>
    {
        private readonly IGrimoireDbContext _grimoireDbContext;

        public DeleteMessageCommandHandler(IGrimoireDbContext grimoireDbContext)
        {
            this._grimoireDbContext = grimoireDbContext;
        }

        public async ValueTask<DeleteMessageCommandResponse> Handle(DeleteMessageCommand command, CancellationToken cancellationToken)
        {
            var message = await this._grimoireDbContext.Messages
                .WhereIdIs(command.Id)
                .WhereMessageLoggingIsEnabled()
                .Select(x => new DeleteMessageCommandResponse
                {
                    LoggingChannel = x.Guild.MessageLogSettings.DeleteChannelLogId,
                    UserId = x.UserId,
                    MessageContent = x.MessageHistory
                        .OrderByDescending(x => x.TimeStamp)
                        .First(y => y.Action != MessageAction.Deleted)
                        .MessageContent,
                    ReferencedMessage = x.ReferencedMessageId,
                    Attachments = x.Attachments
                        .Select(x => new AttachmentDto
                        {
                            Id = x.Id,
                            FileName = x.FileName,
                        })
                        .ToArray(),
                    Success = true

                }).FirstOrDefaultAsync(cancellationToken: cancellationToken);
            if (message is null)
                return new DeleteMessageCommandResponse { Success = false };
            await this._grimoireDbContext.MessageHistory.AddAsync(new MessageHistory
            {
                MessageId = command.Id,
                Action = MessageAction.Deleted,
                GuildId = command.GuildId,
                DeletedByModeratorId = command.DeletedByModerator
            }, cancellationToken);
            await this._grimoireDbContext.SaveChangesAsync(cancellationToken);
            return message;
        }
    }
}
