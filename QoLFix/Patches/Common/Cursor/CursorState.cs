using UnityEngine;

namespace QoLFix.Patches.Common.Cursor
{
    public class CursorState
    {
        internal CursorState() { }

        public SpriteRenderer HandSprite { get; internal set; }

        public CM_Cursor Cursor { get; internal set; }

        public CursorStyle Style { get; internal set; }

        public CursorTooltip Tooltip { get; } = new();
    }
}
