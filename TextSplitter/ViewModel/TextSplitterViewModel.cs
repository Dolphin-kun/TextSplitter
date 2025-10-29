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
        private IItem? _selectedItem;

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
            if (_selectedItem is null) return;
            if (_undoRedoManager is null) return;

            string? textToSplit = null;
            if(_selectedItem is TextItem textItem)
            {
                textToSplit = textItem.Text;
            }
            else if(_selectedItem is VoiceItem voiceItem)
            {
                textToSplit = voiceItem.Serif;
            }

            if (string.IsNullOrEmpty(textToSplit)) return;

            var settings = TextSplitterSettings.Default;

            int startFrame = _selectedItem.Frame;
            int startLayer = settings.IsDeleteOriginalItem
                ? _selectedItem.Layer
                : _selectedItem.Layer + 1;

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
                baseNewLength = _selectedItem.Length / totalSplitItems;
                remainder = _selectedItem.Length % totalSplitItems;
            }

            int i = 0;
            int currentFrame = (settings.SplitDirection == SplitDirection.Horizontal && !settings.IsDeleteOriginalItem)
                ? startFrame + _selectedItem.Length
                : startFrame;
            int targetStartFrame = currentFrame;

            foreach (string textElement in validTextElements)
            {
                var newItem = _selectedItem.GetClone();
                if (newItem is TextItem newTextItem)
                {
                    newTextItem.Text = textElement;
                }
                else if(newItem is VoiceItem newVoiceItem)
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
                _timeline.DeleteItems([_selectedItem]);
                _timeline.TryAddItems(itemsArray, targetStartFrame, startLayer);
            }
            else
            {
                _timeline.TryAddItems(itemsArray, targetStartFrame, startLayer);
            }
            
            _undoRedoManager.Record();
        }

        private bool CanSplitText()
        {
            string? text = null;
            if (_selectedItem is TextItem textItem)
            {
                text = textItem.Text;
            }
            else if (_selectedItem is VoiceItem voiceItem)
            {
                text = voiceItem.Serif;
            }

            return !string.IsNullOrEmpty(text);
        }

        public void SetTimelineToolInfo(TimelineToolInfo info)
        {
            if (_timeline != null) _timeline.PropertyChanged -= Timeline_PropertyChanged;
            if(_selectedItem is INotifyPropertyChanged oldItem)
            {
                oldItem.PropertyChanged -= SelectedItem_PropertyChanged;
            }

            _selectedItem = null;
            _timeline = info.Timeline;
            _undoRedoManager = info.UndoRedoManager;

            if (_timeline != null)
            {
                _timeline.PropertyChanged += Timeline_PropertyChanged;
                UpdateSelectedItem(_timeline.SelectedItem);
            }
        }

        private void Timeline_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Timeline.SelectedItem))
            {
                UpdateSelectedItem(_timeline?.SelectedItem);
            }
        }

        private void UpdateSelectedItem(IItem? newItem)
        {
            if (_selectedItem is INotifyPropertyChanged oldItem)
            {
                oldItem.PropertyChanged -= SelectedItem_PropertyChanged;
            }

            if (newItem is TextItem newTextItem)
            {
                _selectedItem = newTextItem;
                newTextItem.PropertyChanged += SelectedItem_PropertyChanged;
            }
            else if (newItem is VoiceItem newVoiceItem)
            {
                _selectedItem = newVoiceItem;
                newVoiceItem.PropertyChanged += SelectedItem_PropertyChanged;
            }
            else
            {
                _selectedItem = null;
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
