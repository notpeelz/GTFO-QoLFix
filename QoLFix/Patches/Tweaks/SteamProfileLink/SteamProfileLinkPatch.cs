using BepInEx.Configuration;
using CellMenu;
using HarmonyLib;
using QoLFix.Patches.Common;
using SNetwork;
using Steamworks;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace QoLFix.Patches.Tweaks
{
    public partial class SteamProfileLinkPatch : IPatch
    {
        private static readonly string PatchName = nameof(SteamProfileLinkPatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Lets you open the steam profile of your teammates by clicking on their name."));
        }

        public string Name => PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            ClassInjector.RegisterTypeInIl2Cpp<SteamProfileClickHandler>();

            this.PatchMethod<CM_PlayerLobbyBar>(nameof(CM_PlayerLobbyBar.UpdatePlayer), PatchType.Postfix);
            this.PatchMethod<CM_PlayerLobbyBar>(nameof(CM_PlayerLobbyBar.SetupFromPage), PatchType.Postfix);
            this.PatchMethod<PUI_Inventory>(nameof(PUI_Inventory.Setup), new[] { typeof(GuiLayer) }, PatchType.Postfix);
            this.PatchMethod<CM_PageBase>(nameof(CM_PageBase.UpdateButtonPress), PatchType.Postfix);
        }

        private static void CM_PlayerLobbyBar__UpdatePlayer__Postfix(CM_PlayerLobbyBar __instance)
        {
            var handler = __instance.m_nickText.GetComponent<SteamProfileClickHandler>();
            handler.enabled = true;
        }

        private static void CM_PageBase__UpdateButtonPress__Postfix(CM_PageBase __instance)
        {
            if (__instance.TryCast<CM_PageLoadout>() == null
                && __instance.TryCast<CM_PageMap>() == null) return;

            var isHovering = false;
            try
            {
                var point = __instance.m_cursor.WorldPos;

                if (!__instance.m_guiLayer.GuiLayerBase.m_cellUICanvas.Raycast(point, out var rayHit)) return;

                var comp = rayHit.collider.GetComponent<SteamProfileClickHandler>();
                if (comp == null) return;
                isHovering = true;

                var player = GetPlayerInfo(rayHit.collider.gameObject);
                if (player == null) return;

                if (!InputMapper.GetButtonDown.Invoke(InputAction.MenuClick, eFocusState.None)) return;

                Instance.LogInfo($"Opening steam profile for {player.NickName} ({player.Lookup})");

                var url = $"https://steamcommunity.com/profiles/{player.Lookup}";
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
                __instance.SetCursorHovering(isHovering);
            }

            static SNet_Player GetPlayerInfo(GameObject go)
            {
                var playerLobbyBar = go.GetComponentInParent<CM_PlayerLobbyBar>();
                if (playerLobbyBar != null) return playerLobbyBar.m_player;

                var playerInventory = go.GetComponentInParent<PUI_Inventory>();
                if (playerInventory != null) return playerInventory.m_owner;

                return null;
            }
        }

        private static void CM_PlayerLobbyBar__SetupFromPage__Postfix(CM_PlayerLobbyBar __instance) =>
            InitializeClickHandler(__instance.m_nickText.gameObject);

        private static void PUI_Inventory__Setup__Postfix(PUI_Inventory __instance)
        {
            var go = __instance.m_headerRoot.transform.Find("Background")?.gameObject;
            var bg = go?.GetComponent<SpriteRenderer>();

            if (bg == null)
            {
                Instance.LogError($"Failed to get {nameof(SpriteRenderer)} from {nameof(PUI_Inventory)}/Background");
                return;
            }

            InitializeClickHandler(go, new Vector2(-bg.size.x / 2f, 0), bg.size);
        }

        private static void InitializeClickHandler(GameObject go, Vector2? offset = null, Vector2? size = null)
        {
            var comp = go.GetComponent<SteamProfileClickHandler>();
            if (comp == null)
            {
                comp = go.gameObject.AddComponent<SteamProfileClickHandler>();
                var collider = comp.gameObject.AddComponent<BoxCollider2D>();
                if (offset != null) collider.offset = (Vector2)offset;
                if (size != null) collider.size = (Vector2)size;
                comp.enabled = false;
            }
        }
    }
}
