// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Grimoire.Core.Features.Leveling.Commands.ManageXpCommands.UpdateIgnoreStateForXpGain;
using Grimoire.Core.Features.Shared.SharedDtos;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Grimoire.Core.Test.Unit.Features.Leveling.Commands.ManageXpCommands.UpdateIgnoreStateForXpGain;

[TestFixture]
public class UpdateIgnoreStateForXpGainCommandHandlerTests
{

    [Test]
    public async Task WhenAddIgnoreForXpGainCommandHandlerCalled_AddIgnoreStatusAsync()
    {
        var databaseFixture = new TestDatabaseFixture();
        using var context = databaseFixture.CreateContext();
        //context.Database.BeginTransaction();
        var cut = new AddIgnoreForXpGainCommandHandler(context);

        var result = await cut.Handle(
            new AddIgnoreForXpGainCommand
            {
                Users = new [] { new UserDto { Id = TestDatabaseFixture.Member1.UserId } },
                GuildId = TestDatabaseFixture.Member1.GuildId,
                Channels = new []
                {
                    new ChannelDto
                    {
                        Id = TestDatabaseFixture.Channel1.Id,
                        GuildId = TestDatabaseFixture.Channel1.GuildId
                    }
                },
                Roles = new []
                {
                    new RoleDto
                    {
                        Id = TestDatabaseFixture.Role1.Id,
                        GuildId = TestDatabaseFixture.Role1.GuildId
                    }
                }
            }, default);
        context.ChangeTracker.Clear();
        result.Message.Should().Be("<@!4> <@&6> <#3>  are now ignored for xp gain.");

        var member = await context.Members.Where(x =>
            x.UserId == TestDatabaseFixture.Member1.UserId
            && x.GuildId == TestDatabaseFixture.Member1.GuildId
            ).FirstAsync();

        member.IsIgnoredMember.Should().NotBeNull();

        var role = await context.Roles.Where(x =>
            x.Id == TestDatabaseFixture.Role1.Id
            && x.GuildId == TestDatabaseFixture.Role1.GuildId
            ).FirstAsync();

        role.IsIgnoredRole.Should().NotBeNull();

        var channel = await context.Channels.Where(x =>
            x.Id == TestDatabaseFixture.Channel1.Id
            && x.GuildId == TestDatabaseFixture.Channel1.GuildId
            ).FirstAsync();

        channel.IsIgnoredChannel.Should().NotBeNull();
    }

    [Test]
    public async Task WhenUpdateIgnoreStateForXpGainCommandHandlerCalled_AndThereAreInvalidAndMissingIds_UpdateMessageAsync()
    {
        var databaseFixture = new TestDatabaseFixture();
        using var context = databaseFixture.CreateContext();
        context.Database.BeginTransaction();
        var cut = new AddIgnoreForXpGainCommandHandler(context);

        var result = await cut.Handle(
            new AddIgnoreForXpGainCommand
            {
                GuildId = TestDatabaseFixture.Member1.GuildId,
                InvalidIds = new [] { "asldfkja" }
            }, default);
        context.ChangeTracker.Clear();
        result.Message.Should().Be("Could not match asldfkja with a role, channel or user. ");
    }
}
