using System;
using TMPro;
using UnityEngine;

namespace QoLFix.Patches.Tweaks
{
    public partial class SteamProfileLinkPatch
    {
        public class SteamProfileClickHandler : MonoBehaviour
        {
            public SteamProfileClickHandler(IntPtr value)
                : base(value) { }

            private void Update()
            {
                var collider = this.GetComponent<BoxCollider2D>();

                var nickText = this.GetComponent<TextMeshPro>();
                if (nickText == null)
                {
                    Instance.LogError($"{nameof(SteamProfileClickHandler)} isn't attached to a {nameof(TextMeshPro)}");
                    this.enabled = false;
                    return;
                }

                var width = nickText.GetRenderedWidth(true);
                var height = nickText.GetRenderedHeight(true);
                collider.size = new Vector2(width, height);
                collider.offset = new Vector2(width / 2f, 0);

                this.enabled = false;
            }
        }
    }
}
