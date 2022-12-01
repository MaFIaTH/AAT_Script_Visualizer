using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
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
        public static int topBlockIndex { get; set; }
        public static int bottomBlockIndex { get; set; }
    }
    public class DirectoryPath
    {
        public static string currentProjectPath { get; set; }
        public static string currentOriginalPath { get; set; }
        public static string currentTranslatedPath { get; set; }

        public static string currentTermsPath = Directory.GetCurrentDirectory() + @"\Terms.txt";
        public static string backupTermsPath = Directory.GetCurrentDirectory() + @"\Terms.backup";
    }
    public class ProjectSystem
    {
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

        public static void OpenProject()
        {
            string[] markup = { @"<Translated.Lines>", @"</Translated.Lines>" };
            string[] markup2 = { @"<Original.Lines>", @"</Original.Lines>" };
            string[] markup3 = { @"<Translated.Index>", @"</Translated.Index>" };
            string[] markup4 = { @"<Original.Index>", @"</Original.Index>" };
            List<string> translatedLineList = new List<string>();
            List<string> originalLineList = new List<string>();
            List<string> translatedIndexList = new List<string>();
            List<string> originalIndexList = new List<string>();
            AssignTempList(markup, out translatedLineList);
            AssignTempList(markup2, out originalLineList);
            AssignTempList(markup3, out translatedIndexList);
            AssignTempList(markup4, out originalIndexList);
            tempTranslatedLines = translatedLineList;
            tempOriginalLines = originalLineList;
            tempTranslatedIndex = translatedIndexList;
            tempOriginalIndex = originalIndexList;
        }

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
                    outList.Add(lines);
                }
                if (lines.Equals(markup[0]))
                {
                    addLine = true;
                }
            }
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

        public static void AddTempListDataList()
        {
            
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
            VisualizerTabPage visualizerTabPage = new VisualizerTabPage();
            visualizerTabPage.RefreshList();
        }
    }
}