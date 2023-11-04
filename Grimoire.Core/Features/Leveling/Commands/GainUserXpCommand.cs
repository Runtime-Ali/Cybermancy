// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using Grimoire.Core.DatabaseQueryHelpers;
using Grimoire.Core.Extensions;

namespace Grimoire.Core.Features.Leveling.Commands.ManageXpCommands.GainUserXp;

public sealed record GainUserXpCommand : ICommand<GainUserXpCommandResponse>
{
    public ulong GuildId { get; init; }
    public ulong UserId { get; init; }
    public ulong ChannelId { get; init; }
    public ulong[] RoleIds { get; init; } = Array.Empty<ulong>();
}

public class GainUserXpCommandHandler : ICommandHandler<GainUserXpCommand, GainUserXpCommandResponse>
{
    private readonly IGrimoireDbContext _grimoireDbContext;

    public GainUserXpCommandHandler(IGrimoireDbContext grimoireDbContext)
    {
        this._grimoireDbContext = grimoireDbContext;
    }

    public async ValueTask<GainUserXpCommandResponse> Handle(GainUserXpCommand command, CancellationToken cancellationToken)
    {
        var result = await this._grimoireDbContext.Members
            .AsNoTracking()
            .WhereLevelingEnabled()
            .WhereMemberHasId(command.UserId, command.GuildId)
            .WhereMemberNotIgnored(command.ChannelId, command.RoleIds)
            .Select(x => new
            {
                Xp = x.XpHistory.Sum(x => x.Xp),
                Timeout = x.XpHistory.Select(x => x.TimeOut)
                    .OrderByDescending(x => x).First(),
                x.Guild.LevelSettings.Base,
                x.Guild.LevelSettings.Modifier,
                x.Guild.LevelSettings.Amount,
                x.Guild.LevelSettings.LevelChannelLogId,
                x.Guild.LevelSettings.TextTime,
                x.Guild.ModChannelLog,
                Rewards = x.Guild.Rewards.Select(reward => new { reward.RoleId, reward.RewardLevel, reward.RewardMessage })
            }).FirstOrDefaultAsync(cancellationToken);

        if (result is null || result.Timeout > DateTime.UtcNow)

            return new GainUserXpCommandResponse { Success = false };

        var previousLevel = MemberExtensions.GetLevel(result.Xp, result.Base, result.Modifier);
        var currentLevel = MemberExtensions.GetLevel(result.Xp + result.Amount, result.Base, result.Modifier);

        await this._grimoireDbContext.XpHistory.AddAsync(new XpHistory
        {
            Xp = result.Amount,
            UserId = command.UserId,
            GuildId = command.GuildId,
            TimeOut = DateTimeOffset.UtcNow + result.TextTime,
            Type = XpHistoryType.Earned
        }, cancellationToken);
        await this._grimoireDbContext.SaveChangesAsync(cancellationToken);

        var earnedRewards = result.Rewards
            .Where(x => x.RewardLevel <= currentLevel)
            .Select(x => new RewardDto
            {
                RoleId = x.RoleId,
                Message = x.RewardMessage
            } )
            .ToArray();

        return new GainUserXpCommandResponse
        {
            Success = true,
            EarnedRewards = earnedRewards,
            PreviousLevel = previousLevel,
            CurrentLevel = currentLevel,
            LevelLogChannel = result.LevelChannelLogId,
            LogChannelId = result.ModChannelLog,
        };

    }
}

public sealed record GainUserXpCommandResponse : BaseResponse
{
    public RewardDto[] EarnedRewards { get; init; } = Array.Empty<RewardDto>();
    public int PreviousLevel { get; init; }
    public int CurrentLevel { get; init; }
    public ulong? LevelLogChannel { get; init; }
    public bool Success { get; init; } = false;
}

public sealed record RewardDto
{
    public ulong RoleId { get; init; }
    public string? Message { get; init; }
}