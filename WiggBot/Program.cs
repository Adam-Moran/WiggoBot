using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.VoiceNext;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace WiggBot
{
    internal class Program
    {
        private static DiscordClient mDiscord;
        private static CommandsNextModule mCommands;
        private static VoiceNextClient mVoice;

        private static void Main()
        {
            if (!TryReadConfiguration(out var token))
            {
                return;
            }

            var prog = new Program();
            prog.MainAsync(token).GetAwaiter().GetResult();
        }

        private async Task MainAsync(string token)
        {
            mDiscord = new DiscordClient(new DiscordConfiguration
            {
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug,
                Token = token,
                TokenType = TokenType.Bot,
            });

            mCommands = mDiscord.UseCommandsNext(new CommandsNextConfiguration
            {
                EnableDms = true,
                EnableMentionPrefix = true,
                StringPrefix = ";;"
            });

            mVoice = mDiscord.UseVoiceNext(new VoiceNextConfiguration
            {
                EnableIncoming = true
            });

            mCommands.RegisterCommands<Commands>();

            CreateMessageBindings();

            await mDiscord.ConnectAsync();
            await Task.Delay(-1);
        }

        private static void CreateMessageBindings()
        {
            mDiscord.MessageCreated += async e =>
            {
                if (e.Message.Content.StartsWith("hippo", StringComparison.OrdinalIgnoreCase))
                    await e.Message.RespondAsync("Matt Lawrence is shit at Fortnite");
            };
        }

        private static bool TryReadConfiguration(out string token)
        {
            token = string.Empty;

            try
            {
                using (var reader = new StreamReader("settings.json"))
                {
                    var content = reader.ReadToEnd();

                    var configuration = JsonConvert.DeserializeObject<Configuration>(content);
                    token = configuration.Token;

                    return !String.IsNullOrEmpty(token);
                }
            }
            catch (FileNotFoundException)
            {
                return false;
            }

        }

        private class Configuration
        {
            public string Token { get; set; }
        }
    }
}