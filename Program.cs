using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using MoreleOutletTracker.MoreleTracker;
using MoreleOutletTracker.MoreleTracker.JSONObjects;
using Serilog;
namespace MoreleOutletTracker
{
    public class Program
    {
        public static DiscordClient Client { get; set; }
        static string logFileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().WriteTo.File($"{JsonFM.exePath}\\logs\\{logFileName}", outputTemplate: "[{Timestamp:HH:mm:ss dd-MM-yyyy}] [{Level:u3}] {Message:lj}{NewLine}{Exception}").CreateLogger();
            var logFactory = new LoggerFactory().AddSerilog();

            await JsonFM.GetConfig();
            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = Config.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                LoggerFactory = logFactory
            };

            Client = new DiscordClient(discordConfig);

            var slashCommandsConfig = Client.UseSlashCommands();
            slashCommandsConfig.RegisterCommands<MoreleCommands>();

            Client.Ready += Client_Ready;

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static async Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            DiscordActivity activity = new DiscordActivity()
            {
                ActivityType = ActivityType.Watching,
                Name = "Śledzenie produktów....."
            };

            await Client.UpdateStatusAsync(activity, UserStatus.Online, null);

            await MoreleTracker.MoreleTracker.Initialize();
        }
    }
}