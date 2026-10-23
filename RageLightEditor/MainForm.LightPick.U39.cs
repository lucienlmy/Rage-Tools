using System;
using System.Windows.Forms;
using SharpDX;

namespace RageLightEditor
{
    public partial class MainForm
    {
        public const float ClickSlopPx_U39 = 8.0f;
        public const float LightPickPx_U39 = 16.0f;
        public const float IconPickPx_U40 = 9.0f;

        private readonly System.Collections.Generic.List<(Vector3 a, Vector3 b)> pickSegs_U40 = new System.Collections.Generic.List<(Vector3 a, Vector3 b)>(48);

        internal float IconScreenDistance_U40(int i, Vector2 cursor)
        {
            var l = scene.Lights[i];
            var inst = scene.GetInstance(l);
            var p = inst.WorldPosition;
            var dir = inst.WorldDirection.LengthSquared() > 1e-6f ? Vector3.Normalize(inst.WorldDirection) : -Vector3.UnitZ;
            var toCam = camera.Position - p;
            float dist = toCam.Length();
            if (!scene.IsSelected(i) && settings.MarkerOccludeU36 && dist > 1e-4f) p += toCam / dist * Math.Min(MarkerPullMax_U34, dist * 0.5f);
            float best = LightScreenPos_U39(p, out var c) ? Vector2.Distance(c, cursor) : float.MaxValue;
            MaxIconSegs_U40(p, dir, l, pickSegs_U40);
            foreach (var (a, b) in pickSegs_U40)
            {
                if (!LightScreenPos_U39(a, out var sa) || !LightScreenPos_U39(b, out var sb)) continue;
                var ab = sb - sa;
                float len2 = ab.LengthSquared();
                float t = len2 > 1e-6f ? Math.Clamp(Vector2.Dot(cursor - sa, ab) / len2, 0f, 1f) : 0f;
                best = Math.Min(best, Vector2.Distance(sa + ab * t, cursor));
            }
            return best;
        }

        internal bool LightScreenPos_U39(Vector3 p, out Vector2 s)
        {
            s = default;
            if (deviceResources == null) return false;
            var c = Vector4.Transform(new Vector4(p, 1f), camera.ViewProjMatrix);
            if (c.W <= 1e-4f) return false;
            s = new Vector2((c.X / c.W * 0.5f + 0.5f) * deviceResources.Width,
                            (1f - (c.Y / c.W * 0.5f + 0.5f)) * deviceResources.Height);
            return true;
        }

        internal int LightUnderCursor_U39(int x, int y)
        {
            int best = -1;
            float bestPx = float.MaxValue, bestDepth = float.MaxValue;
            var cam = camera.Position;
            for (int i = 0; i < scene.Lights.Count; i++)
            {
                var l = scene.Lights[i];
                if (l == null || !scene.TypeShown_U35(l.Type)) continue;
                var f = scene.OwnerFile(l);
                if (f != null && !f.Visible) continue;
                var p = scene.GetInstance(l).WorldPosition;
                if (!LightScreenPos_U39(p, out var s)) continue;
                var cursor = new Vector2(x, y);
                float centre = Vector2.Distance(s, cursor);
                float px = Math.Min(centre, IconScreenDistance_U40(i, cursor));
                if (centre > LightPickPx_U39 && px > IconPickPx_U40) continue;
                float depth = Vector3.Distance(cam, p);
                bool sel = scene.IsSelected(i);
                if (!sel && settings.MarkerFadeU36 && MarkerFade_U34(depth) <= 0.02f) continue;
                if (!sel && settings.MarkerOccludeU36 && LightHidden_U37(p, depth)) continue;
                bool better = px < bestPx - 2.0f || (px <= bestPx + 2.0f && depth < bestDepth);
                if (!better) continue;
                best = i; bestPx = px; bestDepth = depth;
            }
            return best;
        }
    }
}
