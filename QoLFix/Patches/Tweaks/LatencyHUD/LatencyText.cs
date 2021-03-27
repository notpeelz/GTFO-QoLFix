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

            private float time;
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
                this.time -= Time.deltaTime;
                if (this.time > 0) return;
                this.time = this.UpdateInterval;

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
