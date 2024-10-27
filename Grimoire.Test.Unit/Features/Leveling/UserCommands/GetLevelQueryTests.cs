// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Grimoire.Domain;
using Grimoire.Exceptions;
using Grimoire.Features.Leveling.UserCommands;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Grimoire.Test.Unit.Features.Leveling.UserCommands;

[Collection("Test collection")]
public sealed class GetLevelQueryTests(GrimoireCoreFactory factory) : IAsyncLifetime
{
    private const ulong GuildId = 1;
    private const ulong UserId = 1;
    private const ulong RoleId = 1;
    private const int RewardLevel = 100;

    private readonly GrimoireDbContext _dbContext = new(
        new DbContextOptionsBuilder<GrimoireDbContext>()
            .UseNpgsql(factory.ConnectionString)
            .Options);

    private readonly Func<Task> _resetDatabase = factory.ResetDatabase;

    public async Task InitializeAsync()
    {
        await this._dbContext.AddAsync(new Guild { Id = GuildId, LevelSettings = new GuildLevelSettings() });
        await this._dbContext.AddAsync(new User { Id = UserId });
        await this._dbContext.AddAsync(new Member { UserId = UserId, GuildId = GuildId });
        await this._dbContext.AddAsync(new XpHistory
        {
            UserId = UserId,
            GuildId = GuildId,
            Xp = 300,
            Type = XpHistoryType.Awarded,
            TimeOut = DateTime.UtcNow.AddMinutes(-5)
        });
        await this._dbContext.AddAsync(new Role { Id = RoleId, GuildId = GuildId });
        await this._dbContext.AddAsync(new Reward { RoleId = RoleId, GuildId = GuildId, RewardLevel = RewardLevel });

        await this._dbContext.SaveChangesAsync();
    }

    public Task DisposeAsync() => this._resetDatabase();

    [Fact]
    public async Task WhenCallingGetLevelQueryHandler_IfUserDoesNotExist_ReturnFailedResponse()
    {
        var cut = new GetLevel.Handler(this._dbContext);
        var command = new GetLevel.Query { GuildId = GuildId, UserId = 234081234 };

        var response = await Assert.ThrowsAsync<AnticipatedException>(async () => await cut.Handle(command, default));

        response.Should().NotBeNull();
        response?.Message.Should().Be("That user could not be found.");
    }

    [Fact]
    public async Task WhenCallingGetLevelQueryHandler_IfUserExists_ReturnResponseAsync()
    {
        var cut = new GetLevel.Handler(this._dbContext);
        var command = new GetLevel.Query { GuildId = GuildId, UserId = UserId };

        var response = await cut.Handle(command, default);

        response.UsersXp.Should().Be(300);
        response.UsersLevel.Should().Be(8);
        response.LevelProgress.Should().Be(15);
        response.XpForNextLevel.Should().Be(94);
        response.NextRoleRewardId.Should().Be(RoleId);
        response.NextRewardLevel.Should().Be(RewardLevel);
    }
}
