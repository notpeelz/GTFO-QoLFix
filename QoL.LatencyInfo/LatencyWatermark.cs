using System;
using SNetwork;
using TMPro;
using UnityEngine;

namespace QoL.LatencyInfo
{
    partial class LatencyInfoPatch
    {
        public class LatencyWatermark : MonoBehaviour
        {
            public LatencyWatermark(IntPtr pointer)
                : base(pointer) { }

            private float time;
            private PUI_Watermark? watermark;
            private TextMeshPro? text;

            internal void Awake()
            {
                this.text = this.GetComponent<TextMeshPro>();
            }

            internal void Update()
            {
                if (this.text == null) return;

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

                var statusShown = this.watermark.m_statusText.isActiveAndEnabled;
                var fpsShown = this.watermark.m_fpsText.isActiveAndEnabled;

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
