using System;
using System.IO;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;

namespace AktualizatorEME
{
    public partial class ProfileEditWindow : Window
    {
        public ProfileEditWindow()
        {
            InitializeComponent();
        }

        public void LoadProfileData(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return;

                // Czyścimy pole wersji przed wczytaniem
                ClientVersionBox.Text = "";

                string json = File.ReadAllText(filePath);
                var settings = JsonConvert.DeserializeObject<SettingsModel>(json);

                if (settings != null)
                {
                    // Wypełniamy pola danymi z JSONa
                    ProfileNameBox.Text = Path.GetFileNameWithoutExtension(filePath);
                    
                    LoginBox.Text = settings.Username;
                    PassBox.Password = PasswordVault.Decrypt(settings.Password); // Hasło deszyfrujemy
                    ClientVersionBox.Text = settings.ClientVersion;

                    PathBox.Text = settings.UltimaOnlineDirectory;

                    // WYMUSZENIE AKTUALIZACJI WERSJI Z PLIKÓW GRY
                    if (!string.IsNullOrEmpty(settings.UltimaOnlineDirectory))
                    {
                        UpdateClientVersionFromGameFiles(settings.UltimaOnlineDirectory);
                    }
                    else
                    {
                        // Jeśli w profilu nie ma ścieżki, weź wersję zapisaną w JSON profilu
                        ClientVersionBox.Text = settings.ClientVersion;
                    }

                    IpBox.Text = settings.Ip;
                    PortBox.Text = settings.Port.ToString();
            
                    AutoLoginCheck.IsChecked = settings.Autologin;
                    ReconnectCheck.IsChecked = settings.Reconnect;
                    LoginMusicCheck.IsChecked = settings.LoginMusic;
                    MusicSlider.Value = settings.LoginMusicVolume;
            
                    // Blokujemy edycję nazwy profilu, żeby użytkownik nie stworzył kopii zamiast edycji
                    ProfileNameBox.IsEnabled = false; 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wczytywania profilu: {ex.Message}");
            }
        }

        private void BrowsePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Wybierz główny folder gry (np. Endore)",
                InitialDirectory = string.IsNullOrEmpty(PathBox.Text) ? "C:\\" : PathBox.Text
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string mainPath = dialog.FileName;
                PathBox.Text = mainPath;

                // AUTOMATYCZNA AKTUALIZACJA WERSJI - teraz wywoływana od razu po wyborze
                UpdateClientVersionFromGameFiles(mainPath);
            }
        }

        // Gdy naciskasz przycisk - pokaż hasło
        private void ShowPassBtn_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PassVisibleBox.Text = PassBox.Password;
            PassBox.Visibility = Visibility.Collapsed;
            PassVisibleBox.Visibility = Visibility.Visible;
            ShowPassBtn.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 126, 0)); // Zmień kolor na pomarańczowy
        }

        // Gdy puszczasz przycisk - ukryj hasło
        private void ShowPassBtn_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PassBox.Visibility = Visibility.Visible;
            PassVisibleBox.Visibility = Visibility.Collapsed;
            ShowPassBtn.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136)); // Wróć do szarego
        }

        private void UpdateClientVersionFromGameFiles(string mainGamePath)
        {
            try
            {
                string settingsPath = Path.Combine(mainGamePath, "ClassicUO", "settings.json");
                if (File.Exists(settingsPath))
                {
                    string jsonRaw = File.ReadAllText(settingsPath);
                    // Dynamicznie parsujemy JSONa, żeby wyciągnąć tylko clientversion
                    var gameSettings = Newtonsoft.Json.Linq.JObject.Parse(jsonRaw);
            
                    if (gameSettings["clientversion"] != null)
                    {
                        ClientVersionBox.Text = gameSettings["clientversion"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                // Cichy błąd, żeby nie denerwować użytkownika przy wyborze folderu
                System.Diagnostics.Debug.WriteLine($"Błąd odczytu wersji: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try 
            {
                string rawPass = PassBox.Password;
                
                // Sprawdzamy czy Port jest liczbą, jeśli nie - dajemy domyślny 4003
                if (!int.TryParse(PortBox.Text, out int portValue)) portValue = 4003;

                var settings = new SettingsModel
                {
                    Username = LoginBox.Text,
                    Password = PasswordVault.Encrypt(PassBox.Password), // Szyfrujemy hasło przed zapisem do JSONa
                    Ip = IpBox.Text,
                    Port = portValue,
                    UltimaOnlineDirectory = PathBox.Text,
                    ClientVersion = ClientVersionBox.Text,
                    Autologin = AutoLoginCheck.IsChecked ?? false,
                    Reconnect = ReconnectCheck.IsChecked ?? false,
                    LoginMusic = LoginMusicCheck.IsChecked ?? false,
                    LoginMusicVolume = (int)MusicSlider.Value
                };

                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        
                // Tworzymy folder Profiles jeśli nie istnieje
                string profilesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");
                Directory.CreateDirectory(profilesFolder);

                // Zapisujemy pod nazwą profilu (np. MojProfil.json)
                string safeName = string.Join("_", ProfileNameBox.Text.Split(Path.GetInvalidFileNameChars()));
                string filePath = Path.Combine(profilesFolder, $"{safeName}.json");

                File.WriteAllText(filePath, json);

                MessageBox.Show($"Profil '{ProfileNameBox.Text}' został zapisany!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas zapisywania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}