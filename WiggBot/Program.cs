using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.VoiceNext;
using System;
using System.Threading.Tasks;

namespace WiggBot
{
    internal class Program
    {
        private static DiscordClient mDiscord;
        private static CommandsNextModule mCommands;
        private static DropboxFileAccessor mDPFileAccessor;

        private static void Main()
        {
            try
            {
                mDPFileAccessor = DropboxFileAccessor.GetFiles().GetAwaiter().GetResult();
                var prog = new Program();

                prog.MainAsync(mDPFileAccessor.Settings.Token).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                Console.ReadKey();
            }
            finally
            {
                mDPFileAccessor.Dispose();
            }
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

            mDiscord.UseVoiceNext(new VoiceNextConfiguration
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
    }
}