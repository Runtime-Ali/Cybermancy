// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

namespace Grimoire.Core.Features.Moderation.Commands.BanComands.PublishBan
{
    public class PublishBanCommandHandler : ICommandHandler<PublishBanCommand>
    {
        private readonly IGrimoireDbContext _grimoireDbContext;

        public PublishBanCommandHandler(IGrimoireDbContext grimoireDbContext)
        {
            this._grimoireDbContext = grimoireDbContext;
        }

        public async ValueTask<Unit> Handle(PublishBanCommand command, CancellationToken cancellationToken)
        {
            await _grimoireDbContext.PublishedMessages.AddAsync(new PublishedMessage
            {
                MessageId = command.MessageId,
                SinId = command.SinId,
                PublishType = command.PublishType,
            }, cancellationToken);
            await _grimoireDbContext.SaveChangesAsync(cancellationToken);

            return new Unit();
        }
    }
}