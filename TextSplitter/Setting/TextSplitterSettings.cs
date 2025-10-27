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

        public override void Initialize()
        {
        }
    }
}
