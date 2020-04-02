using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DiscordBot_TSNS.Database
{
    public partial class RoleSelectMessage
    {
        [Key]
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public RoleSelectMessage(ulong guildId, ulong channelId, ulong messageId)
        {
            GuildId = guildId;
            ChannelId = channelId;
            MessageId = messageId;
        }
    }
}
