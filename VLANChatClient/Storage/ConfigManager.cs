using System;
using System.IO;
using Microsoft.Win32;

namespace LANChatPro.Storage
{
    public class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LANChatPro",
            "config.json"
        );

        public AppConfig Config { get; private set; }

        public ConfigManager()
        {
            Config = LoadConfig();
        }

        private AppConfig LoadConfig()
        {
            var config = JsonStorage.Load(ConfigPath, JsonContext.Default.AppConfig);
            if (config == null)
            {
                config = CreateDefaultConfig();
                SaveConfig(config);
            }
            else
            {
                bool needsSave = false;
                if (string.IsNullOrEmpty(config.ClientId))
                {
                    config.ClientId = Guid.NewGuid().ToString("N");
                    needsSave = true;
                }

                // Use MachineName as default only if username has never been set
                if (string.IsNullOrWhiteSpace(config.Username))
                {
                    config.Username = Environment.MachineName;
                    needsSave = true;
                }

                if (string.IsNullOrEmpty(config.DownloadFolder))
                {
                    config.DownloadFolder = GetDefaultDownloadFolder();
                    needsSave = true;
                }

                if (needsSave)
                {
                    SaveConfig(config);
                }
            }
            return config;
        }

        public void Save()
        {
            SaveConfig(Config);
            UpdateStartupShortcut();
        }

        private void SaveConfig(AppConfig config)
        {
            JsonStorage.Save(ConfigPath, config, JsonContext.Default.AppConfig);
        }

        private AppConfig CreateDefaultConfig()
        {
            return new AppConfig
            {
                ClientId = Guid.NewGuid().ToString("N"),
                Username = Environment.MachineName,
                AvatarIndex = new Random().Next(0, 8),
                DownloadFolder = GetDefaultDownloadFolder(),
                EnableSound = true,
                StartWithWindows = false,
                Port = 50002
            };
        }

        private string GetDefaultDownloadFolder()
        {
            string downloads = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "LANChatPro"
            );
            try
            {
                if (!Directory.Exists(downloads))
                {
                    Directory.CreateDirectory(downloads);
                }
            }
            catch
            {

                downloads = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LANChatPro",
                    "Downloads"
                );
                if (!Directory.Exists(downloads))
                {
                    Directory.CreateDirectory(downloads);
                }
            }
            return downloads;
        }

        private void UpdateStartupShortcut()
        {
            try
            {
                string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(runKey, true))
                {
                    if (key != null)
                    {
                        string appName = "LANChatPro";
                        string? exePath = Environment.ProcessPath;

                        if (string.IsNullOrEmpty(exePath))
                            return;

                        if (Config.StartWithWindows)
                        {
                            key.SetValue(appName, $"\"{exePath}\"");
                        }
                        else
                        {
                            key.DeleteValue(appName, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update startup shortcut: {ex.Message}");
            }
        }
    }
}
