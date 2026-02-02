using System;
using System.IO;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Microsoft.VisualBasic;

namespace AktualizatorEME
{
    public partial class ProfileEditWindow : Window
    {
        private string _lastValidPort = "4003";

        public ProfileEditWindow() { InitializeComponent(); }

        public void LoadProfileData(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return;
                string json = File.ReadAllText(filePath);
                var settings = JsonConvert.DeserializeObject<SettingsModel>(json);

                if (settings != null)
                {
                    ProfileNameBox.Text = Path.GetFileNameWithoutExtension(filePath);
                    LoginBox.Text = settings.Username;
                    PassBox.Password = PasswordVault.Decrypt(settings.Password);
                    PathBox.Text = settings.UltimaOnlineDirectory;
                    DevModeCheck.IsChecked = settings.IsDev;

                    // LOGIKA WERSJI: 
                    // Pobieraj z plików gry TYLKO jeśli:
                    // a) To nie jest tryb DEV
                    // b) Pole wersji w profilu jest puste (pierwsze ładowanie)
                    if (!settings.IsDev || string.IsNullOrEmpty(settings.ClientVersion))
                    {
                        if (!string.IsNullOrEmpty(settings.UltimaOnlineDirectory))
                            UpdateClientVersionFromGameFiles(settings.UltimaOnlineDirectory);
                        else
                            ClientVersionBox.Text = settings.ClientVersion;
                    }
                    else
                    {
                        // Jeśli to DEV i mamy już zapisaną wersję, trzymajmy się jej
                        ClientVersionBox.Text = settings.ClientVersion;
                    }

                    IpBox.Text = settings.Ip;
                    PortBox.Text = settings.Port.ToString();
                    _lastValidPort = settings.Port.ToString();
            
                    AutoLoginCheck.IsChecked = settings.Autologin;
                    ReconnectCheck.IsChecked = settings.Reconnect;
                    LoginMusicCheck.IsChecked = settings.LoginMusic;
                    MusicSlider.Value = settings.LoginMusicVolume;
                    ProfileNameBox.IsEnabled = false; 
                }
            }
            catch (Exception ex) { MessageBox.Show($"Błąd wczytywania: {ex.Message}"); }
        }

        private void DevModeCheck_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Jeśli tryb jest wyłączony, zapytaj o hasło przed włączeniem
            if (DevModeCheck.IsChecked == false)
            {
                string password = Interaction.InputBox("Podaj hasło deweloperskie, aby odblokować edycję parametrów połączenia:", "Autoryzacja Dewelopera", "");

                if (password == "desktop-92") 
                {
                    DevModeCheck.IsChecked = true;
                    MessageBox.Show("Tryb Dewelopera odblokowany.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Błędne hasło!", "Brak dostępu", MessageBoxButton.OK, MessageBoxImage.Stop);
                }
        
                // Zatrzymujemy zdarzenie, żeby WPF nie zmienił stanu checkboxa automatycznie
                e.Handled = true; 
            }
            else
            {
                // Jeśli tryb jest już włączony, pozwól go wyłączyć bez hasła
                DevModeCheck.IsChecked = false;
                e.Handled = true;
            }
        }

        private void BrowsePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Wybierz główny folder gry",
                InitialDirectory = string.IsNullOrEmpty(PathBox.Text) ? "C:\\" : PathBox.Text
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string mainPath = dialog.FileName;
                PathBox.Text = mainPath;
        
                // Jeśli pole wersji jest puste, to znaczy że tworzymy profil 
                // - wtedy pobieramy wersję nawet w DEV. 
                // Jeśli nie jest puste i jest DEV - nie ruszamy (developer wie co robi).
                if (DevModeCheck.IsChecked == false || string.IsNullOrEmpty(ClientVersionBox.Text))
                {
                    UpdateClientVersionFromGameFiles(mainPath);
                }
            }
        }

        private void ShowPassBtn_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PassVisibleBox.Text = PassBox.Password;
            PassBox.Visibility = Visibility.Collapsed;
            PassVisibleBox.Visibility = Visibility.Visible;
            ShowPassBtn.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 126, 0));
        }

        private void ShowPassBtn_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PassBox.Visibility = Visibility.Visible;
            PassVisibleBox.Visibility = Visibility.Collapsed;
            ShowPassBtn.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136));
        }

        private void UpdateClientVersionFromGameFiles(string mainGamePath)
        {
            try
            {
                string settingsPath = Path.Combine(mainGamePath, "ClassicUO", "settings.json");
                if (File.Exists(settingsPath))
                {
                    string jsonRaw = File.ReadAllText(settingsPath);
                    var gameSettings = Newtonsoft.Json.Linq.JObject.Parse(jsonRaw);
            
                    if (gameSettings["clientversion"] != null)
                    {
                        ClientVersionBox.Text = gameSettings["clientversion"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd odczytu wersji: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try 
            {
                // 1. Walidacja pól
                if (!ValidateForm())
                {
                    return; // Przerwij zapis, jeśli walidacja nie przeszła
                }

                int portValue;
                if (!int.TryParse(PortBox.Text, out portValue)) 
                {
                    MessageBox.Show($"Błędny port. Przywrócono: {_lastValidPort}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PortBox.Text = _lastValidPort;
                    portValue = int.Parse(_lastValidPort);
                }

                var settings = new SettingsModel
                {
                    Username = LoginBox.Text,
                    Password = PasswordVault.Encrypt(PassBox.Password),
                    Ip = IpBox.Text,
                    Port = portValue,
                    UltimaOnlineDirectory = PathBox.Text,
                    ClientVersion = ClientVersionBox.Text,
                    Autologin = AutoLoginCheck.IsChecked ?? false,
                    Reconnect = ReconnectCheck.IsChecked ?? false,
                    LoginMusic = LoginMusicCheck.IsChecked ?? false,
                    LoginMusicVolume = (int)MusicSlider.Value,
                    IsDev = DevModeCheck.IsChecked ?? false 
                };

                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                string profilesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");
                Directory.CreateDirectory(profilesFolder);
        
                string safeName = string.Join("_", ProfileNameBox.Text.Split(Path.GetInvalidFileNameChars()));
                string filePath = Path.Combine(profilesFolder, $"{safeName}.json");

                File.WriteAllText(filePath, json);
                MessageBox.Show("Profil zapisany!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
            }
            catch (Exception ex) { MessageBox.Show($"Błąd zapisu: {ex.Message}"); }
        }

        private bool ValidateForm()
        {
            System.Collections.Generic.List<string> errors = new System.Collections.Generic.List<string>();

            if (string.IsNullOrWhiteSpace(ProfileNameBox.Text)) errors.Add("- Nazwa profilu");
            if (string.IsNullOrWhiteSpace(PathBox.Text)) errors.Add("- Ścieżka do gry");
            if (string.IsNullOrWhiteSpace(ClientVersionBox.Text)) errors.Add("- Wersja klienta");
            if (string.IsNullOrWhiteSpace(LoginBox.Text)) errors.Add("- Login");
            if (string.IsNullOrWhiteSpace(PassBox.Password)) errors.Add("- Hasło");
            if (string.IsNullOrWhiteSpace(IpBox.Text)) errors.Add("- Adres serwera");
            if (string.IsNullOrWhiteSpace(PortBox.Text)) errors.Add("- Port");

            if (errors.Count > 0)
            {
                string message = "Aby zapisać profil, uzupełnij następujące pola:\n" + string.Join("\n", errors);
                MessageBox.Show(message, "Brakujące dane", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
    }
}