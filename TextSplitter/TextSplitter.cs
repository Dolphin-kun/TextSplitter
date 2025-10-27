using TextSplitter.View;
using TextSplitter.ViewModel;
using YukkuriMovieMaker.Plugin;

namespace TextSplitter
{
    public class TextSplitter : IToolPlugin
    {
        public string Name => "テキスト分割";
        public Type ViewModelType => typeof(TextSplitterViewModel);
        public Type ViewType => typeof(TextSplitterControl);
    }
}
