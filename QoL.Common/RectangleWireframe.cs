using System;
using MTFO.Core;
using UnhollowerBaseLib.Attributes;
using UnityEngine;

namespace QoL.Common
{
    public class RectangleWireframe : MonoBehaviour
    {
        public RectangleWireframe(IntPtr pointer)
            : base(pointer) { }

        private const int LINE_COUNT = 16;
        private readonly Material material = new(Shader.Find("Unlit/Texture"));
        private LineRenderer[] lines = new LineRenderer[LINE_COUNT];

        [HideFromIl2Cpp]
        public Vector3 DefaultPosition { get; set; }

        [HideFromIl2Cpp]
        public Vector3 Center { get; set; }

        [HideFromIl2Cpp]
        public Vector3 Size { get; set; }

        internal void Awake()
        {
            for (var i = 0; i < LINE_COUNT; i++)
            {
                GOFactory.CreateObject($"line{i}", this.gameObject.transform, out LineRenderer r);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.useWorldSpace = false;
                r.positionCount = 2;
                r.startWidth = 0.005f;
                r.endWidth = 0.005f;
                r.material = this.material;
                r.enabled = true;
                this.lines[i] = r;
            }
        }

        internal void OnDisable()
        {
            if (this.lines == null) return;
            foreach (var line in this.lines)
            {
                line.enabled = false;
            }
        }

        internal void OnEnable()
        {
            if (this.lines == null) return;
            foreach (var line in this.lines)
            {
                line.enabled = true;
            }
        }

        internal void Update()
        {
            var width = this.Size.x;
            var length = this.Size.z;
            var height = this.Size.y;

            width *= 0.5f;
            length *= 0.5f;
            height *= 0.5f;

            var center = Vector3.zero;
            var up = Vector3.up;
            var forward = Vector3.forward;
            var left = Vector3.Cross(up, forward);

            var backBottomRight = center - (up * height) - (forward * length) - (left * width);
            var frontBottomRight = center - (up * height) + (forward * length) - (left * width);
            var backBottomLeft = center - (up * height) - (forward * length) + (left * width);
            var frontBottomLeft = center - (up * height) + (forward * length) + (left * width);
            var backTopRight = center + (up * height) - (forward * length) - (left * width);
            var frontTopRight = center + (up * height) + (forward * length) - (left * width);
            var backTopLeft = center + (up * height) - (forward * length) + (left * width);
            var frontTopLeft = center + (up * height) + (forward * length) + (left * width);

            var i = 0;

            // bottom lines
            UpdateLine(frontBottomLeft, frontBottomRight);
            UpdateLine(backBottomLeft, backBottomRight);
            UpdateLine(frontBottomRight, backBottomRight);
            UpdateLine(frontBottomRight, backBottomLeft); // diag
            UpdateLine(frontBottomLeft, backBottomLeft);
            UpdateLine(frontBottomLeft, backBottomRight); // diag

            // top lines
            UpdateLine(frontTopLeft, frontTopRight);
            UpdateLine(backTopLeft, backTopRight);
            UpdateLine(frontTopRight, backTopRight);
            UpdateLine(frontTopRight, backTopLeft); // diag
            UpdateLine(frontTopLeft, backTopLeft);
            UpdateLine(frontTopLeft, backTopRight); // diag

            // vertical lines
            UpdateLine(backBottomRight, backTopRight);
            UpdateLine(frontBottomRight, frontTopRight);
            UpdateLine(backBottomLeft, backTopLeft);
            UpdateLine(frontBottomLeft, frontTopLeft);

            [HideFromIl2Cpp]
            void UpdateLine(Vector3 start, Vector3 end)
            {
                var line = this.lines[i];
                i++;
                line.SetPosition(0, this.Center + start);
                line.SetPosition(1, this.Center + end);
            }
        }

        internal void OnDestroy()
        {
            if (this.lines == null) return;
            for (var i = 0; i < this.lines.Length; i++)
            {
                Destroy(this.lines[i]);
                Destroy(this.lines[i].gameObject);
            }
            this.lines = default!;
        }
    }
}
