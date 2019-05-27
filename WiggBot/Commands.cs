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

                await Talk(ctx, "C:\\Temp\\aloha.wav");
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
                await Talk(ctx, "C:\\Temp\\goodbye.wav");
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
            await Talk(ctx, "C:\\Temp\\sweden.wav");
        }

        [Command("alone")]
        public async Task PlayAlone(CommandContext ctx)
        {
            await Talk(ctx, "C:\\Temp\\alonenow.wav");
        }

        [Command("sorry")]
        public async Task Play(CommandContext ctx)
        {
            await Talk(ctx, "C:\\Temp\\wormsorry.wav");
        }

        public async Task OnVoiceReceived(VoiceReceiveEventArgs ea)
        {
            if (!this.mFfmpegs.ContainsKey(ea.SSRC))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $@"-ac 2 -f s16le -ar 48000 -i pipe:0 -ac 2 -ar 44100 {ea.SSRC}.wav",
                    RedirectStandardInput = true
                };

                this.mFfmpegs.TryAdd(ea.SSRC, Process.Start(psi));
            }

            var buff = ea.Voice.ToArray();

            var ffmpeg = this.mFfmpegs[ea.SSRC];
            await ffmpeg.StandardInput.BaseStream.WriteAsync(buff, 0, buff.Length);
            await ffmpeg.StandardInput.BaseStream.FlushAsync();
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