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
                // 🛑 CHANGED: hyp-global -> hyp-cn
                var uri = $"hyp-cn://launchgame?gamebiz={GameBiz}&openGame=true";

                // Note: CN games usually don't need 'package', but we keep support just in case
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
        public static string PluginID => "2C03D96350FE4EBE81A9399F36F4E46E";

        private PluginInitContext? _context;
        private List<HoYoGame> _cachedGames = new List<HoYoGame>();

        public string Name => "HoYoPlay Quick Launch - Mainland China";
        public string Description => "Quickly launch miHoYo games via HoYoPlay";

        public void Init(PluginInitContext context)
        {
            _context = context;

            // This writes to the standard PowerToys log path
            Log.Info($"[HoYoPlay] Init started. Version: {_context.CurrentPluginMetadata.Version}", typeof(Main));

            try
            {
                Log.Info("[HoYoPlay] Scanning registry for games...", typeof(Main));
                _cachedGames = GetInstalledGames();
                Log.Info($"[HoYoPlay] Found {_cachedGames.Count} games. Init Success!", typeof(Main));
            }
            catch (Exception ex)
            {
                // This captures the full stack trace in the main log
                Log.Exception($"[HoYoPlay] FATAL ERROR during Init: {ex.Message}", ex, typeof(Main));
                throw;
            }
        }
        
        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            var search = query.Search;

            // 🛠️ RELOAD COMMAND
            // If user types "reload" or just "r", offer to refresh the list
            if (string.Equals(search, "reload", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(search, "r", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new Result
                {
                    Title = "Reload Game List",
                    SubTitle = "Force a re-scan of the Registry for installed games",
                    IcoPath = "Images\\icon.ico", // Use your main plugin icon
                    Action = _ =>
                    {
                        // Force the refresh
                        _cachedGames = GetInstalledGames();

                        // Tell PowerToys to refresh the UI immediately to show new games
                        _context?.API.ChangeQuery(query.ActionKeyword, true);
                        return true;
                    }
                });

                // Return immediately so we don't mix this with game results
                return results;
            }

            // Use the cached list
            foreach (var game in _cachedGames)
            {
                // Filter logic
                if (string.IsNullOrEmpty(search) || game.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new Result
                    {
                        Title = game.Title,
                        SubTitle = "Launch via HoYoPlay",
                        // Ensure this matches your ACTUAL filenames (e.g., .png vs .jpg)
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
                            catch
                            {
                                return false;
                            }
                        }
                    });
                }
            }
            return results;
        }

        private List<HoYoGame> GetInstalledGames()
        {
            var games = new List<HoYoGame>();

            // 🛑 CHANGED: Cognosphere -> miHoYo
            string rootPath = @"Software\miHoYo\HYP\1_1";

            using (RegistryKey? rootKey = Registry.CurrentUser.OpenSubKey(rootPath))
            {
                if (rootKey == null)
                {
                    Log.Warn($"[HoYoPlay] Registry key not found: {rootPath}", typeof(Main));
                    return games;
                }

                string[] subKeys = rootKey.GetSubKeyNames();

                foreach (var subKeyName in subKeys)
                {
                    using (RegistryKey? gameKey = rootKey.OpenSubKey(subKeyName))
                    {
                        if (gameKey == null) continue;

                        // Validation: Ensure the game is actually installed
                        object? pathValue = gameKey.GetValue("GameInstallPath");
                        if (pathValue is not string path || string.IsNullOrWhiteSpace(path))
                        {
                            continue;
                        }

                        // 🛑 CHANGED: Mapping for Mainland China IDs
                        if (subKeyName == "hk4e_cn")
                        {
                            games.Add(new HoYoGame { Title = "Genshin Impact (CN)", GameBiz = "hk4e_cn", IconName = "icon_ys.ico" });
                        }
                        else if (subKeyName == "hkrpg_cn")
                        {
                            games.Add(new HoYoGame { Title = "Honkai: Star Rail (CN)", GameBiz = "hkrpg_cn", IconName = "icon_sr.ico" });
                        }
                        else if (subKeyName == "nap_cn")
                        {
                            games.Add(new HoYoGame { Title = "Zenless Zone Zero (CN)", GameBiz = "nap_cn", IconName = "icon_zzz.ico" });
                        }
                        // 🛑 SIMPLIFIED: No complex parsing needed for HI3 CN
                        else if (subKeyName == "bh3_cn")
                        {
                            games.Add(new HoYoGame { Title = "Honkai Impact 3rd (CN)", GameBiz = "bh3_cn", IconName = "icon_bh3.ico" });
                        }
                    }
                }
            }
            return games;
        }

            return new HoYoGame
        }
    }
}