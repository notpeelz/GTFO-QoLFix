using System;
using SNetwork;
using TMPro;
using UnityEngine;

namespace QoLFix.Patches.Tweaks
{
    public partial class LatencyHUDPatch
    {
        public class LatencyWatermark : MonoBehaviour
        {
            public LatencyWatermark(IntPtr value)
                : base(value) { }

            private float time;
            private PUI_Watermark watermark;
            private TextMeshPro text;

            internal void Awake()
            {
                this.text = this.GetComponent<TextMeshPro>();
            }

            internal void Update()
            {
                this.time -= Time.deltaTime;
                if (this.time > 0) return;
                this.time = PING_UPDATE_INTERVAL;

                if (this.watermark == null)
                {
                    this.watermark = this.GetComponentInParent<PUI_Watermark>();
                }
                if (this.watermark == null) return;

                // Hide when we're not connected
                if (!SNet.HasMaster)
                {
                    this.text.enabled = false;
                    return;
                }

                var statusShown = this.watermark.m_statusText?.isActiveAndEnabled == true;
                var fpsShown = this.watermark.m_fpsText?.isActiveAndEnabled == true;

                // Hide when StatusText is shown
                if (statusShown)
                {
                    this.text.enabled = false;
                    return;
                }
                else
                {
                    this.text.enabled = true;
                }

                this.text.transform.position = fpsShown
                    ? this.watermark.m_statusText.transform.position
                    : this.watermark.m_fpsText.transform.position;
            }
        }
    }
}
