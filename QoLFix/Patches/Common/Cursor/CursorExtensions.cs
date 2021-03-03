using System;
using System.Collections.Generic;
using CellMenu;
using QoLFix.UI;
using UnhollowerBaseLib;
using UnityEngine;

namespace QoLFix.Patches.Common.Cursor
{
    public static class CursorExtensions
    {
        public class CursorState
        {
            private SpriteRenderer tooltipSprite;
            private RectTransform tooltipTransform;
            private GameObject tooltipContent;
            private RectTransform tooltipContentTransform;
            private Vector2 minScale = Vector2.zero;
            private Vector2 maxScale = Vector2.positiveInfinity;

            internal CursorState() { }

            public GameObject Tooltip => this.TooltipSprite.transform.parent.gameObject;

            public SpriteRenderer TooltipSprite => this.GetOrCreateTooltip();

            public Vector3 TooltipScale
            {
                get
                {
                    this.GetOrCreateTooltip();
                    return this.tooltipTransform.localScale;
                }
                internal set
                {
                    this.GetOrCreateTooltip();
                    this.tooltipTransform.localScale = value;
                }
            }

            public GameObject TooltipContent
            {
                get => this.tooltipContent;
                internal set
                {
                    this.tooltipContent = value;
                    this.tooltipContentTransform = value?.GetComponent<RectTransform>();
                }
            }

            public SpriteRenderer HandSprite { get; internal set; }

            public CM_Cursor Cursor { get; internal set; }

            public CursorStyle Style { get; internal set; }

            private SpriteRenderer GetOrCreateTooltip()
            {
                if (this.tooltipSprite != null) return this.tooltipSprite;

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
                    out this.tooltipTransform,
                    out SpriteRenderer r);
                bgGO.layer = LayerManager.LAYER_UI;

                r.sortingOrder = 9999;
                r.color = new Color(0.4f, 0.4f, 0.4f, 1);

                this.tooltipTransform.pivot = new Vector2(0.5f, 0.5f);
                this.tooltipTransform.localPosition = Vector2.zero;

                var tex = Resources.Load<Texture2D>("gui/gear/frames/cellUI_Frame_BoxFiled");
                r.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), t.pivot, 100f);

                this.tooltipSprite = r;

                return r;
            }

            public void ResizeTooltip(Vector2? minScale = null, Vector2? maxScale = null)
            {
                this.minScale = minScale ?? Vector2.zero;
                this.maxScale = maxScale ?? Vector2.positiveInfinity;
                this.PerformResize();
            }

            public void PerformResize()
            {
                var size = this.tooltipContentTransform?.GetSize() ?? Vector2.zero;

                size.x = Math.Clamp(size.x, this.minScale.x, this.maxScale.x);
                size.y = Math.Clamp(size.y, this.minScale.y, this.maxScale.y);
                QoLFixPlugin.LogDebug($"Tooltip size: ({size.x}, {size.y})");

                this.TooltipScale = new Vector3(size.x / 2f, size.y / 2f, 1f);
            }
        }

        private static readonly Dictionary<int, CursorState> CursorStates = new();

        public static void SetCursorTooltip(this CM_PageBase page, GameObject content, Vector2? minScale = null, Vector2? maxScale = null, bool updateOnNextFrame = true)
        {
            var state = page.GetCursorState();

            if (state.TooltipContent != null)
            {
                state.TooltipContent.transform.SetParent(null, false);
            }

            state.TooltipContent = content;

            state.Tooltip.transform.SetParent(page.transform, false);
            state.Tooltip.SetActive(false);

            if (content != null)
            {
                var canvasGroup = state.Tooltip.GetComponent<CanvasGroup>();
                UpdateContent();

                if (updateOnNextFrame)
                {
                    canvasGroup.alpha = 0;
                    UIManager.OnNextFrame += UpdateContent;
                }

                void UpdateContent()
                {
                    state.TooltipContent.transform.SetParent(state.Tooltip.transform, false);
                    state.Tooltip.SetActive(true);
                    state.ResizeTooltip(minScale, maxScale);
                    canvasGroup.alpha = 1;
                }
            }
        }

        public static CursorState GetCursorState(this CM_PageBase page)
        {
            var pageId = page.GetInstanceID();
            if (!CursorStates.TryGetValue(pageId, out var state))
            {
                state = CursorStates[pageId] = new CursorState();
            }

            return state;
        }

        public static void SetCursorStyle(this CM_PageBase page, CursorStyle style)
        {
            var state = page.GetCursorState();

            try
            {
                if (state.Cursor == null
                    || state.Cursor.Pointer == IntPtr.Zero
                    || state.Cursor.GetInstanceID() != page.m_cursor?.GetInstanceID())
                {
                    UpdateCursorRef(page, state);
                }
            }
            catch (ObjectCollectedException)
            {
                UpdateCursorRef(page, state);
            }

            if (state.Style == style) return;
            state.Style = style;

            switch (style)
            {
                case CursorStyle.Hand:
                    page.m_cursor.m_cursorSprite.enabled = false;
                    page.m_cursor.m_cursorSpriteDrag.enabled = false;
                    state.HandSprite.gameObject.SetActive(true);
                    break;
                default:
                    page.m_cursor.m_cursorSprite.enabled = true;
                    page.m_cursor.m_cursorSpriteDrag.enabled = true;
                    state.HandSprite.gameObject.SetActive(false);
                    break;
            }
        }

        private static void UpdateCursorRef(CM_PageBase page, CursorState state)
        {
            QoLFixPlugin.LogDebug("UpdateCursorRef");

            state.Cursor = page.m_cursor;

            var handGO = GOFactory.CreateObject("Hand", state.Cursor.transform,
                out RectTransform t,
                out SpriteRenderer r);

            handGO.SetActive(false);
            handGO.layer = LayerManager.LAYER_UI;

            t.localScale = new Vector3(8f, 8f, 8f);
            t.localPosition = new Vector3(7f, -20f, 0.33f);
            t.anchorMin = new Vector2(0.5f, 0.5f);
            t.anchorMax = new Vector2(0.5f, 0.5f);
            t.pivot = new Vector2(0.5f, 0.5f);

            var tex = Resources.Load<Texture2D>("gui/crosshairs/clicker");
            r.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), t.pivot, 100f);

            state.HandSprite = r;
        }
    }
}
