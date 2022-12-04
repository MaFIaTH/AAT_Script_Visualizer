using System.IO;
using System.Windows;
using System.Windows.Controls;
using static AAT_Script_Visualizer.ProjectSystem;
using static AAT_Script_Visualizer.DirectoryPath;

namespace AAT_Script_Visualizer
{
    public partial class TermTabPage : UserControl
    {
        public TermTabPage()
        {
            InitializeComponent();
        }

        private void ReadTerms(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show($"The system tried to load file in\n" +
                                $"{path}\n" +
                                $"but the file does not exist.\n" +
                                $"Aborting...",
                    "Error: ReadTerms",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string termsText = File.ReadAllText(path);
            termsBox.Text = termsText;
        }

        private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            ReadTerms(currentTermsPath);
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveTerms();
        }

        private void SaveTerms()
        {
            if (!File.Exists(currentTermsPath))
            {
                MessageBox.Show($"The system tried to save file in\n" +
                                $"{currentTermsPath}\n" +
                                $"but the file does not exist.\n" +
                                $"Aborting...",
                    "Error: SaveTerms",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string termsBoxText = termsBox.Text;
            File.WriteAllText(currentTermsPath, termsBoxText);
        }

        private void ResetButton_OnClick(object sender, RoutedEventArgs e)
        {
            ReadTerms(backupTermsPath);
            SaveTerms();
        }

        private void TermTabPage_OnGotFocus(object sender, RoutedEventArgs e)
        {
            ReadTerms(currentTermsPath);
        }
    }
}