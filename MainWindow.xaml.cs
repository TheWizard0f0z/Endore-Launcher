using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using AktualizatorEME.Services;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AktualizatorEME
{
    public partial class MainWindow : Window
    {
        private readonly ConfigurationService _configService;
        private readonly LoggerService _logger;
        private readonly UpdateService _updateService;
        private readonly MusicService _musicService;
        private readonly UpdaterApplicationService _appUpdater;
        private readonly ServerStatusService _statusService;
        private DispatcherTimer _statusTimer;

        private string _selectedProfilePath = "";

        public MainWindow()
        {
            InitializeComponent();

            // Inicjalizacja serwisów
            _configService = new ConfigurationService();
            _logger = new LoggerService(StatusEditor);
            _updateService = new UpdateService(_logger, _configService);
            _appUpdater = new UpdaterApplicationService(_logger);
            _musicService = new MusicService();
            _statusService = new ServerStatusService("server.endore.pl", 4003);

            // Rejestracja zdarzeń UI
            MusicSwitch.Checked += MusicSwitch_Checked;
            MusicSwitch.Unchecked += MusicSwitch_Unchecked;
            SelectLocationButton.Click += SelectLocationButton_Click;
            UpdateButton.Click += UpdateButton_Click;
            PlayButton.Click += PlayButton_Click;

            // Start systemów pobocznych
            LoadConfig();
            InitStatusTimer();
        }

        // --- STATUS SERWERA ---
        private void InitStatusTimer()
        {
            _statusTimer = new DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(10);
            _statusTimer.Tick += async (s, e) => await UpdateServerStatus();
            _statusTimer.Start();
            
            // Pierwsze sprawdzenie zaraz po uruchomieniu
            Task.Run(async () => await UpdateServerStatus());
        }

        private async Task UpdateServerStatus()
        {
            bool isOnline = await _statusService.IsServerOnline();

            await Dispatcher.InvokeAsync(() =>
            {
                if (isOnline)
                {
                    StatusText.Text = "Online";
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(144, 238, 144)); // LightGreen
                    StatusDot.Fill = Brushes.LimeGreen;
                    DotGlow.Color = Colors.LimeGreen;
                    DotGlow.Opacity = 0.8;
                }
                else
                {
                    StatusText.Text = "Offline";
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 99, 71)); // Tomato
                    StatusDot.Fill = Brushes.Red;
                    DotGlow.Color = Colors.Red;
                    DotGlow.Opacity = 0.8;
                }
            });
        }

        // --- OBSŁUGA OKNA I LINKÓW ---
        private void DiscordButton_Click(object sender, RoutedEventArgs e) => OpenUrl("https://discord.gg/XsDRTce");
        private void WikiButton_Click(object sender, RoutedEventArgs e) => OpenUrl("https://wiki.endore.pl/");
        
        private void OpenUrl(string url) {
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            catch (Exception ex) { _logger.LogMessage($"Błąd linku: {ex.Message}"); }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        // --- LOGIKA AKTUALIZACJI ---
        private void MusicSwitch_Checked(object sender, RoutedEventArgs e) => _musicService.PlayMusic();
        private void MusicSwitch_Unchecked(object sender, RoutedEventArgs e) => _musicService.PauseMusic();

        private void SelectLocationButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog { IsFolderPicker = true, Title = "Wybierz folder gry" };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _configService.SetGamePath(dialog.FileName);
                _logger.LogMessage($"Wybrano nowy katalog gry: {dialog.FileName}");
            }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Sprawdzamy profil i ustawiamy flagę ochronną w serwisie
            bool isDev = false;
            if (!string.IsNullOrEmpty(_selectedProfilePath) && File.Exists(_selectedProfilePath))
            {
                try 
                {
                    var json = File.ReadAllText(_selectedProfilePath);
                    var settings = JsonConvert.DeserializeObject<SettingsModel>(json);
                    if (settings != null && settings.IsDev)
                    {
                        isDev = true;
                        _logger.LogMessage("Tryb DEV aktywny: Synchronizacja plików będzie chronić Twoje IP/Port/Wersję.");
                    }
                } catch { }
            }

            // Przekazujemy informację o trybie DEV do serwisu aktualizacji
            _updateService.IsDevMode = isDev;

            // 2. Przygotowanie UI
            SetUIEnabled(false);
            UpdateProgressBar.Value = 0;
            ProgressPercentage.Text = "0%";

            var progress = new Progress<double>(value => {
                UpdateProgressBar.Value = value * 100;
                ProgressPercentage.Text = $"{Math.Round(value * 100)}%";
            });

            try 
            {
                // 3. Uruchomienie aktualizacji
                _updateService.SkipClassicUO = SkipClassicUOCheckBox.IsChecked ?? false;
        
                await _updateService.UpdateFiles(progress);
        
                ProgressPercentage.Text = "100% Gotowe!";

                // 4. Pytanie o skrót (tylko raz w historii aplikacji)
                if (!_configService.ShortcutCreated)
                {
                    var result = MessageBox.Show(
                        "Czy chcesz utworzyć skrót do launchera na pulpicie?", 
                        "Skrót na pulpicie", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes) CreateDesktopShortcut();
                    _configService.ShortcutCreated = true;
                }
            }
            catch (Exception ex) 
            {
                _logger.LogMessage($"Błąd: {ex.Message}");
                ProgressPercentage.Text = "Błąd!";
            }
            finally 
            {
                SetUIEnabled(true);
            }
        }

        private void SetUIEnabled(bool enabled)
        {
            UpdateButton.IsEnabled = enabled;
            SelectLocationButton.IsEnabled = enabled;
            PlayButton.IsEnabled = enabled;
        }

        // --- TWORZENIE SKRÓTU (DYNAMIC COM) ---
        private void CreateDesktopShortcut()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string appPath = Process.GetCurrentProcess().MainModule.FileName;
                string shortcutPath = Path.Combine(desktopPath, "Endore Launcher.lnk");

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                var shortcut = shell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = appPath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(appPath);
                shortcut.Description = "Launcher Endore Middle-Earth";
                shortcut.IconLocation = appPath;
                shortcut.Save();

                _logger.LogMessage("Skrót na pulpicie został utworzony.");
            }
            catch (Exception ex)
            {
                _logger.LogMessage($"Błąd tworzenia skrótu: {ex.Message}");
            }
        }

        // --- OBSŁUGA ZMIEŃ ---
        private void OpenProfileManager_Click(object sender, RoutedEventArgs e)
        {
            var manager = new ProfileManagerWindow();
            manager.Owner = this;
            manager.WindowStartupLocation = WindowStartupLocation.CenterOwner;
    
            // Pokazujemy okno jako Dialog (blokuje MainWindow do czasu zamknięcia)
            manager.ShowDialog();

            // Po zamknięciu sprawdzamy, czy w Tagu jest nazwa profilu
            if (manager.Tag != null)
            {
                string chosenProfile = manager.Tag.ToString();
        
                // Aktualizujemy TextBlock w UI okna głównego
                CurrentProfileName.Text = chosenProfile;
                CurrentProfileName.Foreground = Brushes.LimeGreen; // Opcjonalnie: zmiana na zielony też tutaj
        
                // Zapisujemy ścieżkę do wybranego profilu, żeby przycisk GRAJ wiedział co odpalić
                _selectedProfilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles", $"{chosenProfile}.json");
        
                _logger.LogMessage($"Aktywowano profil: {chosenProfile}");
            }
        }

        // --- URUCHAMIANIE GRY ---
        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Sprawdzenie profilu
                if (string.IsNullOrEmpty(_selectedProfilePath) || !File.Exists(_selectedProfilePath))
                {
                    MessageBox.Show("Najpierw wybierz profil w Managerze!", "Brak profilu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 2. Pobieramy dane z naszego profilu (TU JEST KLUCZ DO TWOJEGO PROBLEMU)
                string rawProfileJson = File.ReadAllText(_selectedProfilePath);
                var mySettings = JsonConvert.DeserializeObject<SettingsModel>(rawProfileJson);

                if (mySettings == null || string.IsNullOrEmpty(mySettings.UltimaOnlineDirectory))
                {
                    _logger.LogMessage("BŁĄD: Profil jest pusty lub nie ma ustawionej ścieżki gry.");
                    return;
                }

                // UWAGA: Teraz używamy ścieżki Z PROFILU (mySettings.UltimaOnlineDirectory)
                // a nie globalnej ścieżki launchera (_configService.GamePath)
                string gameBaseDir = mySettings.UltimaOnlineDirectory; 
        
                // 3. UI: Zmiana tekstu i blokada
                PlayButton.Content = "URUCHAMIANIE...";
                SetUIEnabled(false);
                await Task.Delay(500);

                // 4. Ścieżki do plików
                string exePath = Path.Combine(gameBaseDir, "ClassicUO", "ClassicUO.exe");
                string gameSettingsPath = Path.Combine(gameBaseDir, "ClassicUO", "settings.json");

                if (!File.Exists(exePath))
                {
                    _logger.LogMessage($"BŁĄD: Nie znaleziono ClassicUO.exe w {exePath}!");
                    SetUIEnabled(true);
                    PlayButton.Content = "URUCHOM GRĘ";
                    return;
                }

                // 5. Synchronizacja JSON
                _logger.LogMessage("Synchronizacja profilu z ClassicUO...");
                try
                {
                    string decryptedPass = PasswordVault.Decrypt(mySettings.Password);
                    string passForGame = PasswordVault.ToClassicUOPassword(decryptedPass);

                    if (File.Exists(gameSettingsPath))
                    {
                        var gameJsonRaw = File.ReadAllText(gameSettingsPath);
                        JObject gameConfig = JObject.Parse(gameJsonRaw);

                        // --- PODSTAWOWE ---
                        gameConfig["username"] = mySettings.Username;
                        gameConfig["password"] = passForGame;
                        gameConfig["autologin"] = mySettings.Autologin;
                        gameConfig["reconnect"] = mySettings.Reconnect;
                        gameConfig["login_music"] = mySettings.LoginMusic;
                        gameConfig["login_music_volume"] = mySettings.LoginMusicVolume;

                        // --- PLUGINY (NOWOŚĆ) ---
                        if (mySettings.Plugins != null)
                        {
                            gameConfig["plugins"] = JArray.FromObject(mySettings.Plugins);
                        }

                        // --- LOGIKA DEV ---
                        if (mySettings.IsDev)
                        {
                            _logger.LogMessage("Tryb DEV: Pominięto IP/Port/Wersję.");
                        }
                        else
                        {
                            gameConfig["ip"] = mySettings.Ip;
                            gameConfig["port"] = mySettings.Port;
                            gameConfig["ultimaonlinedirectory"] = mySettings.UltimaOnlineDirectory;
                            gameConfig["clientversion"] = mySettings.ClientVersion; // Poprawiłem literówkę z client_version na clientversion
                        }

                        File.WriteAllText(gameSettingsPath, gameConfig.ToString(Formatting.Indented));
                        _logger.LogMessage("Ustawienia zsynchronizowane.");
                    }
                }
                catch (Exception ex) { _logger.LogMessage($"Błąd synchronizacji: {ex.Message}"); }

                // 6. Start gry
                _logger.LogMessage("Odpalanie ClassicUO...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    UseShellExecute = true
                });

                await Task.Delay(3000);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogMessage($"Błąd krytyczny: {ex.Message}");
                SetUIEnabled(true);
                PlayButton.Content = "URUCHOM GRĘ";
            }
        }

        private void LoadConfig() => _configService.LoadConfig();
        private async void Window_Loaded(object sender, RoutedEventArgs e) => await _appUpdater.CheckForAppUpdateAsync();
    }
}