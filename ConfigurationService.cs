using System;
using System.IO;
using System.Text.Json;

namespace AktualizatorEME.Services
{
    public class ConfigurationService
    {
        private readonly string _configPath;
        private Configuration _config;

        // Właściwości publiczne dostępne dla MainWindow
        public string GamePath => _config?.GamePath ?? string.Empty;

        public bool ShortcutCreated 
        { 
            get => _config?.ShortcutCreated ?? false; 
            set 
            { 
                if (_config != null) 
                { 
                    _config.ShortcutCreated = value; 
                    SaveConfig(); 
                } 
            } 
        }

        public ConfigurationService()
        {
            _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AktualizatorEME", "config.json");
            LoadConfig();
        }

        public void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string jsonString = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<Configuration>(jsonString) ?? new Configuration();
                }
                else
                {
                    _config = new Configuration();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas ładowania konfiguracji: {ex.Message}");
                _config = new Configuration();
            }
        }

        public string GetGamePath()
        {
            return _config?.GamePath ?? string.Empty;
        }

        public void SetGamePath(string path)
        {
            try
            {
                if (_config != null)
                {
                    _config.GamePath = path;
                    SaveConfig();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas ustawiania ścieżki do katalogu gry: {ex.Message}");
            }
        }

        private void SaveConfig()
        {
            try
            {
                string directory = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string jsonString = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas zapisywania konfiguracji: {ex.Message}");
            }
        }

        public string LastProfileName
        {
            get => _config?.LastProfileName ?? string.Empty;
            set 
            { 
                if (_config != null) 
                { 
                    _config.LastProfileName = value; 
                    SaveConfig(); 
                } 
            }
        }

        // Wewnętrzna klasa reprezentująca strukturę pliku JSON
        private class Configuration
        {
            public string GamePath { get; set; } = string.Empty;
            public bool ShortcutCreated { get; set; } = false;
            public string LastProfileName { get; set; } = string.Empty;
        }
    }
}