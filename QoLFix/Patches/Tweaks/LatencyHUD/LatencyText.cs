using System;
using SNetwork;
using TMPro;
using UnhollowerBaseLib.Attributes;
using UnityEngine;

namespace QoLFix.Patches.Tweaks
{
    public partial class LatencyHUDPatch
    {
        public class LatencyText : MonoBehaviour
        {
            public LatencyText(IntPtr value)
                : base(value) { }

            private float lastUpdated;
            private TextMeshPro text;

            public SNet_Player Player { get; set; }

            [HideFromIl2Cpp]
            public float UpdateInterval { get; set; } = PING_UPDATE_INTERVAL;

            internal void Awake()
            {
                this.text = this.GetComponent<TextMeshPro>();
            }

            internal void Update()
            {
                if ((Time.time - this.lastUpdated) < this.UpdateInterval) return;
                this.lastUpdated = Time.time;

                this.UpdateText();
            }

            [HideFromIl2Cpp]
            public void UpdateText()
            {
                this.text.SetText(GetPlayerPing(this.Player) switch
                {
                    var i when i is -1 => "HOST",
                    var i when i is -2 or -3 => "?",
                    var i => $"{i} ms",
                });
                this.text.ForceMeshUpdate(true);
            }
        }
    }
}
