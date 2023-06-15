using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordTradeBot
{
    public class GuildsTableRow
    {
        public SocketGuild Guild { get; set; }
        public SocketTextChannel Channel { get; set; }
        public string Role { get; set; }
    }
}
