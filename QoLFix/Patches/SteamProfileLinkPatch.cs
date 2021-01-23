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

        public void Initialize()
        {
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
                var methodInfo = typeof(CM_PlayerLobbyBar).GetMethod(nameof(CM_PlayerLobbyBar.OnBtnPressAnywhere));
                harmony.Patch(methodInfo, postfix: new HarmonyMethod(AccessTools.Method(typeof(SteamProfileLinkPatch), nameof(CM_PlayerLobbyBar__OnBtnPressAnywhere))));
            }
        }

        private static void CM_PlayerLobbyBar__UpdatePlayer(CM_PlayerLobbyBar __instance)
        {
            var handler = __instance.m_nickText.GetComponent<SteamProfileClickHandler>();
            handler.enabled = true;
        }

        private static void CM_PlayerLobbyBar__OnBtnPressAnywhere(CM_PlayerLobbyBar __instance)
        {
            if (__instance.m_player == null) return;

            if (__instance.m_parentPage.m_guiLayer.GuiLayerBase.m_cellUICanvas.Raycast(__instance.m_parentPage.m_cursor.WorldPos, out var rayHit))
            {
                var component = rayHit.collider.GetComponent<SteamProfileClickHandler>();

                if (component != null)
                {
                    QoLFixPlugin.Instance.Log.LogInfo($"Opening steam profile for {__instance.m_player.NickName} ({__instance.m_player.Lookup})");

                    var url = $"https://steamcommunity.com/profiles/{__instance.m_player.Lookup}";
                    if (SteamUtils.IsOverlayEnabled())
                    {
                        SteamFriends.ActivateGameOverlayToWebPage(url);
                    }
                    else
                    {
                        Application.OpenURL(url);
                    }
                }
            }
        }

        private static void CM_PlayerLobbyBar__SetupFromPage(CM_PlayerLobbyBar __instance)
        {
            var comp = __instance.m_nickText.gameObject.AddComponent<SteamProfileClickHandler>();
            comp.NickText = __instance.m_nickText;
            comp.enabled = false;
            comp.gameObject.AddComponent<BoxCollider2D>();
        }

        private class SteamProfileClickHandler : MonoBehaviour
        {
            public SteamProfileClickHandler(IntPtr value)
                : base(value) { }

            public TextMeshPro NickText { get; set; }

            private void Update()
            {
                var collider = this.NickText.GetComponent<SteamProfileClickHandler>().GetComponent<BoxCollider2D>();

                var width = this.NickText.GetRenderedWidth(true);
                collider.size = new Vector2(width, this.NickText.GetRenderedHeight(true));
                collider.offset = new Vector2(width / 2f, 0f);

                this.enabled = false;
            }
        }
    }
}
