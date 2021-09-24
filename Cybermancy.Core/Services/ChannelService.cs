// -----------------------------------------------------------------------
// <copyright file="ChannelService.cs" company="Netharia">
// Copyright (c) Netharia. All rights reserved.
// Licensed under the AGPL-3.0 license. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Cybermancy.Core.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Cybermancy.Core.Contracts.Persistence;
    using Cybermancy.Core.Contracts.Services;
    using Cybermancy.Domain;
    using DSharpPlus.Entities;

    public class ChannelService : IChannelService
    {
        private readonly IAsyncIdRepository<Channel> channelRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelService"/> class.
        /// </summary>
        /// <param name="channelRepository"></param>
        public ChannelService(IAsyncIdRepository<Channel> channelRepository)
        {
            this.channelRepository = channelRepository;
        }

        public Task<ICollection<Channel>> GetAllIgnoredChannelsAsync(ulong guildId)
        {
            throw new System.NotImplementedException();
        }

        public ValueTask<Channel> GetChannelAsync(ulong channelId)
        {
            return this.channelRepository.GetByIdAsync(channelId);
        }

        public async Task<Channel> GetChannelAsync(DiscordChannel discordChannel)
        {
            if (await this.channelRepository.ExistsAsync(discordChannel.Id))
            {
                return await this.channelRepository.GetByIdAsync(discordChannel.Id);
            }
            else
            {
                var newChannel = new Channel()
                {
                    Id = discordChannel.Id,
                    GuildId = discordChannel.GuildId.Value,
                    Name = discordChannel.Name,
                };
                await this.SaveAsync(newChannel);
            }

            return await this.channelRepository.GetByIdAsync(discordChannel.Id);
        }

        public async Task<bool> IsChannelIgnoredAsync(DiscordChannel discordChannel)
        {
            if (await this.channelRepository.ExistsAsync(discordChannel.Id))
            {
                return (await this.channelRepository.GetByIdAsync(discordChannel.Id)).IsXpIgnored;
            }
            else
            {
                var newChannel = new Channel()
                {
                    Id = discordChannel.Id,
                    GuildId = discordChannel.GuildId.Value,
                    Name = discordChannel.Name,
                };
                await this.SaveAsync(newChannel);
            }

            return (await this.channelRepository.GetByIdAsync(discordChannel.Id)).IsXpIgnored;
        }

        public async Task<Channel> SaveAsync(Channel channel)
        {
            if (await this.channelRepository.ExistsAsync(channel.Id))
                return await this.channelRepository.UpdateAsync(channel);
            return await this.channelRepository.AddAsync(channel);
        }

        public Task SetupAllChannelsAsync(IEnumerable<DiscordGuild> guilds)
        {
            var newChannels = new List<Channel>();
            foreach (var guild in guilds)
            {
                foreach (var channel in guild.Channels.Values.Where(x => this.channelRepository.ExistsAsync(x.Id).Result))
                {
                    newChannels.Add(new Channel()
                    {
                        Id = channel.Id,
                        GuildId = channel.GuildId.Value,
                        Name = channel.Name,
                    });
                }
            }

            return this.channelRepository.AddMultipleAsync(newChannels);
        }
    }
}