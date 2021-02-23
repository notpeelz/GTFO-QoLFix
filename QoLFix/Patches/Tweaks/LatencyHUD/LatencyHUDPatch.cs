using System;
using System.Linq;
using BepInEx.Configuration;
using CellMenu;
using HarmonyLib;
using QoLFix.Patches.Common;
using SNetwork;
using TMPro;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace QoLFix.Patches.Tweaks
{
    /// <summary>
    /// This patch displays a network latency information on your HUD.
    /// It displays your latency to the host on the bottom right corner.
    /// Additionaly, the latency to every player is displayed on the map menu.
    ///
    /// Known bugs:
    /// The latency doesn't update correctly throughout the game for some
    /// unknown reason. This might be because the pPingLocation struct doesn't
    /// get replicated after joining the game.
    /// Another thing that could be responsible for this bug is that
    /// pPingLocation is a network replication wrapper for the
    /// "SteamNetworkPingLocation_t" SteamAPI struct.
    /// According to the Steam API documentation, this struct is not supposed
    /// to be serialized or sent over network:
    ///     NOTE: This object should only be used in the same process!
    ///     Do not serialize it, send it over the wire, or persist it in a file
    ///     or database!
    ///     If you need to do that, convert it to a string representation using
    ///     the methods in ISteamNetworkingUtils
    /// See: https://partner.steamgames.com/doc/api/steamnetworkingtypes#SteamNetworkPingLocation_t
    /// </summary>
    public partial class LatencyHUDPatch : IPatch
    {
        private static readonly string PatchName = nameof(LatencyHUDPatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        private static readonly Vector3 PopupCursorOffset = new Vector3(0, 5f, 0);

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Displays network latency on your HUD.\nNOTE: unfortunately, due to a bug with the way GTFO estimates network latency, the ping is only updated once upon joining a game."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            ClassInjector.RegisterTypeInIl2Cpp<LatencyWatermark>();
            this.PatchMethod<WatermarkGuiLayer>(nameof(WatermarkGuiLayer.Setup), new[] { typeof(Transform), typeof(string) }, PatchType.Postfix);
            //this.PatchMethod<SNet_Core_STEAM>(nameof(SNet_Core_STEAM.UpdateConnectionStatus), PatchType.Postfix);

            QoLFixPlugin.RegisterPatch<PlayerNameExtPatch>();
            PlayerNameExtPatch.CursorUpdate += this.OnCursorUpdate;
        }

        private GameObject popupGO;
        private TextMeshPro pingText;
        private SpriteRenderer bgSprite;

        private void InitializePopup(CM_PageBase page)
        {
            this.popupGO = GOFactory.CreateObject("PlayerInfoPopup", null, out RectTransform t);

            this.popupGO.layer = LayerManager.LAYER_UI;
            this.popupGO.SetActive(false);

            t.anchorMin = new Vector2(0.5f, 0.5f);
            t.anchorMax = new Vector2(0.5f, 0.5f);
            t.pivot = new Vector2(0.5f, 0.5f);
            t.offsetMin = Vector2.zero;
            t.offsetMax = Vector2.zero;
            t.localPosition = Vector2.zero;

            var bgGO = GOFactory.CreateObject("Background", this.popupGO.transform,
                out RectTransform bgTransform,
                out this.bgSprite);
            bgGO.layer = LayerManager.LAYER_UI;

            this.bgSprite.sortingOrder = 50;
            this.bgSprite.color = new Color(0.4f, 0.4f, 0.4f, 1);

            bgTransform.pivot = new Vector2(0.5f, 0.5f);
            bgTransform.localScale = new Vector3(31f, 8f, 1f);
            bgTransform.localPosition = Vector2.zero;

            var tex = Resources.Load<Texture2D>("gui/gear/frames/cellUI_Frame_BoxFiled");
            this.bgSprite.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), t.pivot, 100f);

            var textGO = GOFactory.CreateObject("Text", this.popupGO.transform,
                out RectTransform textTransform,
                out this.pingText,
                out Cell_TMProDisabler _,
                out LatencyWatermark _);
            textGO.layer = LayerManager.LAYER_UI;

            var techFont = page.GetComponentsInChildren<TextMeshPro>()
                .FirstOrDefault(x => x.font?.name.Contains("ShareTechMono") == true)
                ?.font;

            if (techFont == null)
            {
                this.LogError("Failed to find font 'ShareTechMono'");
            }

            this.pingText.color = Color.white;
            this.pingText.alpha = 1f;
            this.pingText.fontSize = 18;
            this.pingText.font = techFont;
            this.pingText.alignment = TextAlignmentOptions.CenterGeoAligned;
            this.pingText.enableWordWrapping = false;
            this.pingText.isOrthographic = true;
            this.pingText.autoSizeTextContainer = true;
            this.pingText.sortingOrder = this.bgSprite.sortingOrder + 1;
            this.pingText.UpdateMaterial();

            textTransform.offsetMin = Vector2.zero;
            textTransform.offsetMax = Vector2.zero;
            textTransform.pivot = new Vector2(0.5f, 0.5f);
            textTransform.localPosition = Vector2.zero;

            var extentX = this.bgSprite.bounds.size.x / 2f;
            textTransform.anchorMin = new Vector2(0.5f - extentX, 0.5f);
            textTransform.anchorMax = new Vector2(0.5f + extentX, 0.5f);
        }

        private float pingTextWidth;
        private float pingTextHeight;
        private bool lastState;

        private void OnCursorUpdate(CM_PageBase page, Vector2 cursorPos, ref RaycastHit2D rayHit, bool hovering, bool clicked, Lazy<SNet_Player> player)
        {
            if (this.popupGO == null)
            {
                this.InitializePopup(page);
            }

            if (hovering) UpdatePosition();

            if (this.lastState == hovering) return;
            this.lastState = hovering;

            if (!hovering)
            {
                this.popupGO.SetActive(false);
                return;
            }

            this.popupGO.transform.SetParent(page.transform, false);
            this.popupGO.SetActive(true);

            this.pingText.SetText(GetPlayerPing(player.Value));
            this.pingText.ForceMeshUpdate(true);
            this.pingTextWidth = this.pingText.GetRenderedWidth(false);
            this.pingTextHeight = this.pingText.GetRenderedHeight(false);

            UpdatePosition();

            void UpdatePosition()
            {
                this.popupGO.transform.position = PopupCursorOffset + new Vector3(cursorPos.x, cursorPos.y + this.pingTextHeight / 2f, this.popupGO.transform.position.z);
            }
        }

        private static void SNet_Core_STEAM__UpdateConnectionStatus__Postfix(SNet_Player player)
        {
            Instance.LogDebug("UpdateConnectionStatus: " + player?.NickName + " " + player?.Ping);
        }

        private static void WatermarkGuiLayer__Setup__Postfix(WatermarkGuiLayer __instance)
        {
            var watermark = __instance.m_watermark;

            var go = GOFactory.CreateObject("Latency", watermark.transform,
                    out RectTransform t,
                    out TextMeshPro text,
                    out Cell_TMProDisabler _,
                    out LatencyWatermark _);
            go.layer = LayerManager.LAYER_UI;

            var fpsText = watermark.m_fpsText;
            var statusTransform = watermark.m_statusText.GetComponent<RectTransform>();

            t.position = statusTransform.position;
            t.localPosition = statusTransform.localPosition;
            t.localScale = statusTransform.localScale;
            t.anchorMin = statusTransform.anchorMin;
            t.anchorMax = statusTransform.anchorMax;
            t.pivot = statusTransform.pivot;
            t.offsetMax = statusTransform.offsetMax;
            t.offsetMin = statusTransform.offsetMin;
            t.anchoredPosition = statusTransform.anchoredPosition;

            text.isOrthographic = fpsText.isOrthographic;
            text.font = fpsText.font;
            text.color = fpsText.color;
            text.alpha = fpsText.alpha;
            text.fontSize = fpsText.fontSize;
            text.fontSizeMin = fpsText.fontSizeMin;
            text.fontSizeMax = fpsText.fontSizeMax;
            text.fontWeight = fpsText.fontWeight;
            text.fontStyle = fpsText.fontStyle;
            text.enableKerning = fpsText.enableKerning;
            text.enableWordWrapping = false;
            text.alignment = fpsText.alignment;
            text.autoSizeTextContainer = fpsText.autoSizeTextContainer;
            text.UpdateMaterial();
            text.UpdateFontAsset();
        }

        private static string GetPlayerPing(SNet_Player player)
        {
            // This seems to return the same thing as SNet_Player.Ping
            //SNet.MasterManagement.GetPing(player, ref ping, ref quality);

            //var pingLocation = SNet.Master.Load<pPingLocation>().pingLocation;
            //latency = SteamNetworkingUtils.EstimatePingTimeFromLocalHost(ref pingLocation);

            int? latency = null;
            if (player != null && !player.IsLocal)
            {
                if (player.IsMaster) return "HOST";
                latency = player?.Ping;
            }
            else if (SNet.Master != null)
            {
                if (SNet.Master.IsLocal) return "HOST";
                latency = SNet.Master?.Ping;
            }

            return $"{latency?.ToString() ?? "???"} ms";
        }
    }
}
