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
        private static readonly Dictionary<int, CursorState> CursorStates = new();

        public static void SetCursorTooltip(this CM_PageBase page, GameObject content, Vector2? minScale = null, Vector2? maxScale = null, bool updateOnNextFrame = true)
        {
            var state = page.GetCursorState();

            // Make sure we cancel pending resize actions
            state.Tooltip.ResizeAction?.Invalidate();

            if (state.Tooltip.Content != null)
            {
                state.Tooltip.Content.transform.SetParent(null, false);
            }

            state.Tooltip.Content = content;

            state.Tooltip.GameObject.transform.SetParent(page.transform, false);
            state.Tooltip.GameObject.SetActive(false);

            if (content != null)
            {
                var canvasGroup = state.Tooltip.GameObject.GetComponent<CanvasGroup>();

                UpdateContent();

                if (updateOnNextFrame)
                {
                    // FIXME: for some reason, CanvasGroup::alpha doesn't
                    // seem to have any effect on the tooltip.
                    canvasGroup.alpha = 0;
                    state.Tooltip.ResizeAction = ActionScheduler.Schedule(UpdateContent);
                }

                void UpdateContent()
                {
                    state.Tooltip.Content.transform.SetParent(state.Tooltip.GameObject.transform, false);
                    state.Tooltip.GameObject.SetActive(true);
                    state.Tooltip.Resize(minScale, maxScale);
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
