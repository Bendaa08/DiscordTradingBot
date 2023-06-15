using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using System.Data.SQLite;
using System.Data;
using System.Xml.Linq;
using Discord.Commands;
using System.Threading.Channels;

namespace DiscordTradeBot.Modules
{
    public class SlashCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public static string sqliteConnectionString { get; set; } = "Data Source=GuildsDatabase.db";

        SQLiteConnection sqliteConnection = new SQLiteConnection(sqliteConnectionString);


        [SlashCommand("trade-send", "Sends a trade message.")]
        public async Task HandleTradeSend(string ticker, string entry, string pt, string sl, string comment)
        {
            List<GuildsTableRow> guildsTableRows = new() { };

            sqliteConnection.Open();

            string selectQuery = @"SELECT * FROM GuildsTable";
            SQLiteCommand command = new(selectQuery, sqliteConnection);
            var reader = command.ExecuteReader();

            while(reader.Read())
            {
                try
                {
                    if (reader.GetValue(1) == null || reader.GetValue(1) == DBNull.Value) { continue; }

                    SocketGuild guild = Context.Client.Guilds.First(_guild => _guild.Id == Convert.ToUInt64(reader.GetValue(0)));
                    GuildsTableRow guildsTableRow = new()
                    {
                        Guild = guild,
                        Channel = guild.GetTextChannel(Convert.ToUInt64(reader.GetValue(1))),
                        Role = ""

                    };

                    if (reader.GetValue(2) != null && reader.GetValue(2) != DBNull.Value)
                    {
                        guildsTableRow.Role = $"<@&{reader.GetValue(2)}>";
                    }

                    guildsTableRows.Add(guildsTableRow);
                } catch(InvalidOperationException invalideOperationException)
                {
                    // remove guild from database if it doesn't exist anymore

                    string deleteQuery = $@"DELETE FROM GuildsTable WHERE guildId={reader.GetValue(0)}";
                    SQLiteCommand deleteCommand = new(deleteQuery, sqliteConnection);
                    deleteCommand.ExecuteNonQuery();

                }
            }

            sqliteConnection.Close();

            Embed embed = new EmbedBuilder()
                .AddField(new EmbedFieldBuilder()
                {
                    Name = ":rotating_light: New Trade :rotating_light:",
                    Value = $":ticket: TICKER: {ticker}\n:dart: ENTRY: {entry}\n:moneybag: PT: {pt}\n:x: SL: {sl}\n:pencil: COMMENT: {comment}"
                })
                .WithCurrentTimestamp()
                .Build();
            

            foreach(GuildsTableRow _guild in guildsTableRows)
            {
                await _guild.Channel.SendMessageAsync(_guild.Role ,embed: embed);
            }

            await RespondAsync($"Successfully sent trade messages to {guildsTableRows.Count} servers.", ephemeral: true);
        }



        [SlashCommand("set-channel", "Sets the channel where trade messages will be posted to for a specific server")]
        public async Task HandleSetChannel(string guildId, string channelId)
        {
            try
            {
                bool channelExists = false;
                foreach (SocketGuildChannel channel in Context.Client.GetGuild(Convert.ToUInt64(guildId)).Channels)
                {
                    if (channel.Id == Convert.ToUInt64(channelId)) { channelExists = true; break; }
                }

                if (!channelExists)
                {
                    await RespondAsync("Channel ID was invalid, please make sure you typed it in correctly.", ephemeral: true);
                    return;
                }
            } catch (Exception ex)
            {
                await RespondAsync("Either Guild ID or Channel ID was invalid, please make sure you typed it in correctly.", ephemeral: true);
                return;
            }



            sqliteConnection.Open();

            string updateQuery = $@"UPDATE GuildsTable SET channelId={channelId} WHERE guildId={guildId}";
            SQLiteCommand command = new(updateQuery, sqliteConnection);
            command.ExecuteNonQuery();
            
            sqliteConnection.Close();
            await RespondAsync("Sucess", ephemeral: true);
        }



        [SlashCommand("remove-channel", "Trade messages won't be sent to this channel once it is removed.")]
        public async Task HandleRemoveChannel(string guildId)
        {
            try
            {
                bool guildExists = false;
                foreach(SocketGuild _guild in Context.Client.Guilds)
                {
                    if(_guild.Id == Convert.ToUInt64(guildId)) { guildExists = true; break; }
                }

                if(!guildExists)
                {
                    await RespondAsync("Please make sure you entered the Guild ID correctly", ephemeral: true);
                    return;
                }

                sqliteConnection.Open();

                string updateQuery = $@"UPDATE GuildsTable SET channelId=null WHERE guildId={guildId}";
                SQLiteCommand command = new(updateQuery, sqliteConnection);
                command.ExecuteNonQuery();

                sqliteConnection.Close();
                await RespondAsync(":white_check_mark:", ephemeral: true);
            }
            catch (Exception e)
            {
                await RespondAsync("Please make sure you entered the Guild ID correctly", ephemeral: true);
            }
        }



        [SlashCommand("set-role", "Sets the role that will be mentioned during trade messages.")]
        public async Task HandleSetRole(string guildId, string roleId)
        {
            try
            {
                bool roleExists = false;
                foreach (SocketRole role in Context.Client.GetGuild(Convert.ToUInt64(guildId)).Roles)
                {
                    if (role.Id == Convert.ToUInt64(roleId)) { roleExists = true; break; }
                }

                if (!roleExists)
                {
                    await RespondAsync("Role ID was invalid, please make sure you typed it in correctly.", ephemeral: true);
                    return;
                }
            } catch (Exception ex)
            {
                await RespondAsync("Either Guild ID or Role ID was invalid, please make sure you typed it in correctly.", ephemeral: true);
                return;
            }


            sqliteConnection.Open();

            string updateQuery = $@"UPDATE GuildsTable SET roleId={roleId} WHERE guildId={guildId}";
            SQLiteCommand command = new(updateQuery, sqliteConnection);
            command.ExecuteNonQuery();

            sqliteConnection.Close();
            await RespondAsync("Sucess", ephemeral: true);
        }



        [SlashCommand("remove-role", "Removes the role that gets mentioned in a server during trade messages.")]
        public async Task HandleRemoveRole(string guildId)
        {
            try
            {
                bool guildExists = false;
                foreach (SocketGuild _guild in Context.Client.Guilds)
                {
                    if (_guild.Id == Convert.ToUInt64(guildId)) { guildExists = true; break; }
                }

                if (!guildExists)
                {
                    await RespondAsync("Please make sure you entered the Guild ID correctly", ephemeral: true);
                    return;
                }

                sqliteConnection.Open();

                string updateQuery = $@"UPDATE GuildsTable SET roleId=null WHERE guildId={guildId}";
                SQLiteCommand command = new(updateQuery, sqliteConnection);
                command.ExecuteNonQuery();

                sqliteConnection.Close();
                await RespondAsync(":white_check_mark:", ephemeral: true);
            } catch(Exception e)
            {
                await RespondAsync("Please make sure you entered the Guild ID correctly", ephemeral: true);
            }
        }



        [SlashCommand("showguilds", "Shows all the guilds the bot is in along with other info.")]
        public async Task HandleShowCurrentGuilds()
        {
            sqliteConnection.Open();
            string message = "";

            var command = sqliteConnection.CreateCommand();
            command.CommandText = @"SELECT * FROM GuildsTable";

            using (var reader = command.ExecuteReader())
            {
                while(reader.Read())
                {
                    var guildId = reader.GetValue(0);
                    var channelId = reader.GetValue(1);
                    var roleId = reader.GetValue(2);
                    message += $"Guild Id: {guildId}\nChannel Id: {channelId}\nRole Id: {roleId}\n\n";
                    
                }
            }
            await RespondAsync(message, ephemeral: true);

            sqliteConnection.Close();
        }



        [SlashCommand("update", "Sends an UPDATE message through the bot.")]
        public async Task HandleUpdate(string message)
        {
            await DeferAsync(ephemeral: true);

            var bot = Context.Client.CurrentUser;
            List<GuildsTableRow> guildsTableRows = new() { };

            sqliteConnection.Open();

            SQLiteCommand command = new(@"SELECT * FROM GuildsTable", sqliteConnection);
            command.ExecuteNonQuery();

            using (var reader = command.ExecuteReader())
            {
                try
                {
                    while (reader.Read())
                    {
                        if (reader.GetValue(1) == null || reader.GetValue(1) == DBNull.Value) { continue; }

                        var guild = Context.Client.GetGuild(Convert.ToUInt64(reader.GetValue(0)));

                        GuildsTableRow guildsTableRow = new()
                        {
                            Guild = guild,
                            Channel = guild.GetTextChannel(Convert.ToUInt64(reader.GetValue(1))),
                            Role = "",
                        };

                        if (reader.GetValue(2) != null && reader.GetValue(2) != DBNull.Value)
                        {
                            guildsTableRow.Role = $"<@&{reader.GetValue(2)}>";
                        }

                        guildsTableRows.Add(guildsTableRow);
                    }
                }
                catch (InvalidOperationException invalideOperationException)
                {
                    // remove guild from database if it doesn't exist anymore
                    string deleteQuery = $@"DELETE FROM GuildsTable WHERE guildId={reader.GetValue(0)}";
                    SQLiteCommand deleteCommand = new(deleteQuery, sqliteConnection);
                    deleteCommand.ExecuteNonQuery();
                }
            }

            sqliteConnection.Close();

            Embed embed = new EmbedBuilder()
                .AddField(new EmbedFieldBuilder()
                {
                    Name = ":rotating_light: Update :rotating_light:",
                    Value = message
                })
                .WithFooter(new EmbedFooterBuilder()
                {
                    IconUrl = bot.GetAvatarUrl() ?? bot.GetDefaultAvatarUrl(),
                    Text = bot.Username
                    
                }).Build();

            foreach(var row in guildsTableRows)
            {
                await row.Channel.SendMessageAsync(row.Role, embed: embed);
            }

            await FollowupAsync($"Successfully sent update messages to {guildsTableRows.Count} servers.", ephemeral: true);
        }
    }
}
