using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Discord.WebSocket;
using Discord.Interactions;
using Discord;
using System.Data.SQLite;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DiscordTradeBot
{

    
    class Program
    {
        public static Task Main() => new Program().MainAsync();

        public async Task MainAsync()
        {
            using IHost host = Host.CreateDefaultBuilder().ConfigureServices((_, services) => services
            .AddSingleton(x => new DiscordSocketClient(new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged,
                AlwaysDownloadUsers = true,
            }))
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<InteractionHandler>()).Build();

            await RunAsync(host);
        }

        public async Task RunAsync(IHost host)
        {
            using IServiceScope serviceScope = host.Services.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;

            DiscordSocketClient client = provider.GetRequiredService<DiscordSocketClient>();
            InteractionService interactions = provider.GetRequiredService<InteractionService>();
            await provider.GetRequiredService<InteractionHandler>().InitializeAsync();


            // creates database if it doesn't exist
            string sqliteConnectionString = "Data Source=GuildsDatabase.db";
            SQLiteConnection sqliteConnection = new(sqliteConnectionString);

            sqliteConnection.Open();

            string createTableQuery = @"CREATE TABLE IF NOT EXISTS GuildsTable(guildId TEXT PRIMARY KEY, channelId TEXT, roleId TEXT)";
            SQLiteCommand createTableCommand = new(createTableQuery, sqliteConnection);
            createTableCommand.ExecuteNonQuery();

            sqliteConnection.Close();



            client.Log += async (LogMessage msg) => Console.WriteLine(msg);
            interactions.Log += async (LogMessage msg) => Console.WriteLine(msg);

            client.Ready += async () =>
            {
                Console.WriteLine("Discord Bot is Ready");
                await interactions.RegisterCommandsGloballyAsync(deleteMissing: true);

                sqliteConnection.Open();
                // Adds missing guilds into the database
                foreach (SocketGuild _guild in client.Guilds)
                {
                    string insertQuery = $@"INSERT OR IGNORE INTO GuildsTable (guildId) VALUES ('{_guild.Id}')";
                    SQLiteCommand insertCommand = new(insertQuery, sqliteConnection);
                    insertCommand.ExecuteNonQuery();
                }
                sqliteConnection.Close();
            };


            client.JoinedGuild += async (SocketGuild guild) =>
            {
                try
                {
                    // Adds the guild's data into the database
                    sqliteConnection.Open();

                    string insertQuery = $@"INSERT OR IGNORE INTO GuildsTable (guildId) VALUES ('{guild.Id}')";
                    SQLiteCommand command = new(insertQuery, sqliteConnection);
                    command.ExecuteNonQuery();

                    sqliteConnection.Close();
                    Console.WriteLine($"Joined guild {guild.Id}");
                } catch(Exception ex) { Console.WriteLine(ex.Message); }
            };

            client.LeftGuild += async (SocketGuild guild) =>
            {
                try
                {
                    // Deletes the guild's data from the database
                    sqliteConnection.Open();

                    string deleteQuery = $@"DELETE FROM GuildsTable WHERE guildId='{guild.Id}'";
                    SQLiteCommand command = new(deleteQuery, sqliteConnection);
                    command.ExecuteNonQuery();

                    sqliteConnection.Close();
                    Console.WriteLine($"Left guild {guild.Id}");

                } catch(Exception ex) { Console.WriteLine(ex.ToString()); }
            };


            string TOKEN = JsonSerializer.Deserialize<Settings>(File.ReadAllText("TOKEN.json")).TOKEN;

            await client.LoginAsync(TokenType.Bot, TOKEN);
            await client.StartAsync();
            await Task.Delay(-1);
        }
    }
    

    class Settings
    {
        public string TOKEN { get; set; }
        public ulong OWNERID { get; set; }
    }
}
