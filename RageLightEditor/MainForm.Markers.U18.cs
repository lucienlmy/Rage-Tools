using System;
using CodeWalker.GameFiles;
using RageLightEditor.Editor;
using SharpDX;

namespace RageLightEditor
{
    public partial class MainForm
    {
        public const float MarkerFadeStart_U34 = 15.0f;
        public const float MarkerFadeEnd_U34 = 35.0f;
        public const float MarkerPullMax_U34 = 1.0f;
        public const float MarkerSizePx_U35 = 10.0f;
        private const float MarkerLinePx_U35 = 1.6f;
        private const float MarkerRimPx_U35 = 3.6f;
        private static readonly Vector4 MarkerWire_U35 = new Vector4(1.00f, 0.85f, 0.35f, 1f);
        private static readonly Vector4 MarkerWireSel_U35 = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 MarkerRim_U35 = new Vector4(0.02f, 0.02f, 0.03f, 0.55f);

        public static float MarkerFade_U34(float dist) =>
            dist <= MarkerFadeStart_U34 ? 1.0f
            : dist >= MarkerFadeEnd_U34 ? 0.0f
            : 1.0f - (dist - MarkerFadeStart_U34) / (MarkerFadeEnd_U34 - MarkerFadeStart_U34);

        private bool markerDumpDone_U34;

        private void DrawLightMarkers()
        {
            var cam = camera.Position;
            if (!markerDumpDone_U34 && scene.Lights.Count > 0 && Environment.GetEnvironmentVariable("RLE_MARKERDUMP") == "1")
            {
                markerDumpDone_U34 = true;
                Console.WriteLine($"MARKERDUMP camera {cam} lights {scene.Lights.Count}");
                for (int i = 0; i < scene.Lights.Count; i++)
                    Console.WriteLine($"MARKERDUMP {i} {scene.Lights[i].Type} at {scene.GetInstance(scene.Lights[i]).WorldPosition} dist {Vector3.Distance(cam, scene.GetInstance(scene.Lights[i]).WorldPosition):0.0}");
            }
            BrushProbe_U37();
            PickProbe_U39();
            for (int i = 0; i < scene.Lights.Count; i++)
            {
                var l = scene.Lights[i];
                var ownerFile = scene.OwnerFile(l);
                if (ownerFile != null && !ownerFile.Visible) continue;
                bool sel = scene.IsSelected(i);
                if (!sel && !scene.TypeShown_U35(l.Type)) continue;
                var inst = scene.GetInstance(l);
                var p = inst.WorldPosition;
                var dir = inst.WorldDirection.LengthSquared() > 1e-6f ? Vector3.Normalize(inst.WorldDirection) : -Vector3.UnitZ;
                if (sel)
                {
                    MaxLightIcon_U35(p, dir, l, MarkerWireSel_U35, 1f, false);
                    continue;
                }
                var toCam = cam - p;
                float dist = toCam.Length();
                float fade = settings.MarkerFadeU36 ? MarkerFade_U34(dist) : 1.0f;
                if (fade <= 0.02f) continue;
                bool occlude = settings.MarkerOccludeU36;
                if (occlude && dist > 1e-4f) p += toCam / dist * Math.Min(MarkerPullMax_U34, dist * 0.5f);
                MaxLightIcon_U35(p, dir, l, MarkerWire_U35, fade, occlude);
            }
        }

        private readonly System.Collections.Generic.List<(Vector3 a, Vector3 b)> iconSegs_U40 = new System.Collections.Generic.List<(Vector3 a, Vector3 b)>(48);

        private void MaxLightIcon_U35(Vector3 p, Vector3 dir, LightAttributes l, Vector4 wire, float fade, bool depth)
        {
            var col = new Vector4(wire.X, wire.Y, wire.Z, wire.W * fade);
            var rim = new Vector4(MarkerRim_U35.X, MarkerRim_U35.Y, MarkerRim_U35.Z, MarkerRim_U35.W * fade);
            var segs = iconSegs_U40;
            MaxIconSegs_U40(p, dir, l, segs);
            foreach (var s in segs) selSegs_U25.Add((s.a, s.b, rim, depth, MarkerRimPx_U35));
            foreach (var s in segs) selSegs_U25.Add((s.a, s.b, col, depth, MarkerLinePx_U35));
        }

        internal void MaxIconSegs_U40(Vector3 p, Vector3 dir, LightAttributes l, System.Collections.Generic.List<(Vector3 a, Vector3 b)> segs)
        {
            segs.Clear();
            float r = MarkerSizePx_U35 * camera.WorldPerPixel(p);
            var ax = Vector3.Cross(dir, Math.Abs(dir.Z) < 0.9f ? Vector3.UnitZ : Vector3.UnitX);
            ax.Normalize();
            var bx = Vector3.Cross(dir, ax);
            void Ring(Vector3 c, float rad, int n)
            {
                for (int k = 0; k < n; k++)
                {
                    float a0 = k * MathUtil.TwoPi / n, a1 = (k + 1) * MathUtil.TwoPi / n;
                    segs.Add((c + (ax * (float)Math.Cos(a0) + bx * (float)Math.Sin(a0)) * rad,
                              c + (ax * (float)Math.Cos(a1) + bx * (float)Math.Sin(a1)) * rad));
                }
            }
            switch (l.Type)
            {
                case LightType.Spot:
                {
                    float len = r * 2.4f;
                    float ang = MathUtil.DegreesToRadians(Math.Clamp(Math.Max(l.ConeOuterAngle, l.ConeInnerAngle), 8.0f, 60.0f));
                    float br = len * (float)Math.Tan(ang);
                    br = Math.Min(br, len * 1.2f);
                    var bc = p + dir * len;
                    Ring(bc, br, 16);
                    for (int k = 0; k < 4; k++)
                    {
                        float a = k * MathUtil.PiOverTwo;
                        segs.Add((p, bc + (ax * (float)Math.Cos(a) + bx * (float)Math.Sin(a)) * br));
                    }
                    segs.Add((p, bc + dir * len * 0.9f));
                    break;
                }
                case LightType.Capsule:
                {
                    float half = r * 1.2f, rad = r * 0.5f;
                    var a = p - dir * half;
                    var b = p + dir * half;
                    Ring(a, rad, 12);
                    Ring(b, rad, 12);
                    for (int k = 0; k < 4; k++)
                    {
                        float t = k * MathUtil.PiOverTwo;
                        var o = (ax * (float)Math.Cos(t) + bx * (float)Math.Sin(t)) * rad;
                        segs.Add((a + o, b + o));
                    }
                    break;
                }
                default:
                {
                    segs.Add((p - Vector3.UnitX * r, p + Vector3.UnitX * r));
                    segs.Add((p - Vector3.UnitY * r, p + Vector3.UnitY * r));
                    segs.Add((p - Vector3.UnitZ * r, p + Vector3.UnitZ * r));
                    float d = r * 0.62f / (float)Math.Sqrt(3.0);
                    for (int sx = -1; sx <= 1; sx += 2)
                        for (int sy = -1; sy <= 1; sy += 2)
                            segs.Add((p + new Vector3(sx, sy, 1) * d, p - new Vector3(sx, sy, 1) * d));
                    break;
                }
            }
        }
    }
}
