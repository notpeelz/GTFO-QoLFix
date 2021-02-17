﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace QoLFix.Debugging
{
    public class RectangleWireframe : MonoBehaviour
    {
        // This workaround is necessary because Unhollower doesn't expose fields/properties to IL2CPP
        private static readonly Dictionary<int, Vector3> DefaultPositions = new();

        public RectangleWireframe(IntPtr value)
            : base(value) { }

        private const int LINE_COUNT = 16;
        private Material material;
        private LineRenderer[] lines;

        public Vector3 DefaultPosition
        {
            get => DefaultPositions[this.GetInstanceID()];
            set => DefaultPositions[this.GetInstanceID()] = value;
        }

        public Vector3 Center { get; set; }

        public Vector3 Size { get; set; }

        private void Awake()
        {
            this.material = new Material(Shader.Find("Unlit/Texture"));
            this.lines = new LineRenderer[LINE_COUNT];
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

        private void OnDisable()
        {
            if (this.lines == null) return;
            foreach (var line in this.lines)
            {
                line.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (this.lines == null) return;
            foreach (var line in this.lines)
            {
                line.enabled = true;
            }
        }

        private void Update()
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

            var backBottomRight = center - up * height - forward * length - left * width;
            var frontBottomRight = center - up * height + forward * length - left * width;
            var backBottomLeft = center - up * height - forward * length + left * width;
            var frontBottomLeft = center - up * height + forward * length + left * width;
            var backTopRight = center + up * height - forward * length - left * width;
            var frontTopRight = center + up * height + forward * length - left * width;
            var backTopLeft = center + up * height - forward * length + left * width;
            var frontTopLeft = center + up * height + forward * length + left * width;

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

            void UpdateLine(Vector3 start, Vector3 end)
            {
                var line = this.lines[i];
                i++;
                line.SetPosition(0, this.Center + start);
                line.SetPosition(1, this.Center + end);
            }
        }

        private void OnDestroy()
        {
            if (this.lines == null) return;
            for (var i = 0; i < lines.Length; i++)
            {
                Destroy(this.lines[i]);
                Destroy(this.lines[i].gameObject);
                this.lines[i] = null;
            }
            this.lines = null;
        }
    }
}
