using System;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using static AAT_Script_Visualizer.DirectoryPath;
using static AAT_Script_Visualizer.ProjectSystem;

namespace AAT_Script_Visualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static string version = "1.0.0";
        public MainWindow()
        {
            InitializeComponent();
            ResizeMode = ResizeMode.CanResize;
        }

        private string FileDialog(string operation)
        {
            CommonOpenFileDialog openDialog = new CommonOpenFileDialog();
            SaveFileDialog saveDialog = new SaveFileDialog();
            openDialog.InitialDirectory = Directory.GetCurrentDirectory();
            saveDialog.InitialDirectory = Directory.GetCurrentDirectory();
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
                    folderPicker = true;
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
                    folderPicker = true;
                    break;
                case "Export As":
                case "exportAs":
                    isOpen = false;
                    title = "Select your export directory";
                    folderPicker = true;
                    break;
                case "Open Original File":
                    isOpen = true;
                    title = "Select your original script file";
                    folderPicker = false;
                    openFilter = new CommonFileDialogFilter("Text", "txt");
                    break;
                case "Open Translated File":
                    isOpen = true;
                    title = "Select your translated script file";
                    folderPicker = false;
                    openFilter = new CommonFileDialogFilter("Text", "txt");
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
                    return saveDialog.FileName;
                }

                return null;

            }

            return null;
        }

        private void CommandBinding_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void FileCommand(object sender, ExecutedRoutedEventArgs e)
        {
            string name = e.OriginalSource is MenuItem
                ? (e.OriginalSource as MenuItem)?.Name
                : (e.Command as RoutedUICommand)?.Name;
            if (name.Equals("save", StringComparison.OrdinalIgnoreCase))
            {
                
            }
            if (name == "New Project" || name == "newProject")
            {
                string originalDirectory = FileDialog("Open Original File");
                if (String.IsNullOrEmpty(originalDirectory))
                {
                    return;
                }
                currentOriginalPath = originalDirectory;
                MessageBoxResult result = MessageBox.Show(
                    "Do you have a translated script file?\nIf not, the tool will use original script instead.",
                    "Notice",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                switch (result)
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
                        break;
                }
            }
            else
            {
                string directory = FileDialog(name);
                if (directory != null)
                {
                    if (name == "Open Project" || name == "openProject")
                    {
                        currentProjectPath = directory;
                        OpenProject();
                        AddTempListDataList();
                    }
                }
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
