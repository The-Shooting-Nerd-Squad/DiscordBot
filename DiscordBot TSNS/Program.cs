using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot_TSNS.Database;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot_TSNS
{
    class Program
    {
        private readonly Dictionary<IEmote, string> _emojiMap;
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _config;

        static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public Program()
        {
            _emojiMap = new Dictionary<IEmote, string>();
            _emojiMap.Add(Emote.Parse("<:ow:666420467904675840>"), "Overwatch");
            _emojiMap.Add(Emote.Parse("<:csgo:666420991379111936>"), "CSGO");
            _emojiMap.Add(Emote.Parse("<:mc:666423599803924490>"), "Minecraft");
            _emojiMap.Add(Emote.Parse("<:r6s:666421078079569959>"), "Rainbow 6 Siege");
            _emojiMap.Add(Emote.Parse("<:lol:666420999436107791>"), "League of Legends");
            _emojiMap.Add(Emote.Parse("<:gta:666423818633347075>"), "GTA");
            _emojiMap.Add(Emote.Parse("<:bs:666547875517431858>"), "Beat Saber");
            _emojiMap.Add(Emote.Parse("<:bf:666547836866789377>"), "Battlefield");
            //_emojiMap.Add(Emote.Parse("<:pubg:>"), "PUBG");
            //_emojiMap.Add(new Emoji("\U0001f495"), "Dark");
            _client = new DiscordSocketClient();

            _client.Log += Log;

            _client.MessageReceived += MessageReceivedAsync;
            _client.ReactionAdded += ReactionAdded;
            _client.ReactionRemoved += ReactionRemoved;

            var _builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(path: "config.json");
            _config = _builder.Build();
        }

        public async Task MainAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _config["Token"]);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private Task Log(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.Id != 142722503159185408)
                return;
            string command = "tsns.sendroleselect";
            string content = message.Content;
            if (content.ToLower().StartsWith(command))
            {
                await message.DeleteAsync();
                using (var db = new DbEntities()) {
                    SocketGuild guild = ((SocketGuildChannel)message.Channel).Guild;
                    RoleSelectMessage lastRoleSelectMessageDb = db.RoleSelectMessages.FirstOrDefault(x => x.GuildId == guild.Id);
                    if(lastRoleSelectMessageDb != null)
                    {
                        if(guild.GetTextChannel(lastRoleSelectMessageDb.ChannelId) != null)
                        {
                            IMessage lastRoleSelectMessage = await guild.GetTextChannel(lastRoleSelectMessageDb.ChannelId).GetMessageAsync(lastRoleSelectMessageDb.MessageId);
                            if(lastRoleSelectMessage != null)
                                await lastRoleSelectMessage.DeleteAsync();
                        }
                       
                        db.RoleSelectMessages.Remove(lastRoleSelectMessageDb);
                    }
                    RestUserMessage roleSelectMessage = await message.Channel.SendMessageAsync(content.Substring(command.Length + 1, content.Length - (command.Length + 1)));
                    db.RoleSelectMessages.Add(new RoleSelectMessage(guild.Id, roleSelectMessage.Channel.Id, roleSelectMessage.Id));
                    _ = roleSelectMessage.AddReactionsAsync(_emojiMap.Keys.ToArray());
                    await db.SaveChangesAsync();
                }
            }
        }

        private Task ReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId == _client.CurrentUser.Id)
                return Task.CompletedTask;
            using (var db = new DbEntities())
            {
                if (db.RoleSelectMessages.FirstOrDefault(x => x.ChannelId == reaction.Channel.Id && x.MessageId == reaction.MessageId) == null)
                    return Task.CompletedTask;
                string roleName = _emojiMap.ToList().FirstOrDefault(x => x.Key.Name.Equals(reaction.Emote.Name)).Value;
                if(roleName != null)
                {
                    SocketGuild guild = ((SocketGuildChannel)reaction.Channel).Guild;
                    SocketRole role = guild.Roles.FirstOrDefault(x => x.Name.Equals(roleName));
                    if(role != null)
                    {
                        IGuildUser user;
                        if (reaction.User.IsSpecified)
                        {
                            user = reaction.User.Value as IGuildUser;
                        }
                        else
                        {
                            user = guild.GetUser(reaction.UserId);
                        }
                        _ = user.AddRoleAsync(role);
                    }
                }
            }
            return Task.CompletedTask;
        }
        private Task ReactionRemoved(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId == _client.CurrentUser.Id)
                return Task.CompletedTask;
            using (var db = new DbEntities())
            {
                if (db.RoleSelectMessages.FirstOrDefault(x => x.ChannelId == reaction.Channel.Id && x.MessageId == reaction.MessageId) == null)
                    return Task.CompletedTask;
                string roleName = _emojiMap.ToList().FirstOrDefault(x => x.Key.Name.Equals(reaction.Emote.Name)).Value;
                if (roleName != null)
                {
                    SocketGuild guild = ((SocketGuildChannel)reaction.Channel).Guild;
                    SocketRole role = guild.Roles.FirstOrDefault(x => x.Name.Equals(roleName));
                    if (role != null)
                    {
                        IGuildUser user;
                        if (reaction.User.IsSpecified)
                        {
                            user = reaction.User.Value as IGuildUser;
                        }
                        else
                        {
                            user = guild.GetUser(reaction.UserId);
                        }
                        _ = user.RemoveRoleAsync(role);
                    }
                }
            }
            return Task.CompletedTask;
        }

    }
}
