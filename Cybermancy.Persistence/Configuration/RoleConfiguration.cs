﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cybermancy.Domain;
using System.Diagnostics.CodeAnalysis;

namespace Cybermancy.Persistance.Configuration
{
    [ExcludeFromCodeCoverage]
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasKey(e => e.Id);
            builder.HasOne(e => e.Guild).WithMany(e => e.Roles);
            builder.Property(e => e.IsXpIgnored)
                .HasDefaultValue(false);
            builder.HasOne(e => e.Reward).WithOne(e => e.Role)
                .IsRequired(false);
        }
    }
}
