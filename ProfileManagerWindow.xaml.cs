using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;

namespace AktualizatorEME
{
    public partial class ProfileManagerWindow : Window
    {
        private string profilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");
        
        // Zmienna przechowująca referencję do aktualnie "zazielonionej" nazwy
        private TextBlock lastSelectedTextBlock = null;

        public ProfileManagerWindow()
        {
            InitializeComponent();
            LoadProfiles();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void CreateNewProfile_Click(object sender, RoutedEventArgs e)
        {
            var edit = new ProfileEditWindow();
            edit.Owner = this;
            edit.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            edit.ProfileNameBox.IsEnabled = true;
            if (edit.ShowDialog() == true)
            {
                LoadProfiles();
            }
        }

        public void LoadProfiles()
        {
            ProfilesList.Children.Clear();
            lastSelectedTextBlock = null;

            if (!Directory.Exists(profilesPath))
                Directory.CreateDirectory(profilesPath);

            var files = Directory.GetFiles(profilesPath, "*.json");

            foreach (var file in files)
            {
                var profileName = Path.GetFileNameWithoutExtension(file);
        
                // --- NOWA LOGIKA: Sprawdzanie czy profil jest DEV ---
                bool isDev = false;
                try 
                {
                    string json = File.ReadAllText(file);
                    var settings = JsonConvert.DeserializeObject<SettingsModel>(json);
                    if (settings != null) isDev = settings.IsDev;
                }
                catch { /* ignorujemy błędy odczytu pojedynczych plików */ }
                // ----------------------------------------------------

                AddProfileTile(profileName, file, isDev);
            }
        }

        private void AddProfileTile(string name, string filePath, bool isDev)
        {
            Border card = new Border
            {
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#1A1A1A"),
                BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#444444"),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(10)
            };

            Grid grid = new Grid();

            // 1. Tworzymy pionowy kontener na nazwę i ewentualny znaczek DEV
            StackPanel nameContainer = new StackPanel 
            { 
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left 
            };

            TextBlock txt = new TextBlock
            {
                Text = name,
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold // Opcjonalnie pogrubienie, by lepiej wyglądało
            };
            nameContainer.Children.Add(txt);

            // 2. Jeśli profil to DEV, tworzymy badge i dodajemy go POD tekstem
            if (isDev)
            {
                Border devBadge = new Border
                {
                    Background = Brushes.Orange,
                    Width = 35,
                    Height = 16,
                    Margin = new Thickness(0, 4, 0, 0), // Margines od góry, żeby nie dotykał nazwy
                    CornerRadius = new CornerRadius(2),
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                devBadge.Child = new TextBlock
                {
                    Text = "DEV",
                    Foreground = Brushes.Black,
                    FontWeight = FontWeights.Bold,
                    FontSize = 9,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                nameContainer.Children.Add(devBadge);
            }

            // 3. Panel przycisków (Pionowy kontener główny dla przycisków)
StackPanel mainBtnContainer = new StackPanel 
{ 
    HorizontalAlignment = HorizontalAlignment.Right, 
    VerticalAlignment = VerticalAlignment.Center 
};

// Górny rząd: WYBIERZ i EDYTUJ
StackPanel topRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };

Button btnSelect = new Button { Content = "WYBIERZ", Width = 65, Height = 22, Margin = new Thickness(5, 0, 0, 0), Tag = filePath };
btnSelect.Click += (s, e) => { SelectProfile_Logic(txt, filePath); };

Button btnEdit = new Button { Content = "EDYTUJ", Width = 65, Height = 22, Margin = new Thickness(5, 0, 0, 0), Tag = filePath };
btnEdit.Click += EditProfile_Click;

topRow.Children.Add(btnSelect);
topRow.Children.Add(btnEdit);

// Dolny rząd: USUŃ (wyrównany do prawej)
Button btnDelete = new Button 
{ 
    Content = "USUŃ", 
    Width = 65, 
    Height = 22,
    Foreground = Brushes.Red, 
    Tag = filePath,
    HorizontalAlignment = HorizontalAlignment.Right,
    Margin = new Thickness(5, 0, 0, 0)
};
btnDelete.Click += DeleteProfile_Click;

// Składanie całości
mainBtnContainer.Children.Add(topRow);
mainBtnContainer.Children.Add(btnDelete);

// 4. Dodajemy kontenery do głównego Grida
grid.Children.Add(nameContainer);
grid.Children.Add(mainBtnContainer);
    
            card.Child = grid;
            ProfilesList.Children.Add(card);
        }

        // Nowa logika wyboru profilu
        private void SelectProfile_Logic(TextBlock targetTxt, string filePath)
        {
            // 1. Resetujemy kolor poprzedniego wyboru
            if (lastSelectedTextBlock != null)
            {
                lastSelectedTextBlock.Foreground = Brushes.White;
            }

            // 2. Ustawiamy nowy kolor na LimeGreen
            targetTxt.Foreground = Brushes.LimeGreen;
            lastSelectedTextBlock = targetTxt;

            // 3. Zapisujemy nazwę profilu w Tagu okna (MainWindow to odczyta)
            string profileName = Path.GetFileNameWithoutExtension(filePath);
            this.Tag = profileName;

            // Opcjonalnie: MessageBox.Show($"Profil {profileName} ustawiony jako aktywny!");
        }

        private void SelectProfile_Click(object sender, RoutedEventArgs e)
        {
            // Ta metoda zostaje pusta lub można ją usunąć, jeśli używamy SelectProfile_Logic powyżej
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            var path = (sender as Button)?.Tag?.ToString();
            var edit = new ProfileEditWindow();
            edit.Owner = this;
            edit.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (!string.IsNullOrEmpty(path))
            {
                edit.LoadProfileData(path);
            }

            if (edit.ShowDialog() == true)
            {
                LoadProfiles();
            }
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            var path = (sender as Button).Tag.ToString();
            if (MessageBox.Show($"Czy na pewno usunąć profil {Path.GetFileNameWithoutExtension(path)}?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                File.Delete(path);
                LoadProfiles();
            }
        }
    }
}