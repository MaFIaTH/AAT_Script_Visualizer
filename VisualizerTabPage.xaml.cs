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
            if (itemSource.Count <= 0 || topTranslatedBlockIndex <= 0)
            {
                return;
            }
            //Reset previous block status to String.Empty
            if (bottomTranslatedBlockIndex > -1 && topTranslatedBlockIndex > -1)
            {
                for (int i = topTranslatedBlockIndex; i <= bottomTranslatedBlockIndex; i++)
                {
                    translatedListDataList[i].status = String.Empty;
                }
            }
            if (!SetCurrentBlock(topTranslatedBlockIndex - 1, itemSource, out topTranslatedBlockIndex, out bottomTranslatedBlockIndex))
            {
                return;
            }
            if (!SetCurrentBlock(topOriginalBlockIndex - 1, originalListDataList, out topOriginalBlockIndex, out bottomOriginalBlockIndex))
            {
                return;
            }
            //Set status to Current Block
            for (int i = topTranslatedBlockIndex; i <= bottomTranslatedBlockIndex; i++)
            {
                translatedListDataList[i].status = "Current Block";
            }
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
            if (topTranslatedBlockIndex < 0 && bottomTranslatedBlockIndex < 0)
            {
                if (!SetCurrentBlock(1, itemSource, out topTranslatedBlockIndex, out bottomTranslatedBlockIndex))
                {
                    return;
                }
            }
            if (topOriginalBlockIndex < 0 && bottomOriginalBlockIndex < 0)
            {
                if (!SetCurrentBlock(1, originalListDataList, out topOriginalBlockIndex, out bottomOriginalBlockIndex))
                {
                    return;
                }
            }
            //Set status to Current Block
            for (int i = topTranslatedBlockIndex; i <= bottomTranslatedBlockIndex; i++)
            {
                translatedListDataList[i].status = "Current Block";
            }
            List<ListData> itemSource1 = (translatedListView.ItemsSource as List<ListData>);
            List<ListData> itemSource2 = originalListDataList;
            SetSpeakerName(itemSource1, new[] { translatedFieldSpeaker, translatedVisualizerSpeaker }, false,
                bottomTranslatedBlockIndex);
            SetSpeakerName(itemSource2, new []{originalFieldSpeaker, originalVisualizerSpeaker}, true, bottomOriginalBlockIndex);
            TextAnimation(itemSource1, translatedVisualizer, topTranslatedBlockIndex, bottomTranslatedBlockIndex);
            TextAnimation(itemSource2, originalVisualizer, topOriginalBlockIndex, bottomOriginalBlockIndex);
        }
        private void PlayNext(object sender, RoutedEventArgs e)
        {
            PlayNext();
        }
        private void PlayNext()
        {
            List<ListData> itemSource = (translatedListView.ItemsSource as List<ListData>);
            //Error prevention if itemSource is not initialized yet
            if (itemSource.Count <= 0 || bottomTranslatedBlockIndex >= itemSource.Count - 1)
            {
                StopPlaying();
                return;
            }
            //Reset previous block status to String.Empty
            if (bottomTranslatedBlockIndex > -1 && topTranslatedBlockIndex > -1)
            {
                for (int i = topTranslatedBlockIndex; i <= bottomTranslatedBlockIndex; i++)
                {
                    translatedListDataList[i].status = String.Empty;
                }
            }
            if (topTranslatedBlockIndex < 0 || bottomTranslatedBlockIndex < 0)
            {
                return;
            }
            if (!SetCurrentBlock(bottomTranslatedBlockIndex + 1, itemSource, out topTranslatedBlockIndex, out bottomTranslatedBlockIndex))
            {
                return;
            }
            if (!SetCurrentBlock(bottomOriginalBlockIndex + 1, originalListDataList, out topOriginalBlockIndex, out bottomOriginalBlockIndex))
            {
                return;
            }
            //Set status to Current Block
            for (int i = topTranslatedBlockIndex; i <= bottomTranslatedBlockIndex; i++)
            {
                translatedListDataList[i].status = "Current Block";
            }
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

        #region ListView Control
        private void ListViewItem_OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem listViewItem = sender as ListViewItem;
            ListView listView = ItemsControl.ItemsControlFromItemContainer(listViewItem) as ListView;
            List<ListData> translatedItemSource = (listView.ItemsSource as List<ListData>);
            var command = (listViewItem.Content as ListData).command;
            var value = (listViewItem.Content as ListData).value;
            var lineIndex = (listViewItem.Content as ListData).linesIndex;
            int currentTranslatedIndex = -1;
            int currentOriginalIndex = -1;
            int topIndex = -1;
            int bottomIndex = -1;
            //Get current index by finding matching linesIndex
            foreach (var info in translatedItemSource.Select((v, i) => new {v, i}))
            {
                if (info.v.linesIndex == lineIndex)
                {
                    currentTranslatedIndex = info.i;
                    //tempListDataList[currentIndex].status = "Current Block";
                    break;
                }
            }
            //Get current index by finding matching linesIndex
            foreach (var info in originalListDataList.Select((v, i) => new {v, i}))
            {
                string basedIndex = lineIndex;
                if (lineIndex.Contains('-') || lineIndex.Contains('+'))
                {
                    basedIndex = basedIndex.Remove(basedIndex.Length - 1);
                }
                if (info.v.linesIndex == basedIndex)
                {
                    currentOriginalIndex = info.i;
                    break;
                }
            }
            //Reset previous block status to String.Empty
            if (bottomTranslatedBlockIndex > -1 && topTranslatedBlockIndex > -1)
            {
                for (int i = topTranslatedBlockIndex; i <= bottomTranslatedBlockIndex; i++)
                {
                    translatedListDataList[i].status = String.Empty;
                }
            }
            if (!SetCurrentBlock(currentTranslatedIndex, translatedItemSource, out topTranslatedBlockIndex, out bottomTranslatedBlockIndex))
            {
                return;
            }
            if (!SetCurrentBlock(currentOriginalIndex, originalListDataList, out topOriginalBlockIndex, out bottomOriginalBlockIndex))
            {
                return;
            }
            //Set status to Current Block
            for (int i = topTranslatedBlockIndex; i <= bottomTranslatedBlockIndex; i++)
            {
                translatedListDataList[i].status = "Current Block";
            }
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

        private bool SetCurrentBlock(int currentIndex, List<ListData> itemSource, out int topBlockIndex, out int bottomBlockIndex)
        {
            bottomBlockIndex = -1;
            topBlockIndex = -1;
            //Error prevention if currentIndex is less than zero or more than itemSource.Count
            if (currentIndex < 0 || currentIndex > itemSource.Count)
            {
                return false;
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
            //Get bottom index by find down from currentIndex
            for (int i = currentIndex; i < itemSource.Count; i++)
            {
                if (itemSource[i].flag.Equals("Removed")) continue;
                if (itemSource[i].command.Equals("ClearText") || itemSource[i].command.Equals("ReadKey") ||
                    itemSource[i].command.Contains("Op_"))
                {
                    bottomBlockIndex = i;
                    break;
                }
                if (i == itemSource.Count - 1)
                {
                    MessageBox.Show(
                        $"Text Box Ending Command is not found\n" +
                        $"Aborting...",
                        "Error: SetCurrentBlock",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            return true;
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
            for (int i = topOriginalBlockIndex; i <= bottomOriginalBlockIndex; i++)
            {
                simplifiedOriginalText += ExtractCommand(originalListDataList, i);
            }
            for (int i = topTranslatedBlockIndex; i <= bottomTranslatedBlockIndex; i++)
            {
                simplifiedTranslatedText += ExtractCommand(translatedListDataList, i);
            }
        }
        private string ExtractCommand(List<ListData> listDataList, int index)
        {
            string extractedText = String.Empty;
            if (listDataList[index].command == "Text" && !listDataList[index].flag.Equals("Removed"))
            {
                var firstString =
                    listDataList[index].value.Substring(1, listDataList[index].value.Length - 1);
                extractedText = firstString.Substring(0, firstString.LastIndexOf('"'));
            }
            if (listDataList[index].command == "NewLine" && !listDataList[index].flag.Equals("Removed"))
            {
                extractedText = "\n";
            }
            return extractedText;
        }
        //https://stackoverflow.com/a/3431848//
        //I modify this one slightly to fit the purpose.
        private List<Storyboard> currentStories = new List<Storyboard>();
        private List<EventHandler> currentEvents = new List<EventHandler>();
        public void TextAnimation(List<ListData> itemSource, TextBlock txt, int topBlockIndex, int bottomBlockIndex)
        {
            //Unsub previous event to prevent apply color collision
            if (currentStories.Count >= 2)
            {
                for (int i = 0; i < 2; i++)
                {
                    currentStories[i].Completed -= currentEvents[i];
                }
                currentStories.Clear();
                currentEvents.Clear();
            }
            int keyTime = 0;
            string animationText = String.Empty;
            int messageTime = 50;
            bool hasSMT = false; //has SetMessageTime
            bool hasSPI = false; //has SetSpeakerId
            txt.TextAlignment = TextAlignment.Left;
            txt.BeginAnimation(TextBlock.TextProperty, null);
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
            EventHandler newEvent = (o, e) => ApplyColor(itemSource, txt, topBlockIndex, bottomBlockIndex);
            currentEvents.Add(newEvent);
            story.Completed += newEvent;
            story.Begin(txt);
            currentStories.Add(story);
        }

        private void SetSpeakerName(List<ListData> itemSource, Label[] speakerLabels, bool isOriginal, int bottomBlockIndex)
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
        
        private async void ApplyColor(List<ListData> itemSource, TextBlock txt, int topBlockIndex, int bottomBlockIndex)
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
        private bool _allowRemove = false;
        private bool _allowAddition = false;
        private bool _modified = false;
        private bool _removed = false;
        private bool _added = false;
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

        public bool added
        {
            get => _added;
            set { _added = value; OnPropertyChanged("added"); }
        }

        public bool allowAdded
        {
            get => _allowAddition;
            set { _allowAddition = value; OnPropertyChanged("allowAdded"); }
        }
        
        private void ListViewItem_OnRightMouseClick(object sender, MouseButtonEventArgs e)
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
            cm = FindResource("listViewItemContextMenu") as ContextMenu;
            MenuItem addTopMenuItem = LogicalTreeHelper.FindLogicalNode(cm, "addTopMenuItem") as MenuItem;
            MenuItem addBottomMenuItem = LogicalTreeHelper.FindLogicalNode(cm, "addBottomMenuItem") as MenuItem;
            addTopMenuItem.Items.Clear();
            addBottomMenuItem.Items.Clear();
            string[] allowedAdditionCommands = new[]
                { "Text", "SetMessageTime", "SetTextFlag", "SetTextColor", "SetSpeakerId", "Wait", "NewLine", "ReadKey", "ClearText" };
            foreach (var s in allowedAdditionCommands)
            {
                MenuItem newMenuItem = new MenuItem();
                newMenuItem.Name = $"{s}MenuItem";
                newMenuItem.Header = s;
                addTopMenuItem.Items.Add(newMenuItem);
                newMenuItem = new MenuItem();
                newMenuItem.Name = $"{s}MenuItem";
                newMenuItem.Header = s;
                addBottomMenuItem.Items.Add(newMenuItem);
            }
            allowEditing =
                new[] { "Text", "SetMessageTime", "SetTextFlag", "SetTextColor", "SetSpeakerId", "Wait" }.Any(s =>
                    command.Equals(s));
            _allowRemove = !(new[] { "Op_" }.Any(s => command.Contains(s)));
            modified = flag.Equals("Modified");
            removed = flag.Equals("Removed");
            added = flag.Equals("Added");
            cm.PlacementTarget = listViewItem;
            cm.IsOpen = true;
            if (allowEditing)
            {
                lb = LogicalTreeHelper.FindLogicalNode(cm, "currentValueLabel") as Label;
                tb = LogicalTreeHelper.FindLogicalNode(cm, "newValueBox") as TextBox;
                lb.Content = $"Current Value: {listData.value}";
                tb.Text = String.Empty;
            }
        }
        private void RemoveCommand_OnClick(object sender, RoutedEventArgs e)
        {
            ListData listData = currentItem.Content as ListData;
            if (listData.flag.Equals("Modified"))
            {
                ResetValue();
            }
            if (listData.flag.Equals("Added"))
            {
                string tempString =
                    $"{listData.linesIndex} █ {listData.command} █ {listData.value}";
                tempAddedString.Remove(tempString);
                translatedListDataList.Remove(currentItem.Content as ListData);
                RefreshList();
                return;
            }
            listData.flag = "Removed";
            tempRemovedString.Add($"{listData.linesIndex}");
        }
        private void RevertRemoval_OnClick(object sender, RoutedEventArgs e)
        {
            ListData listData = currentItem.Content as ListData;
            listData.flag = String.Empty;
            tempRemovedString.Remove($"{listData.linesIndex}");
        }
        
        private void SaveValue_OnClick(object sender, RoutedEventArgs e)
        {
            ListData listData = currentItem.Content as ListData;
            string oldValue = listData.value;
            string newValue = String.Empty;
            newValue = tb.Text;
            if (!listData.command.Equals("Text") && (!int.TryParse(newValue, out var valueTest) || valueTest < 0))
            {
                MessageBox.Show(
                    $"Input value is invalid!\n" +
                    $"Aborting...",
                    "Error: SaveValue_OnClick",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (listData.command.Equals("Text"))
            {
                int firstQuoteIndex = newValue.IndexOf('"');
                int lastQuoteIndex = newValue.LastIndexOf('"');
                if (firstQuoteIndex < 0 || lastQuoteIndex < 0 || firstQuoteIndex == lastQuoteIndex)
                {
                    MessageBox.Show(
                        $"Input value is invalid!\n" +
                        $"Aborting...",
                        "Error: SaveValue_OnClick",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                string value = String.Empty;
                for (int i = firstQuoteIndex; i <= lastQuoteIndex; i++)
                {
                    value += newValue[i];
                }
                newValue = value;
            }
            listData.value = newValue;
            if (!listData.flag.Equals("Added") && !listData.flag.Equals("Modified"))
            {
                listData.flag = "Modified";
                //Line Index / New Value / Old Value
                tempModifiedString.Add($"{listData.linesIndex} █ {newValue} █ {oldValue}");
                return;
            }
            if (listData.flag.Equals("Modified"))
            {
                foreach (var match in tempModifiedString.Select((v, i) => new {v, i}))
                {
                    if (match.v.Contains($"{listData.linesIndex}"))
                    {
                        string retainedOldValue = match.v.Split('█')[2].Trim();
                        string replaceString = $"{listData.linesIndex} █ {newValue} █ {retainedOldValue}";
                        tempModifiedString[match.i] = replaceString;
                        break;
                    }
                }
                return;
            }
            if (listData.flag.Equals("Added"))
            {
                foreach (var match in tempAddedString.Select((v, i) => new { v, i }))
                {
                    if (match.v.Equals($"{listData.linesIndex} █ {listData.command} █ {oldValue}"))
                    {
                        string replaceString = $"{listData.linesIndex} █ {listData.command} █ {newValue}";
                        tempAddedString[match.i] = replaceString;
                        break;
                    }
                }
            }
        }
        
        private void CopyValue_OnClick(object sender, RoutedEventArgs e)
        {
            ListData listData = currentItem.Content as ListData;
            tb.Text = listData.value;
        }
        private void ResetValue_OnClick(object sender, RoutedEventArgs e)
        {
            ResetValue();
        }

        private void ResetValue()
        {
            ListData listData = currentItem.Content as ListData;
            listData.flag = String.Empty;
            for (int i = 0; i < tempModifiedString.Count; i++)
            {
                if (listData.linesIndex.Equals(tempModifiedString[i].Split('█')[0].Trim()))
                {
                    listData.value = tempModifiedString[i].Split('█')[2].Trim();
                    tempModifiedString.RemoveAt(i);
                    break;
                }
            }
        }

        
        private void AddTop_OnClick(object sender, RoutedEventArgs e)
        {
            MenuItem currentMenuItem = sender as MenuItem;
            AddNewCommand(currentMenuItem, true);
        }

        private void AddBottom_OnClick(object sender, RoutedEventArgs e)
        {
            MenuItem currentMenuItem = sender as MenuItem;
            AddNewCommand(currentMenuItem, false);
        }

        private void AddNewCommand(MenuItem menuItem, bool isTop)
        {
            MenuItem currentMenuItem = menuItem;
            ListView listView = ItemsControl.ItemsControlFromItemContainer(currentItem) as ListView;
            List<ListData> itemSource = (listView.ItemsSource as List<ListData>);
            int addedIndex = -1;
            string currentItemLineIndex = String.Empty;
            string defaultValue = "0";
            bool isLast = false;
            //Get added index by finding matching linesIndex
            foreach (var info in itemSource.Select((v, i) => new {v, i}))
            {
                if (info.v.linesIndex == (currentItem.Content as ListData).linesIndex)
                {
                    addedIndex = info.i;
                    currentItemLineIndex = info.v.linesIndex;
                    break;
                }
                if (info.i == itemSource.Count)
                {
                    isLast = true;
                    break;
                }
            }
            if (currentMenuItem.Name.Equals("TextMenuItem"))
            {
                defaultValue = @"""""";
            }
            if (currentMenuItem.Name.Equals("NewLineMenuItem") || currentMenuItem.Name.Equals("ReadKeyMenuItem") ||
                currentMenuItem.Name.Equals("ClearTextMenuItem"))
            {
                defaultValue = String.Empty;
            }
            if (currentItemLineIndex.Contains('-'))
            {
                currentItemLineIndex = currentItemLineIndex.Split('-')[0];
            }
            if (currentItemLineIndex.Contains('+'))
            {
                currentItemLineIndex = currentItemLineIndex.Split('+')[0];
            }
            currentItemLineIndex += isTop ? '-' : '+';
            ListData newListData = new ListData()
            {
                linesIndex = $"{currentItemLineIndex}", command = currentMenuItem.Header.ToString(),
                value = defaultValue, flag = "Added", status = String.Empty
            };
            string tempString =
                $"{newListData.linesIndex} █ {newListData.command} █ {newListData.value}";
            tempAddedString.Add(tempString);
            addedIndex = isTop ? addedIndex : addedIndex + 1;
            if (isLast && !isTop)
            {
                translatedListDataList.Add(newListData);
            }
            else
            {
                translatedListDataList.Insert(addedIndex, newListData);
            }
            RefreshList();
        }
        #endregion
        
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void VisualizerTabPage_OnLostFocus(object sender, RoutedEventArgs e)
        {
            StopPlaying();
        }
        #endregion
        
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
}