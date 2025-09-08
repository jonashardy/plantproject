#nullable enable
using Godot;

namespace PlantProject.UI
{
    [GlobalClass]
    public partial class SpecialMeter : Control
    {
        [Export] public float Value { get; set; } = 0f; // 0..1
        [Export] public Color BackColor { get; set; } = new Color(0f, 0f, 0f, 0.6f);
        [Export] public Color StartColor { get; set; } = new Color(1f, 0f, 0f, 0.95f); // red
        [Export] public Color EndColor { get; set; } = new Color(0f, 1f, 0f, 0.95f);   // green
        [Export] public Color BorderColor { get; set; } = new Color(1f, 1f, 1f, 0.9f);
        [Export] public float BorderWidth { get; set; } = 1f;
        [Export] public bool PulseAtFull { get; set; } = true;
        [Export] public float PulseSpeed { get; set; } = 2.0f; // cycles per second
        [Export] public float PulseWidthMin { get; set; } = 1.0f;
        [Export] public float PulseWidthMax { get; set; } = 2.5f;
        [Export] public float PulseAlphaMin { get; set; } = 0.6f;
        [Export] public float PulseAlphaMax { get; set; } = 1.0f;

        [Export] public bool ShowStripesAtFull { get; set; } = false; // disabled per request
        [Export] public Color StripeColor { get; set; } = new Color(1f, 1f, 1f, 0.18f);
        [Export] public float StripeWidth { get; set; } = 18f;
        [Export] public float StripeSpacing { get; set; } = 24f; // gap between stripes
        [Export] public float StripeSpeed { get; set; } = 120f; // px/sec sweep to the right

        [Export] public bool BlinkReadyText { get; set; } = true;
        [Export] public float BlinkSpeed { get; set; } = 2.0f; // cycles per second
        [Export] public int ReadyFontSize { get; set; } = 24;
        [Export] public Color ReadyTextColor { get; set; } = new Color(1f, 1f, 1f, 1f);

        private float _pulseTime;
        private bool _pulseActive;
        private Label? _readyLabel;
        private Label? _readyShadow;

        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always;
            MouseFilter = MouseFilterEnum.Ignore;
            // Shadow label (drawn first, slightly offset)
            _readyShadow = new Label
            {
                Name = "ReadyLabelShadow",
                Text = "SPECIAL READY",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visible = false
            };
            _readyShadow.AddThemeFontSizeOverride("font_size", ReadyFontSize);
            _readyShadow.AddThemeColorOverride("font_color", new Color(0f, 0f, 0f, 0.9f));
            _readyShadow.AnchorLeft = 0f; _readyShadow.AnchorRight = 1f;
            _readyShadow.AnchorTop = 0f; _readyShadow.AnchorBottom = 1f;
            _readyShadow.OffsetLeft = 0f; _readyShadow.OffsetRight = 0f;
            _readyShadow.OffsetTop = 0f; _readyShadow.OffsetBottom = 0f;
            AddChild(_readyShadow);
            _readyShadow.Position = new Vector2(2f, 2f);

            // Main internal label for READY text
            _readyLabel = new Label
            {
                Name = "ReadyLabel",
                Text = "SPECIAL READY",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visible = false
            };
            _readyLabel.AddThemeFontSizeOverride("font_size", ReadyFontSize);
            _readyLabel.AddThemeColorOverride("font_color", ReadyTextColor);
            _readyLabel.AnchorLeft = 0f; _readyLabel.AnchorRight = 1f;
            _readyLabel.AnchorTop = 0f; _readyLabel.AnchorBottom = 1f;
            _readyLabel.OffsetLeft = 0f; _readyLabel.OffsetRight = 0f;
            _readyLabel.OffsetTop = 0f; _readyLabel.OffsetBottom = 0f;
            AddChild(_readyLabel);
            QueueRedraw();
        }

        public void SetValue(float t)
        {
            float clamped = Mathf.Clamp(t, 0f, 1f);
            if (Mathf.IsEqualApprox(clamped, Value)) return;
            Value = clamped;
            QueueRedraw();
        }

        public override void _Process(double delta)
        {
            bool shouldPulse = PulseAtFull && Value >= 0.999f;
            if (shouldPulse)
            {
                _pulseActive = true;
                _pulseTime += (float)delta;
                QueueRedraw();
                if (_readyLabel != null)
                {
                    if (BlinkReadyText)
                    {
                        float phase = 0.5f + 0.5f * Mathf.Sin(_pulseTime * Mathf.Tau * Mathf.Max(0.01f, BlinkSpeed));
                        bool on = phase >= 0.5f;
                        _readyLabel.Visible = on; // blink
                        if (_readyShadow != null) _readyShadow.Visible = on;
                    }
                    else
                    {
                        _readyLabel.Visible = true;
                        if (_readyShadow != null) _readyShadow.Visible = true;
                    }
                }
            }
            else if (_pulseActive)
            {
                _pulseActive = false;
                _pulseTime = 0f;
                QueueRedraw();
                if (_readyLabel != null) _readyLabel.Visible = false;
                if (_readyShadow != null) _readyShadow.Visible = false;
            }
        }

        public override void _Draw()
        {
            var size = GetRect().Size;
            if (size.X <= 1f || size.Y <= 1f) return;

            // Background
            DrawRect(new Rect2(Vector2.Zero, size), BackColor);

            // Fill based on Value with color lerp red->green
            float t = Mathf.Clamp(Value, 0f, 1f);
            float w = Mathf.Max(0f, size.X * t);
            var fillColor = StartColor.Lerp(EndColor, t);
            DrawRect(new Rect2(Vector2.Zero, new Vector2(w, size.Y)), fillColor);

            // Diagonal stripes when full
            if (_pulseActive && ShowStripesAtFull)
            {
                DrawSweepingStripes(size);
            }

            // Border
            float drawBorderWidth = BorderWidth;
            Color drawBorderColor = BorderColor;
            if (_pulseActive)
            {
                float phase = 0.5f + 0.5f * Mathf.Sin(_pulseTime * Mathf.Tau * Mathf.Max(0.01f, PulseSpeed));
                drawBorderWidth = Mathf.Lerp(PulseWidthMin, PulseWidthMax, phase);
                float a = Mathf.Lerp(PulseAlphaMin, PulseAlphaMax, phase);
                drawBorderColor = new Color(BorderColor.R, BorderColor.G, BorderColor.B, a);
            }

            if (drawBorderWidth > 0f)
            {
                var a = new Vector2(0, 0);
                var b = new Vector2(size.X, 0);
                var c = new Vector2(size.X, size.Y);
                var d = new Vector2(0, size.Y);
                DrawLine(a, b, drawBorderColor, drawBorderWidth);
                DrawLine(b, c, drawBorderColor, drawBorderWidth);
                DrawLine(c, d, drawBorderColor, drawBorderWidth);
                DrawLine(d, a, drawBorderColor, drawBorderWidth);
            }
        }

        private void DrawSweepingStripes(Vector2 size)
        {
            // Confine stripes inside the bar interior (exclude border area)
            float pad = Mathf.Max(1f, BorderWidth);
            Vector2 origin = new Vector2(pad, pad);
            float width = Mathf.Max(0f, size.X - pad * 2f);
            float height = Mathf.Max(0f, size.Y - pad * 2f);
            if (width <= 0f || height <= 0f) return;

            // 45° slope, but lean the other way (backslash) and sweep left
            float tilt = height;

            float stripeW = Mathf.Max(2f, StripeWidth);
            float spacing = Mathf.Max(2f, StripeSpacing);
            float period = stripeW + spacing;
            float shift = -((_pulseTime * StripeSpeed) % period); // reverse sweep direction

            // Start slightly beyond bounds to cover corners; we'll mask outside afterward
            float startX = -tilt - stripeW;
            float endX = width + tilt + stripeW;

            for (float x = startX + shift; x <= endX; x += period)
            {
                // Parallelogram points leaning left
                var p0 = origin + new Vector2(x + tilt, 0);
                var p1 = origin + new Vector2(x + tilt + stripeW, 0);
                var p2 = origin + new Vector2(x - tilt + stripeW, height);
                var p3 = origin + new Vector2(x - tilt, height);
                DrawPolygon(new Vector2[] { p0, p1, p2, p3 }, new Color[] { StripeColor, StripeColor, StripeColor, StripeColor });
            }

            // Mask any overflow outside the interior to ensure confinement
            if (origin.X > 0)
            {
                DrawRect(new Rect2(new Vector2(0, 0), new Vector2(origin.X, size.Y)), BackColor);
            }
            float rightX = origin.X + width;
            if (rightX < size.X)
            {
                DrawRect(new Rect2(new Vector2(rightX, 0), new Vector2(size.X - rightX, size.Y)), BackColor);
            }
        }
    }
}
