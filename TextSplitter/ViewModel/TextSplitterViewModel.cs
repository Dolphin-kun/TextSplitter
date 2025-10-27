using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using TextSplitter.Setting;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace TextSplitter.ViewModel
{
    internal class TextSplitterViewModel : INotifyPropertyChanged, ITimelineToolViewModel
    {
        private Timeline? _timeline;
        private TextItem? _selectedTextItem;

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
            if (_selectedTextItem is null) return;

            string textToSplit = _selectedTextItem.Text;
            if (string.IsNullOrEmpty(textToSplit)) return;

            var settings = TextSplitterSettings.Default;

            int startFrame = _selectedTextItem.Frame;
            int startLayer = settings.IsDeleteOriginalItem
                ? _selectedTextItem.Layer
                : _selectedTextItem.Layer + 1;

            var itemsToAdd = new List<IItem>();
            var textElements = StringInfo.GetTextElementEnumerator(textToSplit);

            int i = 0;
            int currentFrame = (settings.SplitDirection == SplitDirection.Horizontal && !settings.IsDeleteOriginalItem)
                ? startFrame + _selectedTextItem.Length
                : startFrame;
            int targetStartFrame = currentFrame;
            while (textElements.MoveNext())
            {
                string textElement = textElements.GetTextElement();

                if (string.IsNullOrWhiteSpace(textElement)) continue;

                var newItem = _selectedTextItem.GetClone();
                if (newItem is TextItem newTextItem)
                {
                    newTextItem.Text = textElement;
                }

                if (settings.SplitDirection == SplitDirection.Vertical)
                {
                    newItem.Frame = startFrame;
                    newItem.Layer = startLayer + i;
                }
                else if (settings.SplitDirection == SplitDirection.Horizontal)
                {
                    newItem.Frame = currentFrame;
                    newItem.Layer = startLayer;

                    currentFrame += newItem.Length;
                }

                itemsToAdd.Add(newItem);
                i++;
            }
            if (itemsToAdd.Count == 0) return;
            IItem[] itemsArray = [.. itemsToAdd];

            if (settings.IsDeleteOriginalItem)
            {
                _timeline.PropertyChanged -= Timeline_PropertyChanged;

                _timeline.DeleteItems([_selectedTextItem]);
                _timeline.TryAddItems(itemsArray, targetStartFrame, startLayer, false);
                _timeline.SelectItem(itemsArray.FirstOrDefault());

                _timeline.PropertyChanged += Timeline_PropertyChanged;
            }
            else
            {
                _timeline.TryAddItems(itemsArray, targetStartFrame, startLayer, true);
            }
        }

        private bool CanSplitText()
        {
            if (_selectedTextItem is not null)
                return !string.IsNullOrEmpty(_selectedTextItem.Text);

            return false;
        }

        public void SetTimelineToolInfo(TimelineToolInfo info)
        {
            if (_timeline != null) _timeline.PropertyChanged -= Timeline_PropertyChanged;
            if(_selectedTextItem != null)
            {
                _selectedTextItem.PropertyChanged -= SelectedTextItem_PropertyChanged;
            }

            _timeline = info.Timeline;

            if (_timeline != null)
            {
                _timeline.PropertyChanged += Timeline_PropertyChanged;

                if (_timeline.SelectedItem is TextItem newTextItem)
                {
                    _selectedTextItem = newTextItem;
                    _selectedTextItem.PropertyChanged += SelectedTextItem_PropertyChanged;
                }
            }
        }

        private void Timeline_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Timeline.SelectedItem))
            {
                if (_selectedTextItem != null) _selectedTextItem.PropertyChanged -= SelectedTextItem_PropertyChanged;
                if (_timeline?.SelectedItem is TextItem newTextItem)
                {
                    _selectedTextItem = newTextItem;
                    _selectedTextItem.PropertyChanged += SelectedTextItem_PropertyChanged;
                }
                else
                {
                    _selectedTextItem = null;
                }

                (SplitTextCommand as ActionCommand)?.RaiseCanExecuteChanged();
            }
        }

        private void SelectedTextItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TextItem.Text))
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
