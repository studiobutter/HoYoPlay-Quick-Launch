using Wox.Plugin;
using Wox.Plugin.Logger;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace PowerToysRun.HoYoPlay
{
    public class HoYoGame
    {
        public string Title { get; set; } = string.Empty;
        public string GameBiz { get; set; } = string.Empty;
        public string? Package { get; set; }
        public string IconName { get; set; } = string.Empty;

        public string LaunchUri
        {
            get
            {
                // 🛑 CN Protocol
                var uri = $"hyp-cn://launchgame?gamebiz={GameBiz}&openGame=true";
                if (!string.IsNullOrEmpty(Package))
                {
                    uri += $"&package={Package}";
                }
                return uri;
            }
        }
    }

    public class Main : IPlugin
    {
        // 🛑 Make sure this ID matches your new CN GUID in plugin.json
        public static string PluginID => "2C03D96350FE4EBE81A9399F36F4E46E";

        private PluginInitContext? _context;
        private List<HoYoGame> _cachedGames = new List<HoYoGame>();

        public string Name => "HoYoPlay Quick Launch (CN)";
        public string Description => "Quickly launch miHoYo games (China) via HoYoPlay";

        public void Init(PluginInitContext context)
        {
            _context = context;
            Log.Info($"[HoYoPlay CN] Init started. Version: {_context.CurrentPluginMetadata.Version}", typeof(Main));

            try
            {
                _cachedGames = GetInstalledGames();
                Log.Info($"[HoYoPlay CN] Found {_cachedGames.Count} games.", typeof(Main));
            }
            catch (Exception ex)
            {
                Log.Exception($"[HoYoPlay CN] FATAL ERROR: {ex.Message}", ex, typeof(Main));
                throw;
            }
        }
        
        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            var search = query.Search;

            // Reload Command
            if (string.Equals(search, "reload", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(search, "r", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new Result
                {
                    Title = "Reload Game List",
                    SubTitle = "Force a re-scan of the Registry",
                    IcoPath = "Images\\icon.ico", 
                    Action = _ =>
                    {
                        _cachedGames = GetInstalledGames();
                        _context?.API.ChangeQuery(query.ActionKeyword, true);
                        return true;
                    }
                });
                return results;
            }

            foreach (var game in _cachedGames)
            {
                if (string.IsNullOrEmpty(search) || game.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new Result
                    {
                        Title = game.Title,
                        SubTitle = "Launch via HoYoPlay (CN)",
                        IcoPath = $"Images\\{game.IconName}",
                        Action = _ =>
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = game.LaunchUri,
                                    UseShellExecute = true
                                });
                                return true;
                            }
                            catch { return false; }
                        }
                    });
                }
            }
            return results;
        }

        private List<HoYoGame> GetInstalledGames()
        {
            var games = new List<HoYoGame>();
            // 🛑 CN Registry Path
            string rootPath = @"Software\miHoYo\HYP\1_0";

            using (RegistryKey? rootKey = Registry.CurrentUser.OpenSubKey(rootPath))
            {
                if (rootKey == null) return games;

                foreach (var subKeyName in rootKey.GetSubKeyNames())
                {
                    using (RegistryKey? gameKey = rootKey.OpenSubKey(subKeyName))
                    {
                        if (gameKey == null) continue;

                        object? pathValue = gameKey.GetValue("GameInstallPath");
                        if (pathValue is not string path || string.IsNullOrWhiteSpace(path)) continue;

                        // 🛑 CN Game Mapping (Simple, no Parsing needed)
                        if (subKeyName == "hk4e_cn")
                            games.Add(new HoYoGame { Title = "Genshin Impact (CN)", GameBiz = "hk4e_cn", IconName = "icon_ys.ico" });
                        else if (subKeyName == "hkrpg_cn")
                            games.Add(new HoYoGame { Title = "Honkai: Star Rail (CN)", GameBiz = "hkrpg_cn", IconName = "icon_sr.ico" });
                        else if (subKeyName == "nap_cn")
                            games.Add(new HoYoGame { Title = "Zenless Zone Zero (CN)", GameBiz = "nap_cn", IconName = "icon_zzz.ico" });
                        else if (subKeyName == "bh3_cn")
                            games.Add(new HoYoGame { Title = "Honkai Impact 3rd (CN)", GameBiz = "bh3_cn", IconName = "icon_bh3.ico" });
                    }
                }
            }
            return games;
        }
    }
}