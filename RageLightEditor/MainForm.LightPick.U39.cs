using System;
using System.Windows.Forms;
using SharpDX;

namespace RageLightEditor
{
    public partial class MainForm
    {
        public const float ClickSlopPx_U39 = 8.0f;
        public const float LightPickPx_U39 = 16.0f;

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
                float px = Vector2.Distance(s, new Vector2(x, y));
                if (px > LightPickPx_U39) continue;
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
