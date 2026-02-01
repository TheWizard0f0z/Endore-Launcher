using System.Net.Http;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using System.IO;
using System.Linq;

namespace AktualizatorEME.Services
{
    public class UpdateService
    {
        private readonly LoggerService _logger;
        private readonly ConfigurationService _configService;
        public bool SkipClassicUO { get; set; } = false;

        // Konstruktor
        public UpdateService(LoggerService logger, ConfigurationService configService)
        {
            _logger = logger;
            _configService = configService;
        }

        public async Task UpdateFiles(IProgress<double> progress)
        {
            _logger.LogMessage("Rozpoczynam skanowanie plików...");

            string gamePath = _configService.GetGamePath();
            if (string.IsNullOrEmpty(gamePath))
            {
                _logger.LogMessage("Błąd: Ścieżka do katalogu gry nie została ustawiona.");
                return;
            }

            // Skanowanie plików lokalnych
            var fileHashes = new Dictionary<string, string>();
            foreach (var filePath in Directory.GetFiles(gamePath, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(gamePath, filePath).Replace("\\", "/");

                // Pomijanie folderu ClassicUO, jeśli opcja została zaznaczona
                if (SkipClassicUO && relativePath.StartsWith("ClassicUO/"))
                {
                    //_logger.LogMessage($"Pomijam folder ClassicUO: {relativePath}");
                    continue;
                }

                string hash = await GetFileHash(filePath);
                if (hash != null)
                {
                    fileHashes[relativePath] = hash;
                }
            }

            // Przygotowanie danych do wysłania do serwera
            string endpoint = "http://server.endore.pl:3300/api/updater";
            var postData = new
            {
                files_to_check = fileHashes,
                settings_json = LoadSettings(gamePath),
                skip_classicuo = SkipClassicUO // Dodanie sygnału o pomijaniu ClassicUO
            };

            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsJsonAsync(endpoint, postData);
                if (response.IsSuccessStatusCode)
                {
                    var serverResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    _logger.LogMessage("Rozpoczęto proces synchronizacji plików...");

                    int totalFiles = serverResponse.Count;
                    int currentFile = 0;

                    foreach (var file in serverResponse)
                    {
                        currentFile++;
                        try
                        {
                            // Pomijanie folderu ClassicUO, jeśli opcja została zaznaczona
                            if (SkipClassicUO && file.Key.StartsWith("ClassicUO/"))
                            {
                                //_logger.LogMessage($"Pomijam synchronizację pliku: {file.Key}");
                                continue;
                            }

                            await DownloadAndExtractFolder(file.Key, gamePath);
                            _logger.LogMessage($"Pobrano i zaktualizowano plik: {file.Key} ({currentFile}/{totalFiles})");

                            // Aktualizowanie postępu w trakcie synchronizacji
                            double progressValue = (double)currentFile / totalFiles;
                            progress?.Report(progressValue);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogMessage($"Błąd podczas rozpakowywania pliku {file.Key}: {ex.Message}");
                        }
                    }

                    // Usuwanie lokalnych plików, które nie znajdują się na serwerze
                    _logger.LogMessage("Sprawdzanie i usuwanie zbędnych plików...");
                    await RemoveLocalFilesNotOnServer(gamePath);

                    _logger.LogMessage("Aktualizacja zakończona pomyślnie!");
                    progress?.Report(1.0); // Postęp pełny
                }
                else
                {
                    _logger.LogMessage($"Błąd podczas aktualizacji: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogMessage($"Błąd podczas aktualizacji: {ex.Message}");
            }
        }




        private async Task<string?> GetFileHash(string filePath)
        {
            try
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                byte[] hashBytes = await sha256.ComputeHashAsync(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
            catch (Exception ex)
            {
                _logger.LogMessage($"Błąd podczas obliczania hash dla pliku {filePath}: {ex.Message}");
                return null; // Możesz zwrócić null lub pusty string, aby uniknąć ostrzeżeń o wartości null
            }
        }


        private string LoadSettings(string gamePath)
        {
            var settingsPath = Path.Combine(gamePath, "ClassicUO", "settings.json");
            if (File.Exists(settingsPath))
            {
                try
                {
                    return File.ReadAllText(settingsPath);
                }
                catch (Exception ex)
                {
                    _logger.LogMessage($"Błąd podczas odczytu settings.json: {ex.Message}");
                }
            }
            return "{}";
        }

        private async Task DownloadAndExtractFolder(string fileName, string currentDir)
        {
            string url = $"http://server.endore.pl:3300/download/{fileName}";
            string tempZip = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
            string tempExtractDir = Path.Combine(currentDir, $"temp_{Path.GetFileNameWithoutExtension(fileName)}");
            string settingsFilePath = Path.Combine(currentDir, "ClassicUO", "settings.json");
            string protectedFolder = Path.Combine(currentDir, "ClassicUO", "Data", "Plugins", "Razor", "Profiles", "default");

            try
            {
                //_logger.LogMessage($"Rozpoczynam pobieranie pliku: {url}");

                // Pobieranie pliku w częściach
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(10); // Zwiększenie czasu oczekiwania na duże pliki

                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var buffer = new byte[8192];
                    long totalRead = 0;
                    bool isMoreToRead = true;

                    using var stream = await response.Content.ReadAsStreamAsync();
                    while (isMoreToRead)
                    {
                        var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (read == 0)
                        {
                            isMoreToRead = false;
                            continue;
                        }

                        await fs.WriteAsync(buffer, 0, read);
                        totalRead += read;

                        if (totalBytes > 0)
                        {
                            double progress = (double)totalRead / totalBytes;
                        }
                    }
                }

                ZipFile.ExtractToDirectory(tempZip, tempExtractDir, true);

                foreach (var file in Directory.GetFiles(tempExtractDir, "*.*", SearchOption.AllDirectories))
                {
                    string destinationPath = Path.Combine(currentDir, Path.GetRelativePath(tempExtractDir, file));
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

                    // Obsługa pliku settings.json
                    if (Path.GetFullPath(destinationPath) == Path.GetFullPath(settingsFilePath))
                    {
                        try
                        {
                            string tempSettingsPath = Path.Combine(tempExtractDir, "ClassicUO", "settings.json");
                            if (File.Exists(tempSettingsPath))
                            {
                                if (!File.Exists(settingsFilePath))
                                {
                                    // Jeśli plik settings.json nie istnieje, skopiuj go w całości
                                    File.Copy(tempSettingsPath, settingsFilePath, true);
                                    _logger.LogMessage("Plik settings.json nie istniał, został skopiowany w całości.");
                                }
                                else
                                {
                                    // Załaduj zawartość aktualnych i tymczasowych ustawień jako dynamiczne słowniki
                                    var currentSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(settingsFilePath));
                                    var tempSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(tempSettingsPath));

                                    if (currentSettings != null && tempSettings != null)
                                    {
                                        // Edytuj tylko wybrane klucze
                                        foreach (var key in new[] { "ip", "port", "clientversion", "last_server_name", "lastservernum" })
                                        {
                                            if (tempSettings.ContainsKey(key))
                                            {
                                                currentSettings[key] = tempSettings[key];
                                            }
                                        }

                                        // Zapisz zmodyfikowane ustawienia
                                        File.WriteAllText(settingsFilePath, JsonSerializer.Serialize(currentSettings, new JsonSerializerOptions { WriteIndented = true }));
                                        _logger.LogMessage("Zaktualizowano wybrane elementy w settings.json");
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogMessage("Nie znaleziono tymczasowego pliku settings.json do nadpisania.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogMessage($"Błąd podczas aktualizacji settings.json: {ex.Message}");
                        }

                        continue; // Pomija dalsze kopiowanie settings.json
                    }

                    // Ochrona plików w chronionym folderze
                    if (Path.GetFullPath(destinationPath).StartsWith(Path.GetFullPath(protectedFolder)))
                    {
                        if (File.Exists(destinationPath))
                        {
                            _logger.LogMessage($"Pominięto plik chroniony: {destinationPath}");
                            continue;
                        }
                        else
                        {
                            _logger.LogMessage($"Plik w chronionym folderze nie istnieje, kopiowanie: {destinationPath}");
                        }
                    }

                    // Kopiuj pozostałe pliki
                    File.Copy(file, destinationPath, true);
                    _logger.LogMessage($"Zaktualizowano plik: {destinationPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogMessage($"Błąd podczas pobierania pliku '{fileName}': {ex.Message}");
            }
            finally
            {
                if (File.Exists(tempZip))
                {
                    File.Delete(tempZip);
                }

                if (Directory.Exists(tempExtractDir))
                {
                    Directory.Delete(tempExtractDir, true);
                    _logger.LogMessage($"Usunięto katalog tymczasowy: {tempExtractDir}");
                }
            }
        }



        private async Task RemoveLocalFilesNotOnServer(string baseDir)
        {
            try
            {
                string url = "http://server.endore.pl:3300/api/delete-list";
                using var client = new HttpClient();
                var deleteList = await client.GetFromJsonAsync<List<string>>(url);

                if (deleteList == null)
                {
                    _logger.LogMessage("Brak plików do usunięcia.");
                    return;
                }

                string executableName = Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);

                foreach (var item in deleteList)
                {
                    string itemPath = Path.Combine(baseDir, item.Replace("/", "\\"));

                    if (File.Exists(itemPath) && !itemPath.Contains(executableName))
                    {
                        File.Delete(itemPath);
                        _logger.LogMessage($"Usunięto plik: '{item}'");
                    }
                    else if (Directory.Exists(itemPath))
                    {
                        Directory.Delete(itemPath, true);
                        _logger.LogMessage($"Usunięto folder: '{item}' wraz z zawartością");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogMessage($"Błąd podczas pobierania listy plików do usunięcia: {ex.Message}");
            }
        }
    }
}
