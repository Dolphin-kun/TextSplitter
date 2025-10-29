using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using TextSplitter.Setting;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace TextSplitter.ViewModel
{
    internal class TextSplitterViewModel : INotifyPropertyChanged, ITimelineToolViewModel
    {
        private Timeline? _timeline;
        private UndoRedoManager? _undoRedoManager;
        private IReadOnlyList<IItem> _selectedItems = [];

        public ICommand SplitTextCommand { get; }

        public TextSplitterViewModel()
        {
            SplitTextCommand = new ActionCommand(
                _ => CanSplitText(),
                _ => SplitText()
            );
        }

        private void SplitText()
        {
            if (_timeline is null) return;
            if (_undoRedoManager is null) return;
            if (_selectedItems.Count == 0) return;

            var settings = TextSplitterSettings.Default;

            var itemsToSplit = _selectedItems
                .Where(item => item is TextItem || item is VoiceItem)
                .ToList();

            if (itemsToSplit.Count == 0) return;

            foreach (var item in itemsToSplit)
            {
                SplitSingleItem(item, settings, _timeline);
            }

            _undoRedoManager.Record();
        }

        private static void SplitSingleItem(IItem selectedItem, TextSplitterSettings settings, Timeline timeline)
        {
            string? textToSplit = null;
            if (selectedItem is TextItem textItem)
            {
                textToSplit = textItem.Text;
            }
            else if (selectedItem is VoiceItem voiceItem)
            {
                textToSplit = voiceItem.Serif;
            }

            if (string.IsNullOrEmpty(textToSplit)) return;

            int startFrame = selectedItem.Frame;
            int startLayer = settings.IsDeleteOriginalItem
                ? selectedItem.Layer
                : selectedItem.Layer + 1;

            var itemsToAdd = new List<IItem>();
            IEnumerable<string> textElements;

            if (settings.SplitMode == SplitMode.PerLine)
            {
                textElements = textToSplit.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            }
            else
            {
                var elements = new List<string>();
                var enumerator = StringInfo.GetTextElementEnumerator(textToSplit);
                while (enumerator.MoveNext())
                {
                    elements.Add(enumerator.GetTextElement());
                }
                textElements = elements;
            }

            var validTextElements = textElements.Where(te => !string.IsNullOrWhiteSpace(te)).ToList();
            int totalSplitItems = validTextElements.Count;

            if (totalSplitItems == 0) return;

            int baseNewLength = 0;
            int remainder = 0;
            if (settings.IsKeepLength && settings.SplitDirection == SplitDirection.Horizontal && totalSplitItems > 0)
            {
                baseNewLength = selectedItem.Length / totalSplitItems;
                remainder = selectedItem.Length % totalSplitItems;
            }

            int i = 0;
            int currentFrame = (settings.SplitDirection == SplitDirection.Horizontal && !settings.IsDeleteOriginalItem)
                ? startFrame + selectedItem.Length
                : startFrame;
            int targetStartFrame = currentFrame;

            foreach (string textElement in validTextElements)
            {
                var newItem = selectedItem.GetClone();
                if (newItem is TextItem newTextItem)
                {
                    newTextItem.Text = textElement;
                }
                else if (newItem is VoiceItem newVoiceItem)
                {
                    newVoiceItem.Serif = textElement;
                }

                if (settings.SplitDirection == SplitDirection.Vertical)
                {
                    newItem.Frame = startFrame + (i * settings.FrameOffset);
                    newItem.Layer = startLayer + i;
                }
                else if (settings.SplitDirection == SplitDirection.Horizontal)
                {
                    newItem.Frame = currentFrame;
                    newItem.Layer = startLayer;

                    int itemLength;
                    if (settings.IsKeepLength)
                    {
                        itemLength = baseNewLength + (i < remainder ? 1 : 0);
                        newItem.Length = itemLength;
                    }
                    else
                    {
                        itemLength = newItem.Length;
                    }

                    currentFrame += itemLength;
                }

                itemsToAdd.Add(newItem);
                i++;
            }
            if (itemsToAdd.Count == 0) return;
            IItem[] itemsArray = [.. itemsToAdd];

            if (settings.IsDeleteOriginalItem)
            {
                timeline.DeleteItems([selectedItem]);
                timeline.TryAddItems(itemsArray, targetStartFrame, startLayer);
            }
            else
            {
                timeline.TryAddItems(itemsArray, targetStartFrame, startLayer);
            }
        }

        private bool CanSplitText()
        {
            return _selectedItems.Any(item =>
            {
                if (item is TextItem textItem)
                {
                    return !string.IsNullOrEmpty(textItem.Text);
                }
                if (item is VoiceItem voiceItem)
                {
                    return !string.IsNullOrEmpty(voiceItem.Serif);
                }
                return false;
            });
        }

        public void SetTimelineToolInfo(TimelineToolInfo info)
        {
            if (_timeline != null) _timeline.PropertyChanged -= Timeline_PropertyChanged;
            UpdateSelectedItems([]);

            _timeline = info.Timeline;
            _undoRedoManager = info.UndoRedoManager;

            if (_timeline != null)
            {
                _timeline.PropertyChanged += Timeline_PropertyChanged;
                UpdateSelectedItems(_timeline.SelectedItems);
            }
        }

        private void Timeline_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Timeline.SelectedItems))
            {
                UpdateSelectedItems(_timeline?.SelectedItems ?? []);
            }
        }

        private void UpdateSelectedItems(IReadOnlyList<IItem> newItems)
        {
            foreach (var oldItem in _selectedItems)
            {
                if (oldItem is INotifyPropertyChanged oldINotifyPropertyChanged)
                {
                    oldINotifyPropertyChanged.PropertyChanged -= SelectedItem_PropertyChanged;
                }
            }

            _selectedItems = newItems;

            foreach (var newItem in _selectedItems)
            {
                if (newItem is TextItem || newItem is VoiceItem)
                {
                    if (newItem is INotifyPropertyChanged newINotifyPropertyChanged)
                    {
                        newINotifyPropertyChanged.PropertyChanged += SelectedItem_PropertyChanged;
                    }
                }
            }

            (SplitTextCommand as ActionCommand)?.RaiseCanExecuteChanged();
        }

        private void SelectedItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TextItem.Text) || e.PropertyName == nameof(VoiceItem.Serif))
            {
                (SplitTextCommand as ActionCommand)?.RaiseCanExecuteChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
