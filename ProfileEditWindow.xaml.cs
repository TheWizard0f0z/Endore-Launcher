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

                string json = File.ReadAllText(filePath);
                var settings = JsonConvert.DeserializeObject<SettingsModel>(json);

                if (settings != null)
                {
                    // Wypełniamy pola danymi z JSONa
                    ProfileNameBox.Text = Path.GetFileNameWithoutExtension(filePath);
                    LoginBox.Text = settings.Username;
            
                    // Hasło deszyfrujemy (pamiętaj, że w Save_Click je szyfrowaliśmy!)
                    PassBox.Password = settings.Password;
            
                    PathBox.Text = settings.UltimaOnlineDirectory;
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

                // Automat szukający ClassicUO.exe
                string expectedCUOPath = Path.Combine(mainPath, "ClassicUO", "ClassicUO.exe");
                /*
                if (File.Exists(expectedCUOPath))
                {
                    ClassicUOPathBox.Text = expectedCUOPath;
                }
                else
                {
                    MessageBox.Show("Ustawiono folder główny, ale nie odnaleziono automatycznie ClassicUO.exe w podfolderze \\ClassicUO\\.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                */
            }
        }

        private void BrowseCUOExe_Click(object sender, RoutedEventArgs e)
        {
            /*
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = false,
                Title = "Wybierz plik ClassicUO.exe",
                Filters = { new CommonFileDialogFilter("Pliki wykonywalne", "*.exe") }
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ClassicUOPathBox.Text = dialog.FileName;
            }
            */
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

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try 
            {
                string rawPass = PassBox.Password;
                MessageBox.Show($"Zapisuję hasło: '{rawPass}' Długość: {rawPass.Length}");
                
                // Sprawdzamy czy Port jest liczbą, jeśli nie - dajemy domyślny 4003
                if (!int.TryParse(PortBox.Text, out int portValue)) portValue = 4003;

                var settings = new SettingsModel
                {
                    Username = LoginBox.Text,
                    Password = PassBox.Password, // Zapisujemy czysty tekst
                    Ip = IpBox.Text,
                    Port = portValue,
                    UltimaOnlineDirectory = PathBox.Text,
                    ClientVersion = "7.0.40.0",
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