// This file is part of the Cybermancy Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cybermancy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cybermancy.Core.Configuration
{
    [ExcludeFromCodeCoverage]
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever().IsRequired();
            builder.Property(e => e.Content).HasMaxLength(4000);
            builder.HasOne(e => e.User).WithMany(e => e.Messages)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            builder.HasOne(e => e.Channel).WithMany(e => e.Messages)
                .HasForeignKey(e => e.ChannelId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            builder.HasOne(e => e.Guild).WithMany(e => e.Messages)
                .HasForeignKey(e => e.GuildId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasMany(e => e.Attachments).WithOne(e => e.Message);
        }
    }
}
