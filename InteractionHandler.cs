using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;
using System.Text.Json;


namespace DiscordTradeBot
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient Client;
        private readonly InteractionService Interactions;
        private readonly IServiceProvider Services;

        public InteractionHandler(DiscordSocketClient client, InteractionService interactions, IServiceProvider services)
        {
            this.Client = client;
            this.Interactions = interactions;
            this.Services = services;
        }

        public async Task InitializeAsync()
        {
            await Interactions.AddModulesAsync(Assembly.GetEntryAssembly(), Services);
            Client.InteractionCreated += HandleInteraction;
        }

        public async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(Client, interaction);
                ulong BotOwnerId = JsonSerializer.Deserialize<Settings>(File.ReadAllText("./TOKEN.json")).OWNERID;

                if(context.User.Id == BotOwnerId)
                {
                    Interactions.ExecuteCommandAsync(context, Services);
                }
                else
                {
                    await context.Interaction.RespondAsync("You do not have permissions to use this command.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
