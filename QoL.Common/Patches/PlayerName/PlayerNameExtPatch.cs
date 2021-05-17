using System;
using CellMenu;
using HarmonyLib;
using MTFO.Core;
using SNetwork;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace QoL.Common.Patches
{
    public partial class PlayerNameExtPatch : MTFOPatch
    {
        public static PlayerNameExtPatch? Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            ClassInjector.RegisterTypeInIl2Cpp<CursorInteraction>();
        }

        public delegate void CursorInteractionHandler(
            CM_PageBase page,
            Vector2 cursorPos,
            ref RaycastHit2D rayHit,
            bool hovering,
            Lazy<SNet_Player?> player);

        public static event CursorInteractionHandler? CursorUpdate;

        private static readonly Lazy<SNet_Player?> DefaultPlayer = new(() => null);

        [HarmonyPatch(typeof(CM_PageBase))]
        [HarmonyPatch(nameof(CM_PageBase.UpdateButtonPress))]
        [HarmonyPostfix]
        private static void CM_PageBase__UpdateButtonPress__Postfix(CM_PageBase __instance)
        {
            if (!__instance.Is<CM_PageLoadout>() && !__instance.Is<CM_PageMap>()) return;

            var point = __instance.m_cursor.WorldPos;

            if (!__instance.m_guiLayer.GuiLayerBase.m_cellUICanvas.Raycast(point, out var rayHit))
            {
                CursorUpdate?.Invoke(__instance, point, ref rayHit, false, DefaultPlayer);
                return;
            }

            var comp = rayHit.collider.GetComponent<CursorInteraction>();
            if (comp == null)
            {
                CursorUpdate?.Invoke(__instance, point, ref rayHit, false, DefaultPlayer);
                return;
            }

            var player = new Lazy<SNet_Player?>(() => GetPlayerInfo(rayHit.collider.gameObject));
            CursorUpdate?.Invoke(__instance, point, ref rayHit, true, player);

            static SNet_Player? GetPlayerInfo(GameObject go)
            {
                var playerLobbyBar = go.GetComponentInParent<CM_PlayerLobbyBar>();
                if (playerLobbyBar != null) return playerLobbyBar.m_player;

                var playerInventory = go.GetComponentInParent<PUI_Inventory>();
                if (playerInventory != null) return playerInventory.m_owner;

                return null;
            }
        }

        [HarmonyPatch(typeof(CM_PlayerLobbyBar))]
        [HarmonyPatch(nameof(CM_PlayerLobbyBar.UpdatePlayer))]
        [HarmonyPostfix]
        private static void CM_PlayerLobbyBar__UpdatePlayer__Postfix(CM_PlayerLobbyBar __instance)
        {
            var handler = __instance.m_nickText.GetComponent<CursorInteraction>();
            handler.enabled = true;
        }

        [HarmonyPatch(typeof(CM_PlayerLobbyBar))]
        [HarmonyPatch(nameof(CM_PlayerLobbyBar.SetupFromPage))]
        [HarmonyPostfix]
        private static void CM_PlayerLobbyBar__SetupFromPage__Postfix(CM_PlayerLobbyBar __instance) =>
            AddCursorInteraction(__instance.m_nickText.gameObject);

        [HarmonyPatch(typeof(PUI_Inventory))]
        [HarmonyPatch(nameof(PUI_Inventory.Setup))]
        [HarmonyPatch(new[] { typeof(GuiLayer) })]
        [HarmonyPostfix]
        private static void PUI_Inventory__Setup__Postfix(PUI_Inventory __instance)
        {
            var go = __instance.m_headerRoot.transform.Find("Background")?.gameObject;
            var bg = go?.GetComponent<SpriteRenderer>();

            if (go == null || bg == null)
            {
                Instance!.LogError($"Failed to get {nameof(SpriteRenderer)} from {nameof(PUI_Inventory)}/Background");
                return;
            }

            AddCursorInteraction(go, new Vector2(-bg.size.x / 2f, 0), bg.size);
        }

        private static void AddCursorInteraction(GameObject go, Vector2? offset = null, Vector2? size = null)
        {
            var comp = go.GetComponent<CursorInteraction>();
            if (comp == null)
            {
                comp = go.gameObject.AddComponent<CursorInteraction>();
                var collider = comp.gameObject.AddComponent<BoxCollider2D>();
                if (offset != null) collider.offset = (Vector2)offset;
                if (size != null) collider.size = (Vector2)size;
                comp.enabled = false;
            }
        }
    }
}