// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

namespace Grimoire.Core.Features.Moderation.Queries.GetMuteRole
{
    public sealed record GetMuteRoleQueryResponse : BaseResponse
    {
        public ulong RoleId { get; init; }
    }
}