using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Net.Http;
namespace AktualizatorEME.Services
{
    public class UpdaterApplicationService
    {
        private readonly LoggerService _logger;
        private readonly string _currentVersion = "1.0.7"; // Zmień na aktualną wersję programu
        private readonly string _versionUrl = "http://server.endore.pl:3300/version.json"; // Adres pliku `version.json`
        private readonly string _configFilePath = "appConfig.json"; // Ścieżka do pliku konfiguracji aplikacji

        public UpdaterApplicationService(LoggerService logger)
        {
            _logger = logger;
        }

        public async Task CheckForAppUpdateAsync()
        {
            try
            {
                _logger.LogMessage("Sprawdzanie dostępności nowej wersji aplikacji...");

                // Pobierz dane z pliku version.json
                // Użycie HttpClient zamiast WebClient
                using var httpClient = new HttpClient();
                string serverResponse = await httpClient.GetStringAsync(_versionUrl);

                // Logowanie surowej odpowiedzi z serwera
                //_logger.LogMessage($"Otrzymana odpowiedź z serwera: {serverResponse}");

                if (string.IsNullOrWhiteSpace(serverResponse))
                {
                    _logger.LogMessage("Nie udało się pobrać danych z serwera. Plik `version.json` jest pusty.");
                    return;
                }

                // Deserializacja danych JSON
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true, // Ignoruj wielkość liter w nazwach właściwości
                        WriteIndented = true
                    };

                    var serverVersionData = JsonSerializer.Deserialize<AppVersion>(serverResponse, options);
                    if (serverVersionData == null || string.IsNullOrWhiteSpace(serverVersionData.Version))
                    {
                        _logger.LogMessage("Nie udało się sparsować danych z serwera lub brak wersji w `version.json`.");
                        return;
                    }

                    // Odczytaj dane z `version.json`
                    string serverVersion = serverVersionData.Version;
                    string downloadUrl = serverVersionData.DownloadUrl;
                    string changelog = serverVersionData.Changelog;
                    string installedVersion = GetInstalledAppVersion();

                    _logger.LogMessage($"Wersja z serwera: {serverVersion}, Zainstalowana wersja: {installedVersion}");

                    if (CompareVersion(serverVersion, installedVersion) > 0)
                    {
                        _logger.LogMessage($"Dostępna jest nowa wersja aplikacji: {serverVersion}.");
                        _logger.LogMessage($"Zmiany w nowej wersji: {changelog}");
                        await UpdateAppAsync(downloadUrl, serverVersion);
                    }
                    else
                    {
                        _logger.LogMessage("Aplikacja jest aktualna.");
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogMessage($"Błąd parsowania JSON: {jsonEx.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogMessage($"Błąd podczas sprawdzania aktualizacji aplikacji: {ex.Message}");
            }
        }

        private string GetInstalledAppVersion()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(_configFilePath));
                    if (config != null && !string.IsNullOrWhiteSpace(config.InstalledVersion))
                    {
                        _logger.LogMessage($"Zainstalowana wersja aplikacji: {config.InstalledVersion}");
                        return config.InstalledVersion;
                    }
                }

                _logger.LogMessage($"Brak pliku konfiguracji. Używanie wersji domyślnej: {_currentVersion}");
                return _currentVersion;
            }
            catch (Exception ex)
            {
                _logger.LogMessage($"Błąd podczas odczytu pliku konfiguracji: {ex.Message}");
                return _currentVersion;
            }
        }

        private void SetInstalledAppVersion(string version)
        {
            try
            {
                var config = new AppConfig { InstalledVersion = version };
                File.WriteAllText(_configFilePath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                _logger.LogMessage($"Błąd podczas zapisywania wersji w konfiguracji: {ex.Message}");
            }
        }

        private int CompareVersion(string versionA, string versionB)
        {
            if (string.IsNullOrWhiteSpace(versionA) || string.IsNullOrWhiteSpace(versionB))
            {
                throw new ArgumentException("Porównywane wersje nie mogą być puste.");
            }

            var partsA = versionA.Split('.');
            var partsB = versionB.Split('.');

            for (int i = 0; i < Math.Max(partsA.Length, partsB.Length); i++)
            {
                int partA = i < partsA.Length ? int.Parse(partsA[i]) : 0;
                int partB = i < partsB.Length ? int.Parse(partsB[i]) : 0;

                if (partA > partB) return 1;
                if (partA < partB) return -1;
            }

            return 0;
        }

        private async Task UpdateAppAsync(string downloadUrl, string newVersion)
        {
            try
            {
                _logger.LogMessage("Pobieranie nowej wersji aplikacji...");
                string newFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updaterEME_new.exe");
                string currentFilePath = Process.GetCurrentProcess().MainModule?.FileName ?? throw new InvalidOperationException("Nie można odnaleźć aktualnego pliku wykonywalnego.");

                using var client = new WebClient();
                await client.DownloadFileTaskAsync(downloadUrl, newFilePath);

                // Zamiana plików i uruchomienie nowej wersji
                await Task.Delay(1000);
                File.Move(currentFilePath, $"{currentFilePath}.old", true);
                File.Move(newFilePath, currentFilePath, true);

                SetInstalledAppVersion(newVersion);
                _logger.LogMessage($"Aplikacja zaktualizowana do wersji {newVersion}. Uruchamianie nowej wersji...");

                // Uruchomienie nowej wersji
                Process.Start(currentFilePath);

                // Zamykanie aktualnej wersji
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                _logger.LogMessage($"Błąd podczas aktualizacji aplikacji: {ex.Message}");
            }
        }

        public class AppVersion
        {
            [JsonPropertyName("version")]
            public string Version { get; set; } = string.Empty;

            [JsonPropertyName("download_url")]
            public string DownloadUrl { get; set; } = string.Empty;

            [JsonPropertyName("changelog")]
            public string Changelog { get; set; } = string.Empty;
        }

        private class AppConfig
        {
            public string InstalledVersion { get; set; } = string.Empty;
        }
    }
}
