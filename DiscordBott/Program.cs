using DSharpPlus.CommandsNext;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DSharpPlus.EventArgs;
using Discord_Bot.Config;
using Discord_Bot.Commands;
using DSharpPlus.Entities;

namespace Discord_Bot
{
    public sealed class Program
    {
        public static DiscordClient Client { get; set; }
        private static CommandsNextExtension Commands { get; set; }

        static async Task Main(string[] args)
        {
            //Reading the Token & Prefix
            var configJson = new ConfigJSONReader();
            await configJson.ReadJSON();

            //Making a Bot Configuration with our token & additional settings
            var config = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = configJson.discordToken,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
            };

            //Initializing the client with this config
            Client = new DiscordClient(config);

            //Setting our default timeout for Interactivity based commands
            Client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(2)
            });

            //EVENT HANDLERS
            Client.Ready += OnClientReady;
            Client.MessageCreated += OnMessageCreated;
            Client.Ready += OnReady;

            //Setting up our Commands Configuration with our Prefix
            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { configJson.discordPrefix },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false,
            };

            //Enabling the use of commands with our config
            Commands = Client.UseCommandsNext(commandsConfig);

            //Prefix Based Commands
            Commands.RegisterCommands<BasicCommands>();

            //Lavalink Configuration
            var endpoint = new ConnectionEndpoint
            {
                Hostname = "v3.lavalink.rocks",
                Port = 443,
                Secured = true
            };

            var lavalinkConfig = new LavalinkConfiguration
            {
                Password = "horizxon.tech",
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };

            var lavalink = Client.UseLavalink();

            //Connect to the Client and get the Bot online
            await Client.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig);
            await Task.Delay(-1);
        }

        private static async Task OnReady(DiscordClient sender, ReadyEventArgs args)
        {
            await sender.UpdateStatusAsync(new DiscordActivity("Crystal_LOL", ActivityType.Watching), UserStatus.Idle);
        }

        private static Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }

        private static async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            // Check if the message author is a bot, and if so, don't respond
            if (e.Author.IsBot)
                return;

            // Your message handling logic here
            if (e.Message.Content.ToLower() == "sa")
            {
                if (DateTime.Now.DayOfWeek == DayOfWeek.Friday)
                {
                    await e.Message.RespondAsync("AleykümSelam mümin kardeşim.");
                }
                else
                {
                    await e.Message.RespondAsync("Cami mi lan burası!");
                }
            }
        }
    }
}
