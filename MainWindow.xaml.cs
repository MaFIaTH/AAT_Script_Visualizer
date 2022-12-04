using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.WindowsAPICodePack.Dialogs;
using static AAT_Script_Visualizer.DirectoryPath;
using static AAT_Script_Visualizer.ProjectSystem;
using static  AAT_Script_Visualizer.FileSystem;
using Application = System.Windows.Application;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace AAT_Script_Visualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static string version = "1.0.0";
        ProjectSystem ps = new ProjectSystem();
        public MainWindow()
        {
            InitializeComponent();
            ResizeMode = ResizeMode.CanResize;
        }

        private void CommandBinding_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            string commandName = (e.Command as RoutedUICommand)?.Name;
            string[] prohibitUntilOpen = { "Save", "Save As", "Export As" };
            if (prohibitUntilOpen.Any(s => commandName.Contains(s)) && !ps.isProjectOpened)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        private void FileCommand(object sender, ExecutedRoutedEventArgs e)
        {
            string name = e.OriginalSource is MenuItem
                ? (e.OriginalSource as MenuItem)?.Name
                : (e.Command as RoutedUICommand)?.Name;
            if (name.Equals("save", StringComparison.OrdinalIgnoreCase))
            {
                SaveProject(String.Empty);
            }
            if (name.Equals("Save As") || name.Equals("saveAs"))
            {
                string saveDirectory = FileDialog("Save As");
                if (String.IsNullOrEmpty(saveDirectory))
                {
                    return;
                }
                SaveProject(saveDirectory);
            }
            if (name.Equals("Export As") || name.Equals("exportAs"))
            {
                string exportDirectory = FileDialog("Export As");
                if (String.IsNullOrEmpty(exportDirectory))
                {
                    return;
                }
                ExportProject(exportDirectory);
                
            }
            if (name.Equals("New Project") || name.Equals("newProject"))
            {
                if (ps.isProjectOpened)
                {
                    MessageBoxResult result1 = MessageBox.Show(
                        "You currently have a project opened and you will lose your progress if you proceed.\n" +
                        "Would you like to continue?",
                        "Warning",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    switch (result1)
                    {
                        case MessageBoxResult.Yes:
                            break;
                        case MessageBoxResult.No:
                            return;
                    }
                }
                string originalDirectory = FileDialog("Open Original File");
                if (String.IsNullOrEmpty(originalDirectory))
                {
                    return;
                }
                currentOriginalPath = originalDirectory;
                MessageBoxResult result2 = MessageBox.Show(
                    "Do you have a translated script file?\nIf not, the tool will use original script instead.",
                    "Notice",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                switch (result2)
                {
                    case MessageBoxResult.Yes:
                        string translatedDirectory = FileDialog("Open Translated File");
                        if (String.IsNullOrEmpty(translatedDirectory))
                        {
                            return;
                        } 
                        currentTranslatedPath = translatedDirectory;
                        currentProjectPath = FileDialog(name);
                        CreateProject();
                        OpenProject();
                        AddTempListDataList();
                        break;
                    case MessageBoxResult.No:
                        currentTranslatedPath = currentOriginalPath;
                        currentProjectPath = FileDialog(name);
                        CreateProject();
                        OpenProject();
                        AddTempListDataList();
                        break;
                    case MessageBoxResult.Cancel:
                        return;
                }
            }
            if (name.Equals("Open Project") || name.Equals("openProject"))
            {
                if (ps.isProjectOpened)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "You currently have a project opened and you will lose your progress if you proceed.\n" +
                        "Would you like to continue?",
                        "Warning",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            break;
                        case MessageBoxResult.No:
                            return;
                    }
                }
                string directory = FileDialog(name);
                if (directory == null)
                {
                    return;
                }
                currentProjectPath = directory;
                OpenProject();
                AddTempListDataList();
            }
        }
        
        private void FontChanger_OnClick(object sender, RoutedEventArgs e)
        {
            FontDialog fd = new FontDialog();
            var result = fd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                FontFamily fontFamily = new FontFamily(fd.Font.Name);
                FontWeight fontWeight = fd.Font.Bold ? FontWeights.Bold : FontWeights.Regular;
                double fontSize = fd.Font.Size * 96.0 / 72.0;
                visualizerTabPage.Resources["visualizerFontSize"] = fontSize;
                visualizerTabPage.Resources["visualizerFontFamily"] = fontFamily;
                visualizerTabPage.Resources["visualizerFontWeight"] = fontWeight;
            }
        }

        private void CheckWiki_OnClick(object sender, RoutedEventArgs e)
        {
            var destinationurl = "https://github.com/MaFIaTH/AAT_Script_Visualizer/wiki";
            var sInfo = new ProcessStartInfo(destinationurl)
            {
                UseShellExecute = true,
            };
            Process.Start(sInfo);
        }

        private void CheckVersion_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                $"Current Version: {version}\n" +
                $"Developed by MaFIa_TH (Marx Duponts)\n" +
                $"Tip: Check Wiki for more information and tutorial.",
                "Version",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (ps.isProjectOpened)
            {
                MessageBoxResult result = MessageBox.Show(
                    "You currently have a project opened and you will lose your progress if you proceed.\n" +
                    "Would you like to continue?",
                    "Warning",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        break;
                    case MessageBoxResult.No:
                        e.Cancel = true;
                        return;
                }
            }
        }

        
    }

    public static class FileSystem
    {
        public static string FileDialog(string operation)
        {
            CommonOpenFileDialog openDialog = new CommonOpenFileDialog();
            SaveFileDialog saveDialog = new SaveFileDialog();
            string recentDirectoryPath = String.IsNullOrEmpty(recentDirectory)
                ? Directory.GetCurrentDirectory()
                : recentDirectory;
            openDialog.InitialDirectory = recentDirectoryPath;
            saveDialog.InitialDirectory = recentDirectoryPath;
            bool isOpen = false;
            string title = "Test";
            bool folderPicker = false;
            CommonFileDialogFilter openFilter = new CommonFileDialogFilter();
            string saveFilter = String.Empty;
            string defaultFileName = String.Empty;
            string defaultExtension = String.Empty;
            bool forceExtension = true;
            switch (operation)
            {
                case "New Project":
                case "newProject":
                    isOpen = false;
                    defaultFileName = "Project1";
                    defaultExtension = ".proj";
                    forceExtension = true;
                    saveFilter = "Project File | *.proj";
                    title = "Select directory and name for your new project";
                    break;
                case "Open Project":
                case "openProject":
                    isOpen = true;
                    title = "Select your project file";
                    folderPicker = false;
                    openFilter = new CommonFileDialogFilter("Project", "proj");
                    break;
                case "Save As":
                case "saveAs":
                    isOpen = false;
                    title = "Select your save directory";
                    defaultFileName = "Project1";
                    defaultExtension = ".proj";
                    forceExtension = true;
                    saveFilter = "Project File | *.proj";
                    break;
                case "Export As":
                case "exportAs":
                    isOpen = false;
                    title = "Select your export directory";
                    defaultFileName = "Project1";
                    defaultExtension = ".txt";
                    forceExtension = true;
                    saveFilter = "Text Document | *.txt";
                    break;
                case "Open Original File":
                    isOpen = true;
                    title = "Select your original script file";
                    folderPicker = false;
                    openFilter = new CommonFileDialogFilter("Text Document", "txt");
                    break;
                case "Open Translated File":
                    isOpen = true;
                    title = "Select your translated script file";
                    folderPicker = false;
                    openFilter = new CommonFileDialogFilter("Text Document", "txt");
                    break;
            }

            if (isOpen)
            {
                openDialog.IsFolderPicker = folderPicker;
                openDialog.Title = title;
                if (!openDialog.IsFolderPicker)
                {
                    openDialog.Filters.Add(openFilter);
                }
                if (openDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    recentDirectory = openDialog.FileName;
                    return openDialog.FileName;
                }
                return null;
            }
            else
            {
                saveDialog.OverwritePrompt = true;
                if (!String.IsNullOrEmpty(saveFilter))
                {
                    saveDialog.Filter = saveFilter;
                }
                saveDialog.FileName = defaultFileName;
                saveDialog.DefaultExt = defaultExtension;
                saveDialog.AddExtension = forceExtension;
                if (saveDialog.ShowDialog() == true)
                {
                    recentDirectory = openDialog.FileName;
                    return saveDialog.FileName;
                }
                return null;

            }
        }
    }
    
    public static class CustomCommand
    {
        public static readonly RoutedUICommand newProject = new RoutedUICommand("New Project", "New Project",
            typeof(CustomCommand),
            new InputGestureCollection() { new KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Shift) });

        public static readonly RoutedUICommand openProject = new RoutedUICommand("Open Project", "Open Project",
            typeof(CustomCommand),
            new InputGestureCollection() { new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift) });

        public static readonly RoutedUICommand save = new RoutedUICommand("Save", "Save",
            typeof(CustomCommand), new InputGestureCollection() { new KeyGesture(Key.S, ModifierKeys.Control) });

        public static readonly RoutedUICommand saveAs = new RoutedUICommand("Save As", "Save As",
            typeof(CustomCommand),
            new InputGestureCollection() { new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift) });

        public static readonly RoutedUICommand exportAs = new RoutedUICommand("Export As", "Export As",
            typeof(CustomCommand),
            new InputGestureCollection() { new KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Shift) });
        
    }
}
