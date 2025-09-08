#nullable enable
using System;
using System.Collections.Generic;
using Godot;

namespace PlantProject.Items
{
    [GlobalClass]
    public partial class AttackArc2D : Node2D
    {
        [Export] public Color FillColor { get; set; } = new Color(1f, 1f, 1f, 0.18f);
        [Export] public int Segments { get; set; } = 36;
        [Export] public int ZIndexArc { get; set; } = 350;

        private float _radius;
        private float _halfAngleRad;

        public override void _Ready()
        {
            ZAsRelative = false;
            ZIndex = ZIndexArc;
            Visible = false;
            QueueRedraw();
        }

        public void Configure(float radius, float halfAngleRadians, Color? colorOverride = null, int? segmentsOverride = null, int? zIndexOverride = null)
        {
            _radius = Math.Max(0f, radius);
            _halfAngleRad = Math.Max(0f, halfAngleRadians);
            if (colorOverride.HasValue) FillColor = colorOverride.Value;
            if (segmentsOverride.HasValue) Segments = Math.Max(6, segmentsOverride.Value);
            if (zIndexOverride.HasValue)
            {
                ZIndexArc = zIndexOverride.Value;
                ZIndex = ZIndexArc;
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_radius <= 0f || _halfAngleRad <= 0f)
                return;

            int segs = Math.Max(6, Segments);
            float start = -(_halfAngleRad);
            float end = _halfAngleRad;

            var points = new List<Vector2>(segs + 2) { Vector2.Zero };
            for (int i = 0; i <= segs; i++)
            {
                float t = (float)i / segs;
                float ang = Mathf.Lerp(start, end, t);
                float x = MathF.Cos(ang) * _radius;
                float y = MathF.Sin(ang) * _radius;
                points.Add(new Vector2(x, y));
            }

            var colors = new Color[points.Count];
            for (int i = 0; i < colors.Length; i++) colors[i] = FillColor;
            DrawPolygon(points.ToArray(), colors);
        }
    }
}

