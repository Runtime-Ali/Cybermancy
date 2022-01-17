// This file is part of the Cybermancy Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using Cybermancy.Domain.Shared;

namespace Cybermancy.Domain
{
    public class Message : Identifiable
    {
        public ulong UserId { get; set; }

        public virtual User User { get; set; } = null!;

        public virtual ulong ChannelId { get; set; }

        public virtual Channel Channel { get; set; } = null!;

        public ulong GuildId { get; set; }

        public virtual Guild Guild { get; set; } = null!;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}
