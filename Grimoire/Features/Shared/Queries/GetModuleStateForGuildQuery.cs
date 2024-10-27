// This file is part of the Grimoire Project.
//
// Copyright (c) Netharia 2021-Present.
//
// All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.

using Grimoire.DatabaseQueryHelpers;

namespace Grimoire.Features.Shared.Queries;

public sealed record GetModuleStateForGuildQuery : IRequest<bool>
{
    public ulong GuildId { get; init; }
    public Module Module { get; init; }
}

public sealed class GetModuleStateForGuildQueryHandler(GrimoireDbContext grimoireDbContext)
    : IRequestHandler<GetModuleStateForGuildQuery, bool>
{
    private readonly GrimoireDbContext _grimoireDbContext = grimoireDbContext;

    public async Task<bool> Handle(GetModuleStateForGuildQuery request, CancellationToken cancellationToken)
    {
        var result = await this._grimoireDbContext.Guilds.AsNoTracking().WhereIdIs(request.GuildId)
            .GetModulesOfType(request.Module)
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            .Select(module => module != null && module.ModuleEnabled)
            .FirstOrDefaultAsync(cancellationToken);
        return result;
    }
}
