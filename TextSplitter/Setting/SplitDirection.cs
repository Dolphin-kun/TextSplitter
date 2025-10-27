using System.ComponentModel.DataAnnotations;

namespace TextSplitter.Setting
{
    public enum SplitDirection
    {
        [Display(Name ="縦",Description ="レイヤー方向")]
        Vertical,

        [Display(Name ="横",Description ="フレーム方向")]
        Horizontal,
    }
}
