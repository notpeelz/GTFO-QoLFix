using System;
using Player;
using SNetwork;
using TMPro;
using UnityEngine;

namespace QoLFix.Patches.Tweaks
{
    public partial class LatencyHUDPatch
    {
        public class LatencyHUDElement : MonoBehaviour
        {
            public LatencyHUDElement(IntPtr value)
                : base(value) { }

            private float lastUpdated;
            private PUI_Watermark watermark;
            private TextMeshPro text;

            public PlayerAgent Player { get; set; }

            private void Awake()
            {
                this.text = this.GetComponent<TextMeshPro>();
            }

            private void Update()
            {
                if ((Time.time - this.lastUpdated) < 0.25f) return;
                this.lastUpdated = Time.time;

                if (this.watermark == null)
                {
                    this.watermark = this.GetComponentInParent<PUI_Watermark>();
                }
                if (this.watermark == null) return;

                // We're taking StatusText's spot, so we have to hide when
                // it shows up!
                if (!SNet.HasMaster || this.watermark.m_statusText?.isActiveAndEnabled == true)
                {
                    this.text.enabled = false;
                    return;
                }
                else
                {
                    this.text.enabled = true;
                }

                // This seems to return the same thing as SNet_Player.Ping
                //SNet.MasterManagement.GetPing(player, ref ping, ref quality);

                int? latency = null;
                if (this.Player != null)
                {
                    latency = this.Player?.Owner?.Ping;
                }
                else if (SNet.Master != null)
                {
                    latency = 0;
                    if (!SNet.Master.IsLocal)
                    {
                        latency = SNet.Master.Ping;
                        //var pingLocation = SNet.Master.Load<pPingLocation>().pingLocation;
                        //latency = SteamNetworkingUtils.EstimatePingTimeFromLocalHost(ref pingLocation);
                    }
                }

                text.SetText($"{latency?.ToString() ?? "???"} ms");
                text.ForceMeshUpdate();
            }

            public static void Patch(PUI_Watermark watermark)
            {
                var go = GOFactory.CreateObject("Latency", watermark.transform,
                    out RectTransform t,
                    out TextMeshPro text,
                    out CanvasRenderer _,
                    out Cell_TMProDisabler _,
                    out LatencyHUDElement _);
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
                text.material = fpsText.material;
                text.font = fpsText.font;
                text.fontMaterial = fpsText.fontMaterial;
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

                text.SetText("??? ms");
                text.ForceMeshUpdate(true);
            }
        }
    }
}
