using BepInEx.Configuration;
using CellMenu;
using HarmonyLib;
using Steamworks;
using System;
using TMPro;
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

        private static void CM_PlayerLobbyBar__UpdatePlayer(CM_PlayerLobbyBar __instance)
        {
            var handler = __instance.m_nickText.GetComponent<SteamProfileClickHandler>();
            handler.enabled = true;
        }

        private static void CM_PageBase__UpdateButtonPress(CM_PageBase __instance)
        {
            var clickDown = InputMapper.GetButtonDown.Invoke(InputAction.MenuClick, eFocusState.None);
            var clickAltDown = InputMapper.GetButtonDown.Invoke(InputAction.MenuClickAlternate, eFocusState.None);
            if (!clickDown && !clickAltDown) return;

            if (__instance.TryCast<CM_PageLoadout>() == null) return;

            if (!__instance.m_guiLayer.GuiLayerBase.m_cellUICanvas.Raycast(__instance.CursorWorldPosition, out var rayHit)) return;

            var comp = rayHit.collider.GetComponent<SteamProfileClickHandler>();
            if (comp == null) return;
            Instance.LogDebug($"Hit {comp.GetInstanceID()}");

            var playerBar = rayHit.collider.GetComponentInParent<CM_PlayerLobbyBar>();
            if (playerBar?.m_player == null) return;

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

        private static void CM_PlayerLobbyBar__SetupFromPage(CM_PlayerLobbyBar __instance)
        {
            var comp = __instance.m_nickText.gameObject.GetComponent<SteamProfileClickHandler>();
            if (comp == null)
            {
                comp = __instance.m_nickText.gameObject.AddComponent<SteamProfileClickHandler>();
                comp.gameObject.AddComponent<BoxCollider2D>();
                Instance.LogDebug($"Added SteamProfileClickHandler #{comp.GetInstanceID()} to {__instance.name}");
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
                collider.offset = new Vector2(width / 2f, height / 2f);

                this.enabled = false;
            }
        }
    }
}
