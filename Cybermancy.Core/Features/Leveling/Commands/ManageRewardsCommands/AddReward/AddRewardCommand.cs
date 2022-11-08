// This file is part of the Cybermancy Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using Cybermancy.Core.Responses;
using Mediator;

namespace Cybermancy.Core.Features.Leveling.Commands.ManageRewardsCommands.AddReward
{
    public sealed record AddRewardCommand : ICommand<BaseResponse>
    {
        public ulong RoleId { get; init; }
        public ulong GuildId { get; init; }
        public int RewardLevel { get; init; }
    }
}
