// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using Grimoire.Domain.Shared;

namespace Grimoire.Core.Extensions;

public static class IMentionableExtensions
{
    public static string Mention<TSource>(this TSource? ignorable) where TSource : IMentionable
        =>
        ignorable switch
        {
            Member member => $"<@!{member.UserId}>",
            User user => $"<@!{user.Id}>",
            Role role => $"<@&{role.Id}>",
            Reward reward => $"<@&{reward.RoleId}>",
            Channel channel => $"<#{channel.Id}>",
            null => "Unknown User",
            _ => throw new NotImplementedException(),
        };
}