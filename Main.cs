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
                var uri = $"hyp-global://launchgame?gamebiz={GameBiz}&openGame=true";
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
        public static string PluginID => "8F13CE4676B6418F8D25B0AE044180C8";

        private PluginInitContext? _context;
        private List<HoYoGame> _cachedGames = new List<HoYoGame>();

        public string Name => "HoYoPlay Quick Launch";
        public string Description => "Quickly launch HoYoverse games via HoYoPlay";

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
            // Note: This path is usually in HKCU (Current User)
            string rootPath = @"Software\Cognosphere\HYP\1_0";

            using (RegistryKey? rootKey = Registry.CurrentUser.OpenSubKey(rootPath))
            {
                if (rootKey == null) return games;

                string[] subKeys = rootKey.GetSubKeyNames();

                foreach (var subKeyName in subKeys)
                {
                    using (RegistryKey? gameKey = rootKey.OpenSubKey(subKeyName))
                    {
                        if (gameKey == null) continue;

                        object? pathValue = gameKey.GetValue("GameInstallPath");
                        if (pathValue is not string path || string.IsNullOrWhiteSpace(path))
                        {
                            continue;
                        }

                        // --- GAME MAPPING ---
                        // Update these Image names to match your actual files!
                        if (subKeyName == "hk4e_global")
                        {
                            games.Add(new HoYoGame { Title = "Genshin Impact", GameBiz = "hk4e_global", IconName = "icon_ys.ico" });
                        }
                        else if (subKeyName == "hkrpg_global")
                        {
                            games.Add(new HoYoGame { Title = "Honkai: Star Rail", GameBiz = "hkrpg_global", IconName = "icon_sr.ico" });
                        }
                        else if (subKeyName == "nap_global")
                        {
                            games.Add(new HoYoGame { Title = "Zenless Zone Zero", GameBiz = "nap_global", IconName = "icon_zzz.ico" });
                        }
                        else if (subKeyName == "abc_global")
                        {
                            games.Add(new HoYoGame { Title = "Honkai: Nexus Anima", GameBiz = "abc_global", IconName = "icon_na.ico" });
                        }
                        else if (subKeyName == "hyg_global")
                        {
                            games.Add(new HoYoGame { Title = "Petit Planet", GameBiz = "hyg_global", IconName = "icon_planet.ico" });
                        }
                        else if (subKeyName.StartsWith("bh3_global"))
                        {
                            var hi3Game = ParseHonkaiImpact(subKeyName);
                            if (hi3Game != null) games.Add(hi3Game);
                        }
                    }
                }
            }
            return games;
        }

        private HoYoGame? ParseHonkaiImpact(string registryKey)
        {
            string title = "Honkai Impact 3rd";
            string package = "glb_official"; // Default fallback

            // Make it case-insensitive and lenient
            string lowerKey = registryKey.ToLower();

            if (lowerKey.Contains("overseas"))
            {
                title += " (SEA)";
                package = "overseas_official";
            }
            else if (lowerKey.Contains("jp"))
            {
                title += " (Japan)";
                package = "jp_official";
            }
            else if (lowerKey.Contains("kr"))
            {
                title += " (Korea)";
                package = "kr_official";
            }
            else if (lowerKey.Contains("asia") || lowerKey.Contains("tw"))
            {
                title += " (TW/HK/MO)";
                package = "asia_official";
            }
            else if (lowerKey.Contains("glb") || lowerKey.Contains("global"))
            {
                title += " (Global)";
                package = "glb_official";
            }
            else
            {
                // 🛑 DEBUG FALLBACK:
                // If we found a folder but didn't recognize the region, show it anyway so we know!
                title += $" (Unknown Region: {registryKey})";
                package = "glb_official";
            }

            return new HoYoGame
            {
                Title = title,
                GameBiz = "bh3_global", // Base ID is usually constant
                Package = package,
                IconName = "icon_bh3.ico"
            };
        }
    }
}