using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static AAT_Script_Visualizer.AppTempData;
using static AAT_Script_Visualizer.DirectoryPath;
using ListView = System.Windows.Controls.ListView;
using ListViewItem = System.Windows.Controls.ListViewItem;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;

namespace AAT_Script_Visualizer
{
    public partial class VisualizerTabPage : UserControl, INotifyPropertyChanged
    {
        private static VisualizerTabPage _vtp = null;
        public static VisualizerTabPage vtp
        {
            get
            {
                if (_vtp == null)
                {
                    _vtp = new VisualizerTabPage();
                }
                return _vtp;
            }
        }
        public VisualizerTabPage()
        {
            InitializeComponent();
            DataContext = this;
            translatedListView.ItemsSource = translatedListDataList;

        }

        #region ListView Initialization
        private bool UserFilter(object item)
        {
            if (String.IsNullOrEmpty(listSearchBox.Text))
            {
                return true;
            }
            else
            {
                StringComparison stringComparison = caseSensitive.IsChecked != null && (bool)caseSensitive.IsChecked
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;
                bool passFilter = false;
                ListData listData = (item as ListData);
                passFilter = ReturnFilter(
                    new[]
                    {
                        listData.linesIndex, listData.command, listData.value, listData.flag
                    },
           new[]
                    {
                        ignoreLineIndex.IsChecked, ignoreCommand.IsChecked, ignoreValue.IsChecked, ignoreFlag.IsChecked
                    }, 
                    exactMatch.IsChecked, stringComparison);
                return passFilter;
            }
        }

        private bool ReturnFilter(string[] comparable, Nullable<bool>[] isChecked, Nullable<bool> isExact,
            StringComparison stringComparison)
        {
            List<bool> boolCheck = new List<bool>();
            for (int i = 0; i < isChecked.Length; i++)
            {
                if (isChecked[i] == false)
                {
                    if (isExact == true)
                    {
                        boolCheck.Add(comparable[i].Equals(listSearchBox.Text, stringComparison));
                    }
                    else
                    {
                        boolCheck.Add(comparable[i].IndexOf(listSearchBox.Text, stringComparison) >= 0);
                    }
                }
            }
            foreach (bool b in boolCheck)
            {
                if (b)
                {
                    return true;
                }
            }
            return false;
        }
        
        private void RefreshList(object sender, RoutedEventArgs e)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(translatedListView.ItemsSource);
            view.Filter = UserFilter;
            CollectionViewSource.GetDefaultView(translatedListView.ItemsSource).Refresh();
        }

        public void RefreshList()
        {
            CollectionViewSource.GetDefaultView(translatedListView.ItemsSource).Refresh();
        }
        #endregion 

        #region Manual Control
        private void PlayPrevious(object sender, RoutedEventArgs e)
        {
           PlayPrevious();
        }
        private void PlayPrevious()
        {
            List<ListData> itemSource = (translatedListView.ItemsSource as List<ListData>);
            //Error prevention if itemSource is not initialized yet
            if (itemSource.Count <= 0)
            {
                return;
            }
            SetCurrentBlock(topBlockIndex - 1, itemSource);
            GetTextValue();
            JumpToCurrentBlock();
            PlayAnimation();
        }
        private void RestartButton_OnClick(object sender, RoutedEventArgs e)
        {
            PlayAnimation();
        }
        private void PlayAnimation()
        {
            List<ListData> itemSource = (translatedListView.ItemsSource as List<ListData>);
            //Error prevention if itemSource is not initialized yet
            if (itemSource.Count <= 0)
            {
                StopPlaying();
                return;
            }
            List<ListData> itemSource1 = (translatedListView.ItemsSource as List<ListData>);
            List<ListData> itemSource2 = originalListDataList;
            SetSpeakerName(itemSource1, new []{translatedFieldSpeaker, translatedVisualizerSpeaker}, false);
            SetSpeakerName(itemSource2, new []{originalFieldSpeaker, originalVisualizerSpeaker}, true);
            TextAnimation(itemSource1, translatedVisualizer);
            TextAnimation(itemSource2, originalVisualizer);
        }
        private void PlayNext(object sender, RoutedEventArgs e)
        {
            PlayNext();
        }
        private void PlayNext()
        {
            List<ListData> itemSource = (translatedListView.ItemsSource as List<ListData>);
            //Error prevention if itemSource is not initialized yet
            if (itemSource.Count <= 0 || bottomBlockIndex >= itemSource.Count - 1)
            {
                StopPlaying();
                return;
            }
            SetCurrentBlock(bottomBlockIndex + 1, itemSource);
            GetTextValue();
            JumpToCurrentBlock();
            PlayAnimation();
        }
        #endregion

        #region Automatic Control
        public static bool isPlaying = false;
        private static int counter = 0;
        public void PlayButton_OnClick(object sender, RoutedEventArgs e)
        { 
            AutoPlay(true);
        }
        public void AutoPlay(bool fromButton)
        {
            if (!fromButton)
            {
                if (!isPlaying)
                {
                    return;
                }
                counter++;
                if (counter == 2 && isPlaying)
                {
                    counter = 0;
                    PlayNext();
                }
            }
            else
            {
                counter = 0;
                isPlaying = true;
                PlayAnimation();
            }
        }
        private void StopButton_OnClick(object sender, RoutedEventArgs e)
        {
            StopPlaying();
        }
        public void StopPlaying()
        {
            isPlaying = false;
            counter = 0;
        }
        #endregion
        
        #region Text Modification Control
        /*
        private void CopyFromOriginalButton_OnClick(object sender, RoutedEventArgs e)
        {
            entryField.SetValue(TextBox.TextProperty, originalField.Text);
        }
        */
        #endregion
        
        #region ListView Control
        private void ListViewItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem listViewItem = sender as ListViewItem;
            ListView listView = ItemsControl.ItemsControlFromItemContainer(listViewItem) as ListView;
            List<ListData> itemSource = (listView.ItemsSource as List<ListData>);
            var command = (listViewItem.Content as ListData).command;
            var value = (listViewItem.Content as ListData).value;
            var lineIndex = (listViewItem.Content as ListData).linesIndex;
            int currentIndex = -1;
            int topIndex = -1;
            int bottomIndex = -1;
            //Get current index by finding matching linesIndex
            foreach (var info in itemSource.Select((v, i) => new {v, i}))
            {
                if (info.v.linesIndex == lineIndex)
                {
                    currentIndex = info.i;
                    //tempListDataList[currentIndex].status = "Current Block";
                    break;
                }
            }
            SetCurrentBlock(currentIndex, itemSource);
            GetTextValue();
            PlayAnimation();
        }
        
        private void JumpButton_OnClick(object sender, RoutedEventArgs e)
        {
            JumpToCurrentBlock();
        }

        private void JumpToCurrentBlock()
        {
            object selectedItem = null;
            List<object> matches = new List<object>();
            foreach (var t in (translatedListView.Items))
            {
                if ((t as ListData).status.Equals("Current Block"))
                {
                    matches.Add(t);
                }
            }
            if (matches.Count == 0)
            {
                return;
            }
            selectedItem = matches[Convert.ToInt32(matches.Count / 2)];
            translatedListView.ScrollIntoView(selectedItem);
        }

        private void SetCurrentBlock(int currentIndex, List<ListData> itemSource)
        {
            //Error prevention if currentIndex is less than zero or more than itemSource.Count
            if (currentIndex < 0 || currentIndex > itemSource.Count)
            {
                return;
            }
            //Reset previous block status to String.Empty
            if (bottomBlockIndex != null && topBlockIndex != null)
            {
                for (int i = topBlockIndex; i <= bottomBlockIndex; i++)
                {
                    translatedListDataList[i].status = String.Empty;
                }
            }
            //Get top index by find up from currentIndex
            for (int i = currentIndex - 1; i >= 0; i--)
            {
                if (itemSource[i].flag.Equals("Removed")) continue;
                if (i == 0)
                {
                    topBlockIndex = 0;
                    break;
                }
                if (itemSource[i].command.Equals("ClearText") || itemSource[i].command.Equals("ReadKey") ||
                    itemSource[i].command.Contains("Op_"))
                {
                    topBlockIndex = i + 1;
                    break;
                }
            }
            //Get buttom index by find down from currentIndex
            for (int i = currentIndex; i < itemSource.Count; i++)
            {
                if (itemSource[i].flag.Equals("Removed")) continue;
                if (itemSource[i].command.Equals("ClearText") || itemSource[i].command.Equals("ReadKey") ||
                    itemSource[i].command.Contains("Op_"))
                {
                    bottomBlockIndex = i;
                    break;
                }
            }
            //Set status to Current Block
            for (int i = topBlockIndex; i <= bottomBlockIndex; i++)
            {
                translatedListDataList[i].status = "Current Block";
            }
        }
        #endregion
        
        #region Visualizer
        private static string _simplifiedTranslatedText = String.Empty;
        private static string _simplifiedOriginalText = String.Empty;
        public string simplifiedTranslatedText
        {
            get => _simplifiedTranslatedText;
            set {_simplifiedTranslatedText = value; OnPropertyChanged("simplifiedTranslatedText");}
        }

        public string simplifiedOriginalText
        {
            get => _simplifiedOriginalText;
            set {_simplifiedOriginalText = value; OnPropertyChanged("simplifiedOriginalText");}
        }
        private void GetTextValue()
        {
            simplifiedTranslatedText = String.Empty;
            simplifiedOriginalText = String.Empty;
            if (translatedListView.Items.Count <= 0)
            {
                return;
            }
            for (int i = topBlockIndex; i <= bottomBlockIndex; i++)
            {
                simplifiedTranslatedText += ExtractCommand(translatedListDataList, i, true);
                simplifiedOriginalText += ExtractCommand(originalListDataList, i, true);
            }
        }
        private string ExtractCommand(List<ListData> listDataList, int index, bool simplify)
        {
            string extractedText = String.Empty;
            if (listDataList[index].command == "Text" && !listDataList[index].flag.Equals("Removed"))
            {
                var firstString =
                    listDataList[index].value.Substring(1, listDataList[index].value.Length - 1);
                if (simplify)
                {
                    extractedText = firstString.Substring(0, firstString.LastIndexOf('"'));
                }
                else
                {
                    extractedText = $"[T:{firstString.Substring(0, firstString.LastIndexOf('"'))}]#";
                }
                
            }
            if (listDataList[index].command == "NewLine" && !listDataList[index].flag.Equals("Removed"))
            {
                if (simplify)
                {
                    extractedText = "\n";
                }
                else
                {
                    extractedText = "[NL]#";
                }
            }
            return extractedText;
        }
        //https://stackoverflow.com/a/3431848//
        //I modify this one slightly to fit the purpose.
        //private static int messageTime = 50;
        private static string rawText = String.Empty;
        public void TextAnimation(List<ListData> itemSource, TextBlock txt)
        {
            int keyTime = 0;
            string animationText = String.Empty;
            int messageTime = 50;
            bool hasSMT = false; 
            bool hasSPI = false;
            txt.TextAlignment = TextAlignment.Left;
            Storyboard story = new Storyboard();
            story.FillBehavior = FillBehavior.HoldEnd;
            DiscreteStringKeyFrame discreteStringKeyFrame;
            StringAnimationUsingKeyFrames stringAnimationUsingKeyFrames = new StringAnimationUsingKeyFrames();
            TimeSpan timeSpanKeyTime = TimeSpan.FromMilliseconds(keyTime);
            for (int i = topBlockIndex; i <= bottomBlockIndex; i++)
            {
                if (itemSource[i].flag.Equals("Removed")) continue;
                if (itemSource[i].command.Equals("SetMessageTime"))
                {
                    hasSMT = true;
                    break;
                }
                if (itemSource[i].command.Equals("Text"))
                {
                    break;
                }
            }
            for (int i = topBlockIndex; i >= 0 && !hasSMT; i--)
            {
                if (itemSource[i].flag.Equals("Removed")) continue;
                if (itemSource[i].command.Equals("SetMessageTime"))
                {
                    if (!GetMessageTime(itemSource[i].value, out messageTime))
                    {
                        MessageBox.Show(
                            $"Command: {itemSource[i].command}\n" +
                            $"Value: {itemSource[i].value}\n" +
                            $"Error: Command and/or Value are undefined.\n" +
                            $"Aborting...",
                            "Error: TextAnimation",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    break;
                }
                if (i == 0)
                {
                    messageTime = 50;
                }
            }
            for (int i = topBlockIndex; i <= bottomBlockIndex; i++)
            {
                if (itemSource[i].flag.Equals("Removed")) continue;
                if (itemSource[i].command.Equals("SetMessageTime"))
                {
                    if (!GetMessageTime(itemSource[i].value, out messageTime))
                    {
                        MessageBox.Show(
                            $"Command: {itemSource[i].command}\n" +
                            $"Value: {itemSource[i].value}\n" +
                            $"Error: Command and/or Value are undefined.\n" +
                            $"Aborting...",
                            "Error: TextAnimation",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                if (itemSource[i].command.Equals("SetTextFlag"))
                {
                    if (itemSource[i].value.Equals("0"))
                    {
                        txt.TextAlignment = TextAlignment.Left;
                    }
                    else if (itemSource[i].value.Equals("1"))
                    {
                        txt.TextAlignment = TextAlignment.Center;
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Command: {itemSource[i].command}\n" +
                            $"Value: {itemSource[i].value}\n" +
                            $"Error: Command and/or Value are undefined.\n" +
                            $"Aborting...",
                            "Error: TextAnimation",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                if (itemSource[i].command.Equals("Wait"))
                {
                    string extractedValue = itemSource[i].value;
                    int waitTime = Convert.ToInt32(extractedValue) * 10;
                    keyTime += waitTime;
                    timeSpanKeyTime = TimeSpan.FromMilliseconds(keyTime);
                    discreteStringKeyFrame = new DiscreteStringKeyFrame(animationText, timeSpanKeyTime);
                    stringAnimationUsingKeyFrames.KeyFrames.Add(discreteStringKeyFrame);
                    keyTime += messageTime;
                }
                if (itemSource[i].command.Equals("Text"))
                {
                    var firstString =
                        itemSource[i].value.Substring(1, itemSource[i].value.Length - 1);
                    string extractedText = firstString.Substring(0, firstString.LastIndexOf('"'));
                    foreach (char c in extractedText)
                    {
                        timeSpanKeyTime = TimeSpan.FromMilliseconds(keyTime);
                        discreteStringKeyFrame = new DiscreteStringKeyFrame(animationText, timeSpanKeyTime);
                        stringAnimationUsingKeyFrames.KeyFrames.Add(discreteStringKeyFrame);
                        animationText += c;
                        keyTime += messageTime;
                    }
                    timeSpanKeyTime = TimeSpan.FromMilliseconds(keyTime);
                    discreteStringKeyFrame = new DiscreteStringKeyFrame(animationText, timeSpanKeyTime);
                    stringAnimationUsingKeyFrames.KeyFrames.Add(discreteStringKeyFrame);
                    keyTime += messageTime;
                }
                if (itemSource[i].command.Equals("NewLine"))
                {
                    animationText += "\n";
                }
                if (itemSource[i].command.Equals("ReadKey"))
                {
                    int waitTime = 500;
                    keyTime += waitTime;
                    timeSpanKeyTime = TimeSpan.FromMilliseconds(keyTime);
                    discreteStringKeyFrame = new DiscreteStringKeyFrame(animationText, timeSpanKeyTime);
                    stringAnimationUsingKeyFrames.KeyFrames.Add(discreteStringKeyFrame);
                    keyTime += messageTime;
                }
            }
            Storyboard.SetTargetName(stringAnimationUsingKeyFrames, txt.Name);
            Storyboard.SetTargetProperty(stringAnimationUsingKeyFrames, new PropertyPath(TextBlock.TextProperty));
            story.Children.Add(stringAnimationUsingKeyFrames);
            story.Completed += (o, e) => ApplyColor(itemSource, txt);
            story.Begin(txt);
        }

        private void SetSpeakerName(List<ListData> itemSource, Label[] speakerLabels, bool isOriginal)
        {
            string speakerName = String.Empty;
            string[] markup = { @"<SetSpeakerId>", @"</SetSpeakerId>" };
            string command = String.Empty;
            string value = String.Empty;
            bool startCompare = false;
            for (int i = bottomBlockIndex; i >= 0; i--)
            {
                if (itemSource[i].flag.Equals("Removed")) continue;
                if (itemSource[i].command.Equals("SetSpeakerId"))
                {
                    command = itemSource[i].command;
                    value = itemSource[i].value;
                    break;
                }
                if (i == 0)
                {
                    MessageBox.Show(
                        $"Command: {itemSource[i].command}\n" +
                        $"Value: {itemSource[i].value}\n" +
                        $"Error: Command and/or Value is undefined.\n" +
                        $"Aborting...",
                        "Error: SetSpeakerName",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            foreach (var lines in File.ReadLines(currentTermsPath))
            {
                if (lines.Contains(markup[1]))
                {
                    MessageBox.Show(
                        $"Command: {command}\n" +
                        $"Value: {value}\n" +
                        $"Error: Command and/or Value is undefined.\n" +
                        $"Aborting...",
                        "Error: SetSpeakerName",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (startCompare)
                {
                    string extractString = lines;
                    if (extractString.Split('=')[0].Trim().Equals(value))
                    {
                        speakerName = extractString.Split('=')[1].Trim();
                        if (speakerName.Contains("/"))
                        {
                            int splitIndex = isOriginal ? 0 : 1; 
                            speakerName = speakerName.Split('/')[splitIndex].Trim();
                            speakerLabels[0].Content = $"[{speakerName}]";
                            speakerLabels[1].Content = $"[{speakerName}]";
                        }
                        else
                        {
                            speakerLabels[0].Content = $"[{speakerName}]";
                            speakerLabels[1].Content = $"[{speakerName}]";
                        }
                        return;
                    }
                }
                if (lines.Contains(markup[0]))
                {
                    startCompare = true;
                }
            }
        }
        private bool GetMessageTime(string value, out int messageTime)
        {
            messageTime = -1;
            string[] markup = { @"<SetMessageTime>", @"</SetMessageTime>" };
            bool startCompare = false;
            foreach (var lines in File.ReadLines(currentTermsPath))
            {
                if (lines.Contains(markup[1]))
                {
                    return false;
                }
                if (startCompare)
                {
                    string extractString = Regex.Replace(lines, @"\s+", "");
                    if (extractString.Split('=')[0].Equals(value))
                    {
                        messageTime = Convert.ToInt32(extractString.Split('=')[1]);
                        return true;
                    }
                }
                if (lines.Contains(markup[0]))
                {
                    startCompare = true;
                }
            }
            return false;
        }
        private async void ApplyColor(List<ListData> itemSource, TextBlock txt)
        {
            bool hasSTC = false;
            SolidColorBrush textBrush = Brushes.White;
            txt.BeginAnimation(TextBlock.TextProperty, null);
            txt.Inlines.Clear();
            for (int i = topBlockIndex; i <= bottomBlockIndex; i++)
            {
                if (itemSource[i].flag.Equals("Removed")) continue;
                if (itemSource[i].command.Equals("SetTextColor"))
                {
                    hasSTC = true;
                    break;
                }
                if (itemSource[i].command.Equals("Text"))
                {
                    break;
                }
            }
            for (int i = topBlockIndex; i >= 0 && !hasSTC; i--)
            {
                if (itemSource[i].flag.Equals("Removed")) continue;
                if (itemSource[i].command.Equals("SetTextColor"))
                {
                    switch (itemSource[i].value)
                    {
                        case "0":
                            textBrush = Brushes.White;
                            break;
                        case "1":
                            textBrush = Brushes.Crimson;
                            break;
                        case "2":
                            textBrush = Brushes.DeepSkyBlue;
                            break;
                        case "3":
                            textBrush = Brushes.LawnGreen;
                            break;
                        default:
                            MessageBox.Show(
                                $"Command: {itemSource[i].command}\n" +
                                $"Value: {itemSource[i].value}\n" +
                                $"Error: Command and/or Value are undefined.\n" +
                                $"Aborting...",
                                "Error: ApplyColor",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                    }
                    break;
                }
                if (i == 0)
                {
                    textBrush = Brushes.White;
                }
            }
            for (int i = topBlockIndex; i <= bottomBlockIndex; i++)
            {
                if (itemSource[i].flag.Equals("Removed")) continue;
                if (itemSource[i].command.Equals("SetTextColor"))
                {
                    switch (itemSource[i].value)
                    {
                        case "0":
                            textBrush = Brushes.White;
                            break;
                        case "1":
                            textBrush = Brushes.Red;
                            break;
                        case "2":
                            textBrush = Brushes.DeepSkyBlue;
                            break;
                        case "3":
                            textBrush = Brushes.LawnGreen;
                            break;
                        default:
                            MessageBox.Show(
                                $"Command: {itemSource[i].command}\n" +
                                $"Value: {itemSource[i].value}\n" +
                                $"Error: Command and/or Value are undefined.\n" +
                                $"Aborting...",
                                "Error: ApplyColor",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                    }
                }
                if (itemSource[i].command.Equals("Text"))
                {
                    var firstString =
                        itemSource[i].value.Substring(1, itemSource[i].value.Length - 1);
                    string extractedText = firstString.Substring(0, firstString.LastIndexOf('"'));
                    txt.Inlines.Add(new Run(extractedText) {Foreground = textBrush});
                }

                if (itemSource[i].command.Equals("NewLine"))
                {
                    txt.Inlines.Add(new Run("\n"));
                }
            }
            await Task.Delay(500);
            AutoPlay(false);
        }
        
        #endregion
        
        #region Command Modification
        private bool _allowEditing = false;
        private bool _modified = false;
        private bool _allowRemove = false;
        private bool _removed = false;
        private ListViewItem currentItem = null;
        private ContextMenu cm = null;
        private Label lb = null;
        private TextBox tb = null;
        public bool allowEditing
        {
            get => _allowEditing;
            set { _allowEditing = value; OnPropertyChanged("allowEditing"); }
        }

        public bool modified
        {
            get => _modified;
            set { _modified = value; OnPropertyChanged("modified"); }
        }

        public bool removed
        {
            get
            {
                if (!_allowRemove)
                {
                    return false;
                }
                return _removed;
            }
            set { _removed = value; OnPropertyChanged("removed");
                OnPropertyChanged("inverseRemoved");
            }
        }

        public bool inverseRemoved
        {
            get 
            {
                if (!_allowRemove)
                {
                    return false;
                }
                return !_removed;
            }
        }
        
        private void ListViewRightMouseClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem listViewItem = sender as ListViewItem;
            currentItem = listViewItem;
            ListView listView = ItemsControl.ItemsControlFromItemContainer(listViewItem) as ListView;
            ListData listData = currentItem.Content as ListData;
            List<ListData> itemSource = (listView.ItemsSource as List<ListData>);
            var command = listData.command;
            var value = listData.value;
            var lineIndex = listData.linesIndex;
            var flag = listData.flag;
            allowEditing =
                new[] { "Text", "SetMessageTime", "SetTextFlag", "SetTextColor", "SetSpeakerId", "Wait" }.Any(s =>
                    command.Equals(s));
            _allowRemove = !(new[] { "ReadKey", "ClearText", "Op_" }.Any(s => command.Contains(s)));
            modified = flag.Equals("Modified");
            removed = flag.Equals("Removed");
            cm = FindResource("listViewItemContextMenu") as ContextMenu;
            cm.PlacementTarget = listViewItem;
            cm.IsOpen = true;
            if (allowEditing)
            {
                lb = LogicalTreeHelper.FindLogicalNode(cm, "currentValueLabel") as Label;
                tb = LogicalTreeHelper.FindLogicalNode(cm, "newValueBox") as TextBox;
                lb.Content = $"Current Value: {listData.value}";
            }
        }
        private void RemoveCommand_OnClick(object sender, RoutedEventArgs e)
        {
            (currentItem.Content as ListData).flag = "Removed";
        }
        private void RevertRemoval_OnClick(object sender, RoutedEventArgs e)
        {
            (currentItem.Content as ListData).flag = String.Empty;
        }
        
        private void SaveValue_OnClick(object sender, RoutedEventArgs e)
        {
            ListData listData = currentItem.Content as ListData;
            string oldValue = listData.value;
            listData.value = tb.Text;
            if (!listData.flag.Equals("Added") && !listData.flag.Equals("Modified"))
            {
                listData.flag = "Modified";
                //Line Index / New Value / Old Value
                tempModifiedString.Add($"{listData.linesIndex} / {listData.value} / {oldValue}");
            }
        }

        private void CopyValue_OnClick(object sender, RoutedEventArgs e)
        {
            ListData listData = currentItem.Content as ListData;
            tb.Text = listData.value;
        }
        private void ResetValue_OnClick(object sender, RoutedEventArgs e)
        {
            ListData listData = currentItem.Content as ListData;
            listData.flag = String.Empty;
            for (int i = 0; i < tempModifiedString.Count; i++)
            {
                if (listData.linesIndex.Equals(tempModifiedString[i].Split('/')[0].Trim()))
                {
                    listData.value = tempModifiedString[i].Split('/')[2].Trim();
                    tempModifiedString.RemoveAt(i);
                    break;
                }
            }

        }
        #endregion
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void VisualizerTabPage_OnLostFocus(object sender, RoutedEventArgs e)
        {
            StopPlaying();
        }

        
    }

    public class ListData : INotifyPropertyChanged
    {
        private string _linesIndex;
        private string _command;
        private string _value;
        private string _flag;
        private string _status;
        public string linesIndex
        {
            get => _linesIndex;
            set {_linesIndex = value; OnPropertyChanged("linesIndex");} 
        }
        public string command 
        {
            get => _command;
            set {_command = value; OnPropertyChanged("command");} 
        }
        public string value 
        {
            get => _value;
            set {_value = value; OnPropertyChanged("value");} 
        }
        public string flag
        {
            get => _flag;
            set {_flag = value; OnPropertyChanged("flag");} 
        }
        public string status
        {
            get => _status;
            set {_status = value; OnPropertyChanged("status");} 
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    //I stole the belows from StackOverflow lol
    //https://stackoverflow.com/a/33166823//
    public class LineLimitingBehavior : Behavior<TextBox>
    {
        public int? TextBoxMaxAllowedLines { get; set; }
        
        protected override void OnAttached()
        {
            if (TextBoxMaxAllowedLines != null && TextBoxMaxAllowedLines > 0)
                AssociatedObject.TextChanged += OnTextBoxTextChanged;
        }
        
        protected override void OnDetaching()
        {
            AssociatedObject.TextChanged -= OnTextBoxTextChanged;
        }

        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;

            int textLineCount = textBox.LineCount;

            //Use Dispatcher to undo - http://stackoverflow.com/a/25453051/685341
            if (textLineCount > TextBoxMaxAllowedLines.Value)
                Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action) (() => textBox.Undo()));
        }
    }
}