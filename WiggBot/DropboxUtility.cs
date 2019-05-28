using Dropbox.Api;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WiggBot
{
    internal class DropboxFileAccessor : IDisposable
    {
        private const string SETTINGS_FILE_NAME = "settings.json";
        private const string SOUNDS_PATH = "/Sounds";
        private const string DOWNLOADED_SOUNDS_DIRECTORY = "DownloadedSounds";

        private DropboxFileAccessor()
        {
        }

        public Settings Settings { get; private set; }

        public ConcurrentDictionary<string, string> SoundsByName { get; set; } = new ConcurrentDictionary<string, string>();

        public async static Task<DropboxFileAccessor> GetFiles()
        {
            using (var dbx = new DropboxClient("oKgzOptcURAAAAAAAAAAK3U_JbX6JjhHpIyv7rTDpuZRU9v5RFzL6eKg-Hv9JhGw"))
            {
                var root = await dbx.Files.ListFolderAsync(string.Empty);
                var settingsFile = root.Entries.FirstOrDefault(file => String.Equals(file.Name, SETTINGS_FILE_NAME));

                if (settingsFile == null)
                {
                    return null;
                }

                var dpFileAccessor = new DropboxFileAccessor();

                using (var response = await dbx.Files.DownloadAsync($"/{SETTINGS_FILE_NAME}"))
                {
                    dpFileAccessor.Settings = JsonConvert.DeserializeObject<Settings>(await response.GetContentAsStringAsync());
                }

                var localSoundsDirectory = Path.Combine(AppContext.BaseDirectory, DOWNLOADED_SOUNDS_DIRECTORY);

                if (!Directory.Exists(localSoundsDirectory))
                {
                    Directory.CreateDirectory(localSoundsDirectory);
                }

                var soundsDirectory = await dbx.Files.ListFolderAsync(SOUNDS_PATH);
                foreach (var soundFile in soundsDirectory.Entries.Where(i => i.IsFile))
                {
                    var name = Path.GetFileNameWithoutExtension(soundFile.Name);
                    var path = Path.Combine(localSoundsDirectory, soundFile.Name);

                    dpFileAccessor.SoundsByName.TryAdd(name, path);

                    if (File.Exists(path))
                    {
                        continue;
                    }

                    using (var response = await dbx.Files.DownloadAsync($"{SOUNDS_PATH}/{soundFile.Name}"))
                    {
                        using (var fileStream = File.Create(path))
                        {
                            (await response.GetContentAsStreamAsync()).CopyTo(fileStream);
                        }
                    }
                }

                return dpFileAccessor;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public struct Settings
    {
        public string Token { get; set; }
    }
}
