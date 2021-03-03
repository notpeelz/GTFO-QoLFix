using System;
using System.Linq;
using BepInEx.Configuration;
using CellMenu;
using HarmonyLib;
using QoLFix.Patches.Common;
using QoLFix.Patches.Common.Cursor;
using SNetwork;
using TMPro;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;

namespace QoLFix.Patches.Tweaks
{
    /// <summary>
    /// <para>
    /// This patch displays a network latency information on your HUD.
    /// It displays your latency to the host on the bottom right corner.
    /// Additionaly, the latency to every player is displayed on the map menu.
    /// </para>
    /// <para>
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
    /// </para>
    /// </summary>
    public partial class LatencyHUDPatch : IPatch
    {
        private const string PatchName = nameof(LatencyHUDPatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        private const float PING_UPDATE_INTERVAL = 0.25f;

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
            ClassInjector.RegisterTypeInIl2Cpp<LatencyText>();
            this.PatchMethod<WatermarkGuiLayer>(nameof(WatermarkGuiLayer.Setup), new[] { typeof(Transform), typeof(string) }, PatchType.Postfix);
            //this.PatchMethod<SNet_Core_STEAM>(nameof(SNet_Core_STEAM.UpdateConnectionStatus), PatchType.Postfix);

            QoLFixPlugin.RegisterPatch<PlayerNameExtPatch>();
            PlayerNameExtPatch.CursorUpdate += this.OnCursorUpdate;
        }

        private RectTransform popupTransform;
        private GameObject popupContent;
        private TextMeshPro pingText;
        private LatencyText latencyText;
        private float pingTextWidth;
        private float pingTextHeight;
        private bool hovering;

        private void OnCursorUpdate(CM_PageBase page, Vector2 cursorPos, ref RaycastHit2D rayHit, bool hovering, bool clicked, Lazy<SNet_Player> player)
        {
            if (this.pingText == null)
            {
                this.InitializePopup(page);
            }

            var cursorState = page.GetCursorState();
            if (hovering) UpdatePosition();

            if (this.hovering == hovering) return;
            this.hovering = hovering;

            if (!hovering)
            {
                page.SetCursorTooltip(null);
                return;
            }

            this.latencyText.Player = player.Value;
            this.latencyText.UpdateText();
            this.pingTextWidth = this.pingText.GetRenderedWidth(false);
            this.pingTextHeight = this.pingText.GetRenderedHeight(false);

            page.SetCursorTooltip(this.popupContent, new Vector2(62f, 16f));

            var extentX = cursorState.TooltipSprite.bounds.size.x / 2f;
            this.popupTransform.anchorMin = new Vector2(0.5f - extentX, 0.5f);
            this.popupTransform.anchorMax = new Vector2(0.5f + extentX, 0.5f);

            UpdatePosition();

            void UpdatePosition()
            {
                cursorState.Tooltip.transform.position = PopupCursorOffset + new Vector3(
                    cursorPos.x,
                    cursorPos.y + (this.pingTextHeight / 2f),
                    cursorState.TooltipSprite.transform.position.z);
            }
        }

        private void InitializePopup(CM_PageBase page)
        {
            var cursorState = page.GetCursorState();

            this.popupContent = GOFactory.CreateObject("Content", null,
                out this.popupTransform,
                out VerticalLayoutGroup group);

            this.popupTransform.offsetMin = Vector2.zero;
            this.popupTransform.offsetMax = Vector2.zero;
            this.popupTransform.pivot = new Vector2(0.5f, 0.5f);
            this.popupTransform.localPosition = Vector2.zero;

            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandHeight = true;
            group.childForceExpandWidth = true;

            var textGO = GOFactory.CreateObject("Text", this.popupContent.transform,
                out RectTransform textTransform,
                out ContentSizeFitter fitter,
                out this.pingText,
                out this.latencyText,
                out Cell_TMProDisabler _);

            // Disable updating the tooltip text since resizing the tooltip
            // is already quite tricky.
            this.latencyText.enabled = false;

            textGO.layer = LayerManager.LAYER_UI;

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var techFont = page.GetComponentsInChildren<TextMeshPro>()
                .FirstOrDefault(x => x.font?.name.Contains("ShareTechMono") == true)
                ?.font;

            if (techFont == null)
            {
                this.LogError("Failed to find font 'ShareTechMono'");
            }

            this.pingText.margin = Vector4.zero;
            this.pingText.color = Color.white;
            this.pingText.alpha = 1f;
            this.pingText.fontSize = 18;
            this.pingText.font = techFont;
            this.pingText.alignment = TextAlignmentOptions.CenterGeoAligned;
            this.pingText.enableWordWrapping = false;
            this.pingText.isOrthographic = true;
            this.pingText.autoSizeTextContainer = true;
            this.pingText.sortingOrder = cursorState.TooltipSprite.sortingOrder + 1;
            this.pingText.UpdateMaterial();

            textTransform.offsetMin = Vector2.zero;
            textTransform.offsetMax = Vector2.zero;
            textTransform.pivot = new Vector2(0.5f, 0.5f);
            textTransform.localPosition = Vector2.zero;
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
                    out LatencyText _,
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

        private static int GetPlayerPing(SNet_Player player = null)
        {
            // This seems to return the same thing as SNet_Player.Ping
            //SNet.MasterManagement.GetPing(player, ref ping, ref quality);

            //var pingLocation = SNet.Master.Load<pPingLocation>().pingLocation;
            //latency = SteamNetworkingUtils.EstimatePingTimeFromLocalHost(ref pingLocation);

            int? latency = null;
            if (player?.IsLocal == false)
            {
                if (player.IsMaster) return -1;
                latency = player?.Ping;
            }
            else if (SNet.Master != null)
            {
                if (SNet.Master.IsLocal) return -1;
                latency = SNet.Master?.Ping;
            }

            if (latency < 0) return -2;
            return latency ?? -3;
        }
    }
}
