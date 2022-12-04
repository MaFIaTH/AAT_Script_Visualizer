using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using static AAT_Script_Visualizer.DirectoryPath;
using static AAT_Script_Visualizer.VisualizerTabPage;
using static AAT_Script_Visualizer.AppTempData;

namespace AAT_Script_Visualizer
{
    public class AppTempData
    {
        public static List<string> tempTranslatedLines = new List<string>();
        public static List<string> tempOriginalLines = new List<string>();
        public static List<string> tempTranslatedIndex = new List<string>();
        public static List<string> tempOriginalIndex = new List<string>();
        public static List<ListData> translatedListDataList = new List<ListData>();
        public static List<ListData> originalListDataList = new List<ListData>();
        public static List<string> tempModifiedString= new List<string>();
        public static List<string> tempAddedString = new List<string>();
        public static List<string> tempRemovedString = new List<string>();
        public static int topOriginalBlockIndex = -1;
        public static int bottomOriginalBlockIndex = -1;
        public static int topTranslatedBlockIndex = -1;
        public static int bottomTranslatedBlockIndex = -1;
    }
    public class DirectoryPath
    {
        public static string currentProjectPath { get; set; }
        public static string currentOriginalPath { get; set; }
        public static string currentTranslatedPath { get; set; }
        
        public static string recentDirectory { get; set; }

        public static string currentTermsPath = Directory.GetCurrentDirectory() + @"\Terms.txt";
        public static string backupTermsPath = Directory.GetCurrentDirectory() + @"\Terms.backup";
    }
    public class ProjectSystem : INotifyPropertyChanged
    {
        private static bool _isProjectOpened = false;
        public bool isProjectOpened
        {
            get => _isProjectOpened;
            set { _isProjectOpened = value; OnPropertyChanged("isProjectOpened"); }
        }
        #region Create Project
        public static void CreateProject()
        {
            var projectFile = File.Create(currentProjectPath);
            projectFile.Close();
            string projectFileDirectory = Directory.GetCurrentDirectory() + @"\Project File\" +
                                          Path.GetFileNameWithoutExtension(currentProjectPath);
            Directory.CreateDirectory(projectFileDirectory);
            string newOriginalPath = projectFileDirectory + @"\" +
                                     Path.GetFileNameWithoutExtension(currentProjectPath) + ".orig";
            string newTranslatedPath = projectFileDirectory + @"\" +
                                     Path.GetFileNameWithoutExtension(currentProjectPath) + ".tran";
            if (File.Exists(newOriginalPath) || File.Exists(newTranslatedPath))
            {
                File.Delete(newOriginalPath);
                File.Delete(newTranslatedPath);
            }
            File.Copy(currentOriginalPath, newOriginalPath);
            File.Copy(currentTranslatedPath, newTranslatedPath);
            currentOriginalPath = newOriginalPath;
            currentTranslatedPath = newTranslatedPath;
            string[] versionAppend = { MainWindow.version };
            string[] directoryAppend =
                { $"currentOriginalPath={currentOriginalPath}", $"currentTranslatedPath={currentTranslatedPath}" };
            using (StreamWriter sw = File.AppendText(projectFile.Name))
            {
                sw.WriteLine("<Version>");
                foreach (string s in versionAppend)
                {
                    sw.WriteLine(s);
                }
                sw.WriteLine("</Version>");
                sw.WriteLine();
                sw.WriteLine("<Directory>");
                foreach (string s in directoryAppend)
                {
                    sw.WriteLine(s);
                }
                sw.WriteLine("</Directory>");
                sw.WriteLine();
                string[] findLines =
                {
                    @"Text(", @"SetTextColor(", @"SetMessageTime(", @"NewLine(", @"Wait(", @"ClearText(", @"ReadKey(",
                    @"SetSpeakerId(", @"SetTextFlag(", @"Op_2D(", @"Op_0A(", @"Op_08(", @"Op_09(", @"Op_15("
                };
                List<int> matchIndex = new List<int>();
                List<string> matchLines = new List<string>();
                foreach (var match in File.ReadLines(currentOriginalPath)
                             .Select((text, index) => new { text, lineNumber = index + 1 })
                             .Where(x => findLines.Any(x.text.Contains)))
                {
                    matchIndex.Add(match.lineNumber);
                    matchLines.Add(match.text);
                }
                CleanIrrelevantCommand(matchLines, matchIndex, out matchLines, out matchIndex);
                sw.WriteLine("<Original.Lines>");
                foreach (string s in matchLines)
                {
                    sw.WriteLine(s);
                }
                sw.WriteLine("</Original.Lines>");
                sw.WriteLine();
                sw.WriteLine("<Original.Index>");
                foreach (int s in matchIndex)
                {
                    sw.WriteLine(s);
                }
                sw.WriteLine("</Original.Index>");
                sw.WriteLine();
                if (!currentTranslatedPath.Equals(currentOriginalPath))
                {
                    matchIndex.Clear();
                    matchLines.Clear();
                    foreach (var match in File.ReadLines(currentTranslatedPath)
                                 .Select((text, index) => new { text, lineNumber = index + 1 })
                                 .Where(x => findLines.Any(x.text.Contains)))
                    {
                        matchIndex.Add(match.lineNumber);
                        matchLines.Add(match.text);
                    }
                }
                CleanIrrelevantCommand(matchLines, matchIndex, out matchLines, out matchIndex);
                sw.WriteLine("<Translated.Lines>");
                foreach (string s in matchLines)
                {
                    sw.WriteLine(s);
                }
                sw.WriteLine("</Translated.Lines>");
                sw.WriteLine();
                sw.WriteLine("<Translated.Index>");
                foreach (int s in matchIndex)
                {
                    sw.WriteLine(s);
                }
                sw.WriteLine("</Translated.Index>");
                sw.WriteLine();
                sw.WriteLine("<Translated.Modified>");
                sw.WriteLine();
                sw.WriteLine("</Translated.Modified>");
                sw.WriteLine();
                sw.WriteLine("<Translated.Removed>");
                sw.WriteLine();
                sw.WriteLine("</Translated.Removed>");
                sw.WriteLine();
                sw.WriteLine("<Translated.Added>");
                sw.WriteLine();
                sw.WriteLine("</Translated.Added>");
            }
            projectFile.Close();
        }
        public static void CleanIrrelevantCommand(List<string> lineList, List<int> indexList, out List<string> newLineList,
            out List<int> newIndexList)
        {
            string[] splitter = { @"Text(", @"Wait(", @"ClearText(", @"ReadKey(", @"Op_" };
            newLineList = lineList;
            newIndexList = indexList;
            List<int> removeIndex = new List<int>();
            bool removeWait = true;
            foreach (var match in lineList
                         .Select((text, index) => new {text, listIndex = index})
                         .Where(x => splitter.Any(x.text.Contains)))
            {
                if (match.text.Contains(@"Wait(") && removeWait)
                {
                    removeIndex.Add(match.listIndex);
                }
                else if (match.text.Contains(@"ClearText(") || match.text.Contains(@"ReadKey(") || match.text.Contains(@"Op_"))
                {
                    removeWait = true;
                }
                else if (match.text.Contains(@"Text("))
                {
                    removeWait = false;
                }
            }
            foreach (int index in removeIndex.OrderByDescending(i => i))
            {
                newLineList.RemoveAt(index);
                newIndexList.RemoveAt(index);
            }
        }
        #endregion
        
        #region Open Project
        public static void OpenProject()
        {
            string[] directoryMarkup = { @"<Directory>", @"</Directory>" };
            string[] markup = { @"<Translated.Lines>", @"</Translated.Lines>" };
            string[] markup2 = { @"<Original.Lines>", @"</Original.Lines>" };
            string[] markup3 = { @"<Translated.Index>", @"</Translated.Index>" };
            string[] markup4 = { @"<Original.Index>", @"</Original.Index>" };
            string[] modifiedMarkup = { @"<Translated.Modified>", @"</Translated.Modified>" };
            string[] removedMarkup = { @"<Translated.Removed>", @"</Translated.Removed>" };
            string[] addedMarkup = { @"<Translated.Added>", @"</Translated.Added>" };
            List<string> directoryPaths = new List<string>();
            tempTranslatedLines.Clear();
            tempTranslatedIndex.Clear();
            tempOriginalIndex.Clear();
            tempOriginalLines.Clear();
            tempModifiedString.Clear();
            tempRemovedString.Clear();
            tempAddedString.Clear();
            AssignTempList(directoryMarkup, out directoryPaths);
            AssignTempList(markup, out tempTranslatedLines);
            AssignTempList(markup2, out tempOriginalLines);
            AssignTempList(markup3, out tempTranslatedIndex);
            AssignTempList(markup4, out tempOriginalIndex);
            AssignTempList(modifiedMarkup, out tempModifiedString);
            AssignTempList(removedMarkup, out tempRemovedString);
            AssignTempList(addedMarkup, out tempAddedString);
            currentOriginalPath = directoryPaths[0].Split('=')[1];
            currentTranslatedPath = directoryPaths[1].Split('=')[1];
            ProjectSystem ps = new ProjectSystem();
            ps.isProjectOpened = true;
        }
        #endregion

        #region Save Project
        public static void SaveProject(string saveDirectory)
        {
            string[] oldLines = File.ReadAllLines(currentProjectPath);
            List<string> newLines = oldLines.ToList();
            string[] modifiedMarkup = { @"<Translated.Modified>", @"</Translated.Modified>" };
            string[] removedMarkup = { @"<Translated.Removed>", @"</Translated.Removed>" };
            string[] addedMarkup = { @"<Translated.Added>", @"</Translated.Added>" };
            SaveSection(modifiedMarkup, tempModifiedString, ref newLines);
            SaveSection(removedMarkup, tempRemovedString, ref newLines);
            SaveSection(addedMarkup, tempAddedString, ref newLines);
            if (String.IsNullOrEmpty(saveDirectory))
            {
                var test = currentProjectPath;
                File.WriteAllLines(currentProjectPath, newLines);
            }
            else
            {
                File.WriteAllLines(saveDirectory, newLines);
            }
        }
        private static void SaveSection(string[] markup, List<string> stringToAdd, ref List<string> newLines)
        {
            bool startRemoving = false;
            List<int> removeIndex = new List<int>();
            int startIndex = -1;
            int endIndex = -1;
            int counter = 0;
            var test = stringToAdd;
            foreach (var match in newLines.Select((s, i) => new { s, i }))
            {
                if (match.s.Equals(markup[1]))
                {
                    endIndex = match.i - 1;
                    break;
                }
                if (startRemoving)
                {
                    removeIndex.Add(match.i);
                }
                if (match.s.Equals(markup[0]))
                {
                    startRemoving = true;
                    startIndex = match.i + 1;
                }
            }
            foreach (int index in removeIndex.OrderByDescending(i => i))
            {
                newLines.RemoveAt(index);
            }
            for (int i = startIndex; ;i++) //Hope this one doesn't crashhhh lol
            {
                if (counter > stringToAdd.Count - 1)
                {
                    return;
                }
                newLines.Insert(i, stringToAdd[counter]);
                counter++;
            }
        }
        #endregion
        
        #region Export Project

        public static void ExportProject(string exportDirectory)
        {
            List<string> newTranslatedLines = File.ReadAllLines(currentTranslatedPath).ToList();
            List<int> insertIndexes = new List<int>();
            List<string> insertLines = new List<string>();
            List<int> removeIndexes = new List<int>();

            //Apply Modification and Add Remove Tag
            foreach (var item in translatedListDataList.Select((v, i) => new { v, i }))
            {
                if (!item.v.flag.Equals("Added"))
                {
                    insertIndexes.Add(Convert.ToInt32(item.v.linesIndex) - 1);
                    string temp = item.v.flag.Equals("Removed")
                        ? "█Removed█"
                        : $"{item.v.command}({item.v.value});";
                    insertLines.Add(temp);
                }
            }
            
            //Insert Line in New Translated Lines
            foreach (var item in insertIndexes.Select((value, index) => new { value, index }))
            {
                newTranslatedLines[item.value] = insertLines[item.index];
            }
            
            //Apply Addition
            foreach (var item in translatedListDataList.Select((v, i) => new { v, i }))
            {
                if (item.v.flag.Equals("Added"))
                {
                    string addedIndex = item.v.linesIndex;
                    int baseIndex = Convert.ToInt32(addedIndex.Remove(addedIndex.Length - 1)) - 1;
                    bool isTop = addedIndex.Contains('-');
                    string temp = $"{item.v.command}({item.v.value});";
                    if (baseIndex >= newTranslatedLines.Count && !isTop)
                    {
                        newTranslatedLines.Add(temp);

                    }
                    else if (isTop)
                    {
                        newTranslatedLines.Insert(baseIndex, temp);
                    }
                    else
                    {
                        newTranslatedLines.Insert(baseIndex + 1, temp);
                    }
                }
            }
            
            //Apply Removal
            foreach (var item in newTranslatedLines.Select((value, index) => new {value, index}))
            {
                if (item.value.Contains("█Removed█"))
                {
                    removeIndexes.Add(item.index);
                }
            }
            foreach (var index in removeIndexes.OrderByDescending(i => i))
            {
                newTranslatedLines.RemoveAt(index);
            }
            File.WriteAllLines(exportDirectory, newTranslatedLines);
        }
        #endregion
        
        #region Common Methods
        public static void AssignTempList(string[] markup, out List<string> outList)
        {
            bool addLine = false;
            outList = new List<string>();
            if (!File.Exists(currentProjectPath))
            {
                MessageBox.Show($"The system tried to load file in\n" +
                                $"{currentProjectPath}\n" +
                                $"but the file does not exist.\n" +
                                $"Aborting...",
                    "Error: AssignTempList",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            foreach (var lines in File.ReadLines(currentProjectPath))
            {
                if (lines.Equals(markup[1]))
                {
                    addLine = false;
                    break;
                }
                if (addLine)
                {
                    if (!String.IsNullOrEmpty(lines))
                    {
                        outList.Add(lines);
                    }
                }
                if (lines.Equals(markup[0]))
                {
                    addLine = true;
                }
            }
        }
        
        #endregion

        #region ListDataList Initialization
        public static void AddTempListDataList()
        {
            translatedListDataList.Clear();
            originalListDataList.Clear();
            List<string> commandList = new List<string>();
            List<string> valueList = new List<string>();
            foreach (var s in tempTranslatedLines)
            {
                string command;
                string value;
                command = s.Split(new[] { "(" }, StringSplitOptions.None)[0];
                int parenthesesIndex = s.IndexOf("(") + 1;
                value = s.Substring(parenthesesIndex, s.Length - parenthesesIndex)
                    .Split(new[] { ");" }, StringSplitOptions.None)[0];
                commandList.Add(command);
                valueList.Add(value);
            }
            for (int i = 0; i < commandList.Count; i++)
            {
                translatedListDataList.Add(new ListData()
                {
                    linesIndex = tempTranslatedIndex[i], command = commandList[i], value = valueList[i],
                    flag = String.Empty, status = String.Empty
                });
            }
            commandList.Clear();
            valueList.Clear();
            foreach (var s in tempOriginalLines)
            {
                string command;
                string value;
                command = s.Split(new[] { "(" }, StringSplitOptions.None)[0];
                int parenthesesIndex = s.IndexOf("(") + 1;
                value = s.Substring(parenthesesIndex, s.Length - parenthesesIndex)
                    .Split(new[] { ");" }, StringSplitOptions.None)[0];
                commandList.Add(command);
                valueList.Add(value);
            }
            for (int i = 0; i < commandList.Count; i++)
            {
                originalListDataList.Add(new ListData()
                {
                    linesIndex = tempOriginalIndex[i], command = commandList[i], value = valueList[i],
                    flag = String.Empty, status = String.Empty
                });
            }
            LoadModification();
            LoadRemoval();
            LoadAddition();
            VisualizerTabPage visualizerTabPage = new VisualizerTabPage();
            visualizerTabPage.RefreshList();
        }
        private static void LoadModification()
        {
            foreach (var item in tempModifiedString.Select((s, i) => new {s, i}))
            {
                foreach (var match in translatedListDataList.Select((v,j) => new {v, j}))
                {
                    if (match.v.linesIndex.Equals(tempModifiedString[item.i].Split('█')[0].Trim()))
                    {
                        translatedListDataList[match.j].value = tempModifiedString[item.i].Split('█')[1].Trim();
                        translatedListDataList[match.j].flag = "Modified";
                        break;
                    }
                }
            }
        }
        private static void LoadRemoval()
        {
            foreach (var item in tempRemovedString.Select((s, i) => new { s, i }))
            {
                foreach (var match in translatedListDataList.Select((v,j) => new {v, j}))
                {
                    if (match.v.linesIndex.Equals(tempRemovedString[item.i]))
                    {
                        translatedListDataList[match.j].flag = "Removed";
                        break;
                    }
                }
            }
        }
        private static void LoadAddition()
        {
            foreach (var item in tempAddedString.Select((s, i) => new { s, i }))
            {
                foreach (var match in translatedListDataList.Select((v,j) => new {v, j}))
                {
                    string addedIndex = tempAddedString[item.i].Split('█')[0].Trim();
                    int baseIndex = Convert.ToInt32(addedIndex.Remove(addedIndex.Length - 1));
                    bool isTop = addedIndex.Contains('-');
                    if (match.v.linesIndex.Equals(baseIndex.ToString()))
                    {
                        string command = tempAddedString[item.i].Split('█')[1].Trim();
                        string value = tempAddedString[item.i].Split('█')[2].Trim();
                        ListData newListData = new ListData()
                        {
                            linesIndex = addedIndex, command = command, value = value, flag = "Added",
                            status = String.Empty
                        };
                        if (baseIndex >= translatedListDataList.Count && !isTop)
                        {
                            translatedListDataList.Add(newListData);
                        }
                        else if (isTop)
                        {
                            translatedListDataList.Insert(match.j, newListData);
                        }
                        else
                        {
                            translatedListDataList.Insert(match.j + 1, newListData);
                        }
                        break;
                    }
                }
            }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}