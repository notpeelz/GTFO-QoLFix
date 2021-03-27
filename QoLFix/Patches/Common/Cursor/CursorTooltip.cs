using System;
using QoLFix.UI;
using UnityEngine;

namespace QoLFix.Patches.Common.Cursor
{
    public class CursorTooltip
    {
        private SpriteRenderer bgSprite;
        private RectTransform bgTransform;
        private GameObject content;
        private RectTransform contentTransform;

        public GameObject GameObject => this.BackgroundSprite.transform.parent.gameObject;

        public SpriteRenderer BackgroundSprite => this.GetOrCreateTooltip();

        public Vector2 MinScale { get; set; } = Vector2.zero;

        public Vector2 MaxScale { get; set; } = Vector2.positiveInfinity;

        public IScheduledAction ResizeAction { get; set; }

        public GameObject Content
        {
            get => this.content;
            internal set
            {
                this.content = value;
                this.contentTransform = value?.GetComponent<RectTransform>();
            }
        }

        public void Resize(Vector2? minScale = null, Vector2? maxScale = null)
        {
            this.MinScale = minScale ?? Vector2.zero;
            this.MaxScale = maxScale ?? Vector2.positiveInfinity;
            this.PerformResize();
        }

        public void PerformResize()
        {
            var size = this.contentTransform?.GetSize() ?? Vector2.zero;

            size.x = Math.Clamp(size.x, this.MinScale.x, this.MaxScale.x);
            size.y = Math.Clamp(size.y, this.MinScale.y, this.MaxScale.y);
            QoLFixPlugin.LogDebug($"Tooltip size: ({size.x}, {size.y})");

            this.GetOrCreateTooltip();
            this.bgTransform.localScale = new Vector3(size.x / 2f, size.y / 2f, 1f);
        }

        private SpriteRenderer GetOrCreateTooltip()
        {
            if (this.bgSprite != null) return this.bgSprite;

            var tooltip = GOFactory.CreateObject("CursorTooltip", null,
                out RectTransform t,
                out CanvasGroup _);
            tooltip.layer = LayerManager.LAYER_UI;
            tooltip.SetActive(false);

            t.anchorMin = new Vector2(0.5f, 0.5f);
            t.anchorMax = new Vector2(0.5f, 0.5f);
            t.pivot = new Vector2(0.5f, 0.5f);
            t.offsetMin = Vector2.zero;
            t.offsetMax = Vector2.zero;
            t.localPosition = Vector2.zero;

            var bgGO = GOFactory.CreateObject("Background", tooltip.transform,
                out this.bgTransform,
                out SpriteRenderer r);
            bgGO.layer = LayerManager.LAYER_UI;

            r.sortingOrder = 9999;
            r.color = new Color(0.4f, 0.4f, 0.4f, 1);

            this.bgTransform.pivot = new Vector2(0.5f, 0.5f);
            this.bgTransform.localPosition = Vector2.zero;

            var tex = Resources.Load<Texture2D>("gui/gear/frames/cellUI_Frame_BoxFiled");
            r.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), t.pivot, 100f);

            this.bgSprite = r;

            return r;
        }
    }
}
