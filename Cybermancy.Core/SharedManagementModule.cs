﻿// -----------------------------------------------------------------------
// <copyright file="SharedManagementModule.cs" company="Netharia">
// Copyright (c) Netharia. All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Cybermancy.Core
{
    using System.Threading.Tasks;
    using Cybermancy.Core.Contracts.Services;
    using DSharpPlus;
    using DSharpPlus.EventArgs;
    using Nefarius.DSharpPlus.Extensions.Hosting.Attributes;
    using Nefarius.DSharpPlus.Extensions.Hosting.Events;

    [DiscordWebSocketEventSubscriber]
    public class SharedManagementModule : IDiscordWebSocketEventSubscriber
    {
        private readonly IGuildService guildService;
        private readonly IRoleService roleService;
        private readonly IChannelService channelService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedManagementModule"/> class.
        /// </summary>
        /// <param name="guildService"></param>
        /// <param name="roleService"></param>
        /// <param name="channelService"></param>
        public SharedManagementModule(IGuildService guildService, IRoleService roleService, IChannelService channelService)
        {
            this.guildService = guildService;
            this.roleService = roleService;
            this.channelService = channelService;
        }

        public async Task DiscordOnReady(DiscordClient sender, ReadyEventArgs args)
        {
            await Task.Run(() => this.guildService.SetupAllGuildAsync(sender.Guilds.Values));
            await Task.Run(() => this.roleService.SetupAllRolesAsync(sender.Guilds.Values));
            await Task.Run(() => this.channelService.SetupAllChannelsAsync(sender.Guilds.Values));
        }

        #region UnusedEvents

        public Task DiscordOnHeartbeated(DiscordClient sender, HeartbeatEventArgs args)
        {
            return Task.CompletedTask;
        }

        public Task DiscordOnResumed(DiscordClient sender, ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }

        public Task DiscordOnSocketClosed(DiscordClient sender, SocketCloseEventArgs args)
        {
            return Task.CompletedTask;
        }

        public Task DiscordOnSocketOpened(DiscordClient sender, SocketEventArgs args)
        {
            return Task.CompletedTask;
        }

        public Task DiscordOnZombied(DiscordClient sender, ZombiedEventArgs args)
        {
            return Task.CompletedTask;
        }

        public Task DiscordOSocketErrored(DiscordClient sender, SocketErrorEventArgs args)
        {
            return Task.CompletedTask;
        }

        #endregion
    }
}