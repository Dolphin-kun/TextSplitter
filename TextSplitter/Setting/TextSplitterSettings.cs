using YukkuriMovieMaker.Plugin;

namespace TextSplitter.Setting
{
    internal class TextSplitterSettings : SettingsBase<TextSplitterSettings>
    {
        public override SettingsCategory Category => SettingsCategory.None;
        public override string Name => "テキスト分割";

        public override bool HasSettingView => true;
        public override object? SettingView => new TextSplitterSettingsView();

        private bool isDeleteOriginalItem = false;
        public bool IsDeleteOriginalItem { get => isDeleteOriginalItem; set => Set(ref isDeleteOriginalItem, value); }

        private SplitDirection splitDirection = SplitDirection.Vertical;
        public SplitDirection SplitDirection { get => splitDirection; set => Set(ref splitDirection, value); }

        private SplitMode splitMode = SplitMode.PerCharacter;
        public SplitMode SplitMode { get => splitMode; set => Set(ref splitMode, value); }

        // Vertical
        private int frameOffset = 0;
        public int FrameOffset { get => frameOffset; set => Set(ref frameOffset, value); }

        // Horizontal
        private bool isKeepLength = false;
        public bool IsKeepLength { get => isKeepLength; set => Set(ref isKeepLength, value); }

        public override void Initialize()
        {
        }
    }
}
