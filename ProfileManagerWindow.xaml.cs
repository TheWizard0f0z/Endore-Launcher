using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            if (edit.ShowDialog() == true || true)
            {
                LoadProfiles();
            }
        }

        public void LoadProfiles()
        {
            ProfilesList.Children.Clear();
            lastSelectedTextBlock = null; // Resetujemy przy przeładowaniu listy

            if (!Directory.Exists(profilesPath))
                Directory.CreateDirectory(profilesPath);

            var files = Directory.GetFiles(profilesPath, "*.json");

            foreach (var file in files)
            {
                var profileName = Path.GetFileNameWithoutExtension(file);
                AddProfileTile(profileName, file);
            }
        }

        private void AddProfileTile(string name, string filePath)
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
            
            // Tworzymy TextBlock dla nazwy
            TextBlock txt = new TextBlock
            {
                Text = name,
                Foreground = Brushes.White,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };

            StackPanel btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            // Przycisk WYBIERZ - teraz przekazuje też TextBlock do metody
            Button btnSelect = new Button { Content = "WYBIERZ", Width = 60, Margin = new Thickness(5, 0, 5, 0), Tag = filePath };
            btnSelect.Click += (s, e) => {
                SelectProfile_Logic(txt, filePath);
            };

            Button btnEdit = new Button { Content = "EDYTUJ", Width = 60, Margin = new Thickness(5, 0, 5, 0), Tag = filePath };
            btnEdit.Click += EditProfile_Click;

            Button btnDelete = new Button { Content = "USUŃ", Width = 60, Margin = new Thickness(5, 0, 5, 0), Foreground = Brushes.Red, Tag = filePath };
            btnDelete.Click += DeleteProfile_Click;

            btnStack.Children.Add(btnSelect);
            btnStack.Children.Add(btnEdit);
            btnStack.Children.Add(btnDelete);

            grid.Children.Add(txt);
            grid.Children.Add(btnStack);
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

            edit.ShowDialog();
            LoadProfiles();
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