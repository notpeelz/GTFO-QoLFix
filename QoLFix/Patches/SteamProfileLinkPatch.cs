using BepInEx.Configuration;
using CellMenu;
using HarmonyLib;
using Steamworks;
using System;
using TMPro;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace QoLFix.Patches
{
    public class SteamProfileLinkPatch : IPatch
    {
        private static readonly string PatchName = nameof(SteamProfileLinkPatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Lets you open the steam profile of your teammates by clicking on their name (only works in lobby)."));
        }

        public string Name => PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public void Patch(Harmony harmony)
        {
            ClassInjector.RegisterTypeInIl2Cpp<SteamProfileClickHandler>();

            {
                var methodInfo = typeof(CM_PlayerLobbyBar).GetMethod(nameof(CM_PlayerLobbyBar.UpdatePlayer));
                harmony.Patch(methodInfo, postfix: new HarmonyMethod(AccessTools.Method(typeof(SteamProfileLinkPatch), nameof(CM_PlayerLobbyBar__UpdatePlayer))));
            }
            {
                var methodInfo = typeof(CM_PlayerLobbyBar).GetMethod(nameof(CM_PlayerLobbyBar.SetupFromPage));
                harmony.Patch(methodInfo, postfix: new HarmonyMethod(AccessTools.Method(typeof(SteamProfileLinkPatch), nameof(CM_PlayerLobbyBar__SetupFromPage))));
            }
            {
                var methodInfo = typeof(CM_PageBase).GetMethod(nameof(CM_PageBase.UpdateButtonPress));
                harmony.Patch(methodInfo, postfix: new HarmonyMethod(AccessTools.Method(typeof(SteamProfileLinkPatch), nameof(CM_PageBase__UpdateButtonPress))));
            }
        }

        private static SpriteRenderer CursorPointerSprite { get; set; }

        private static CM_Cursor Cursor { get; set; }

        private static bool IsHovering { get; set; }

        private static void CM_PlayerLobbyBar__UpdatePlayer(CM_PlayerLobbyBar __instance)
        {
            var handler = __instance.m_nickText.GetComponent<SteamProfileClickHandler>();
            handler.enabled = true;
        }

        private static void CM_PageBase__UpdateButtonPress(CM_PageBase __instance)
        {
            if (__instance.TryCast<CM_PageLoadout>() == null) return;

            var isHovering = false;
            try
            {
                if (!__instance.m_guiLayer.GuiLayerBase.m_cellUICanvas.Raycast(__instance.CursorWorldPosition, out var rayHit)) return;

                var comp = rayHit.collider.GetComponent<SteamProfileClickHandler>();
                if (comp == null) return;
                isHovering = true;

                var playerBar = rayHit.collider.GetComponentInParent<CM_PlayerLobbyBar>();
                if (playerBar?.m_player == null) return;

                if (!InputMapper.GetButtonDown.Invoke(InputAction.MenuClick, eFocusState.None)) return;

                Instance.LogInfo($"Opening steam profile for {playerBar.m_player.NickName} ({playerBar.m_player.Lookup})");

                var url = $"https://steamcommunity.com/profiles/{playerBar.m_player.Lookup}";
                if (SteamUtils.IsOverlayEnabled())
                {
                    SteamFriends.ActivateGameOverlayToWebPage(url);
                }
                else
                {
                    Application.OpenURL(url);
                }
            }
            finally
            {
                UpdateCursor();
            }

            void UpdateCursor()
            {
                try
                {
                    if (Cursor == null
                        || Cursor.Pointer == IntPtr.Zero
                        || Cursor.GetInstanceID() != __instance.m_cursor?.GetInstanceID())
                    {
                        UpdateCursorRef();
                    }
                }
                catch (ObjectCollectedException)
                {
                    UpdateCursorRef();
                }

                if (CursorPointerSprite == null) return; // Not sure why this would happen, but check just in case
                if (IsHovering == isHovering) return;
                IsHovering = isHovering;

                Instance.LogDebug("UpdateCursor");

                if (isHovering)
                {
                    __instance.m_cursor.m_cursorSprite.gameObject.SetActive(false);
                    __instance.m_cursor.m_cursorSpriteDrag.gameObject.SetActive(false);
                    CursorPointerSprite.gameObject.SetActive(true);
                }
                else
                {
                    __instance.m_cursor.m_cursorSprite.gameObject.SetActive(true);
                    CursorPointerSprite.gameObject.SetActive(false);
                }

                void UpdateCursorRef()
                {
                    Instance.LogDebug("UpdateCursorRef");

                    Cursor = __instance.m_cursor;
                    if (CursorPointerSprite != null)
                    {
                        UnityEngine.Object.Destroy(CursorPointerSprite);
                        UnityEngine.Object.Destroy(CursorPointerSprite.gameObject);
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
                    t.localScale = new Vector3(8f, 8f, 8f);
                    t.anchorMin = new Vector2(0.5f, 0.5f);
                    t.anchorMax = new Vector2(0.5f, 0.5f);
                    t.pivot = new Vector2(0.5f, 0.5f);

                    var r = pointerGO.GetComponent<SpriteRenderer>();

                    var tex = Resources.Load<Texture2D>("gui/crosshairs/clicker");
                    r.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), t.pivot, 100f);

                    t.localPosition = new Vector3(0, -t.rect.height / 7f, 0.33f);

                    CursorPointerSprite = r;
                    t.SetParent(Cursor.m_cursorSprite.gameObject.transform.parent, false);
                }
            }
        }

        private static void CM_PlayerLobbyBar__SetupFromPage(CM_PlayerLobbyBar __instance)
        {
            var comp = __instance.m_nickText.gameObject.GetComponent<SteamProfileClickHandler>();
            if (comp == null)
            {
                comp = __instance.m_nickText.gameObject.AddComponent<SteamProfileClickHandler>();
                comp.gameObject.AddComponent<BoxCollider2D>();
            }
            comp.enabled = false;
        }

        private class SteamProfileClickHandler : MonoBehaviour
        {
            public SteamProfileClickHandler(IntPtr value)
                : base(value) { }

            private void Update()
            {
                var collider = this.GetComponent<BoxCollider2D>();

                var nickText = this.GetComponent<TextMeshPro>();
                if (nickText == null)
                {
                    Instance.LogError($"{nameof(SteamProfileClickHandler)} isn't attached to a {nameof(TextMeshPro)}");
                    this.enabled = false;
                    return;
                }

                var width = nickText.GetRenderedWidth(true);
                var height = nickText.GetRenderedHeight(true);
                collider.size = new Vector2(width, height);
                collider.offset = new Vector2(width / 2f, 0);

                this.enabled = false;
            }
        }
    }
}
