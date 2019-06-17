using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WiggBot
{
    internal class Commands
    {
        private readonly static DropboxFileAccessor mDropboxFileAccessor = DropboxFileAccessor.Instance;

        private ConcurrentDictionary<uint, Process> mFfmpegs;

        [Command("hi")]
        public async Task Hi(CommandContext ctx)
        {
            await ctx.RespondAsync($"👋 Hi, {ctx.User.Mention}!");
        }

        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNextClient();

            try
            {
                var vnc = vnext.GetConnection(ctx.Guild);
                if (vnc != null)
                    throw new InvalidOperationException("Already connected in this guild.");

                var chn = ctx.Member?.VoiceState?.Channel;
                if (chn == null)
                    throw new InvalidOperationException("You need to be in a voice channel.");

                vnc = await vnext.ConnectAsync(chn);

                if (mDropboxFileAccessor.SoundsByName.TryGetValue("aloha", out var filePath))
                {
                    await Talk(ctx, filePath);
                }

                mFfmpegs = new ConcurrentDictionary<uint, Process>();
                vnc.VoiceReceived += OnVoiceReceived;
            }
            catch (InvalidOperationException ex)
            {
                await ctx.RespondAsync(ex.Message);
                throw;
            }
        }

        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNextClient();

            try
            {
                var vnc = vnext.GetConnection(ctx.Guild);
                if (vnc == null)
                    throw new InvalidOperationException("Not connected in this guild.");
                this.mFfmpegs = null;

                if (mDropboxFileAccessor.SoundsByName.TryGetValue("goodbye", out var filePath))
                {
                    await Talk(ctx, filePath);
                }
                vnc.Disconnect();
            }
            catch (InvalidOperationException ex)
            {
                await ctx.RespondAsync(ex.Message);
                throw;
            }
        }

        [Command("matt")]
        public async Task PlayMatt(CommandContext ctx)
        {
            if (mDropboxFileAccessor.SoundsByName.TryGetValue("sweden", out var filePath))
            {
                await Talk(ctx, filePath);
            }
        }

        [Command("fortnite")]
        public async Task Fortnite(CommandContext ctx)
        {
            if (mDropboxFileAccessor.SoundsByName.TryGetValue("fortnite", out var filePath))
            {
                await Talk(ctx, filePath);
            }
        }

        [Command("gillette")]
        public async Task Gillette(CommandContext ctx)
        {
            if (mDropboxFileAccessor.SoundsByName.TryGetValue("gillette", out var filePath))
            {
                await Talk(ctx, filePath);
            }
        }

        [Command("bestaman")]
        public async Task BestAMan(CommandContext ctx)
        {
            if (mDropboxFileAccessor.SoundsByName.TryGetValue("bestman", out var filePath))
            {
                await Talk(ctx, filePath);
            }
        }

        [Command("alone")]
        public async Task PlayAlone(CommandContext ctx)
        {
            if (mDropboxFileAccessor.SoundsByName.TryGetValue("alonenow", out var filePath))
            {
                await Talk(ctx, filePath);
            }
        }

        [Command("sorry")]
        public async Task Play(CommandContext ctx)
        {
            if (mDropboxFileAccessor.SoundsByName.TryGetValue("wormsorry", out var filePath))
            {
                await Talk(ctx, filePath);
            }
        }

        public async Task OnVoiceReceived(VoiceReceiveEventArgs ea)
        {

            var eventDetails = ea;
            await ea.Client.Guilds.Values.First().Channels.First().SendMessageAsync(eventDetails.User.Username);

            //if (!this.mFfmpegs.ContainsKey(ea.SSRC))
            //{
            //    var psi = new ProcessStartInfo
            //    {
            //        FileName = "ffmpeg",
            //        Arguments = $@"-ac 2 -f s16le -ar 48000 -i pipe:0 -ac 2 -ar 44100 {ea.SSRC}.wav",
            //        RedirectStandardInput = true
            //    };

            //    this.mFfmpegs.TryAdd(ea.SSRC, Process.Start(psi));
            //}

            //var buff = ea.Voice.ToArray();

            //var ffmpeg = this.mFfmpegs[ea.SSRC];
            //await ffmpeg.StandardInput.BaseStream.WriteAsync(buff, 0, buff.Length);
            //await ffmpeg.StandardInput.BaseStream.FlushAsync();
        }

        private static async Task Talk(CommandContext ctx, string file)
        {
            var vnext = ctx.Client.GetVoiceNextClient();

            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
                throw new InvalidOperationException("Not connected in this guild.");

            if (!File.Exists(file))
                throw new FileNotFoundException("File was not found.");

            //await ctx.RespondAsync("👌");
            await vnc.SendSpeakingAsync(true); // send a speaking indicator

            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $@"-i ""{file}"" -ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            var ffmpeg = Process.Start(psi);
            var ffout = ffmpeg.StandardOutput.BaseStream;

            var buff = new byte[3840];
            int br;
            while ((br = ffout.Read(buff, 0, buff.Length)) > 0)
            {
                if (br < buff.Length) // not a full sample, mute the rest
                    for (var i = br; i < buff.Length; i++)
                        buff[i] = 0;

                await vnc.SendAsync(buff, 20);
            }

            await vnc.SendSpeakingAsync(false); // we're not speaking anymore
        }
    }
}