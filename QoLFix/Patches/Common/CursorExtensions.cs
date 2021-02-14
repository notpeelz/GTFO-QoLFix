using System;
using System.Collections.Generic;
using CellMenu;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace QoLFix.Patches.Common
{
    public static class CursorExtensions
    {
        private class CursorState
        {
            public CursorState() { }

            public CM_Cursor Cursor { get; set; }

            public bool Hovering { get; set; }

            public SpriteRenderer HoveringSprite { get; set; }
        }

        private static readonly Dictionary<int, CursorState> CursorStates = new Dictionary<int, CursorState>();

        public static void SetCursorHovering(this CM_PageBase page, bool hovering)
        {
            var pageId = page.GetInstanceID();
            if (!CursorStates.TryGetValue(pageId, out var state))
            {
                state = CursorStates[pageId] = new CursorState();
            }
            try
            {
                if (state.Cursor == null
                    || state.Cursor.Pointer == IntPtr.Zero
                    || state.Cursor.GetInstanceID() != page.m_cursor?.GetInstanceID())
                {
                    UpdateCursorRef();
                }
            }
            catch (ObjectCollectedException)
            {
                UpdateCursorRef();
            }

            if (state.Hovering == hovering) return;
            state.Hovering = hovering;

            QoLFixPlugin.LogDebug($"<{nameof(SetCursorHovering)}> UpdateCursor: {(hovering ? "hovering" : "not hovering")}");

            if (hovering)
            {
                page.m_cursor.m_cursorSprite.GetComponent<SpriteRenderer>().enabled = false;
                page.m_cursor.m_cursorSpriteDrag.GetComponent<SpriteRenderer>().enabled = false;
                state.HoveringSprite.gameObject.SetActive(true);
            }
            else
            {
                page.m_cursor.m_cursorSprite.GetComponent<SpriteRenderer>().enabled = true;
                page.m_cursor.m_cursorSpriteDrag.GetComponent<SpriteRenderer>().enabled = true;
                state.HoveringSprite.gameObject.SetActive(false);
            }

            void UpdateCursorRef()
            {
                QoLFixPlugin.LogDebug($"<{nameof(SetCursorHovering)}> UpdateCursorRef");

                state.Cursor = page.m_cursor;
                if (state.HoveringSprite != null)
                {
                    UnityEngine.Object.Destroy(state.HoveringSprite.gameObject);
                }
                var pointerGO = new GameObject("Pointer", new[]
                {
                    Il2CppType.Of<RectTransform>(),
                    Il2CppType.Of<CanvasRenderer>(),
                    Il2CppType.Of<SpriteRenderer>()
                });
                pointerGO.SetActive(false);
                pointerGO.layer = LayerManager.LAYER_UI;

                var t = pointerGO.GetComponent<RectTransform>();
                var r = pointerGO.GetComponent<SpriteRenderer>();

                t.localScale = new Vector3(8f, 8f, 8f);
                t.localPosition = new Vector3(7f, -20f, 0.33f);
                t.anchorMin = new Vector2(0.5f, 0.5f);
                t.anchorMax = new Vector2(0.5f, 0.5f);
                t.pivot = new Vector2(0.5f, 0.5f);

                var tex = Resources.Load<Texture2D>("gui/crosshairs/clicker");
                r.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), t.pivot, 100f);

                state.HoveringSprite = r;
                t.SetParent(state.Cursor.transform, false);
            }
        }
    }
}
