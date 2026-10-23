using System;
using System.Windows.Forms;
using SharpDX;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private int pickProbeFrames_U39;
        private static readonly bool PickProbeOn_U39 = Environment.GetEnvironmentVariable("RLE_PICKPROBE") == "1";

        private void PickProbe_U39()
        {
            if (!PickProbeOn_U39 || pickProbeFrames_U39 < 0 || scene.Lights.Count == 0 || deviceResources == null) return;
            if (++pickProbeFrames_U39 < 3) return;
            pickProbeFrames_U39 = -1;
            float w = deviceResources.Width, h = deviceResources.Height;
            float hourWas = panel.PreviewHour;
            bool fadeWas = settings.MarkerFadeU36, occWas = settings.MarkerOccludeU36;
            settings.MarkerFadeU36 = false; settings.MarkerOccludeU36 = false;
            int tried = 0, hit = 0, nearer = 0, missed = 0;
            for (int i = 0; i < scene.Lights.Count; i++)
            {
                var p = scene.GetInstance(scene.Lights[i]).WorldPosition;
                if (!LightScreenPos_U39(p, out var s)) continue;
                if (s.X < w * 0.24f || s.X > w * 0.64f || s.Y < 80 || s.Y > h - 80) continue;
                float depth = Vector3.Distance(camera.Position, p);
                tried++;
                scene.SelectedIndices.Clear();
                int x = (int)s.X + 5, y = (int)s.Y - 4;
                OnMouseDownEv(this, new MouseEventArgs(MouseButtons.Right, 1, x, y, 0));
                OnMouseMoveEv(this, new MouseEventArgs(MouseButtons.Right, 0, x + 2, y + 1, 0));
                OnMouseMoveEv(this, new MouseEventArgs(MouseButtons.Right, 0, x + 1, y + 2, 0));
                OnMouseUpEv(this, new MouseEventArgs(MouseButtons.Right, 1, x + 1, y + 2, 0));
                int got = scene.SelectedIndex;
                if (got == i) hit++;
                else if (got >= 0 && LightScreenPos_U39(scene.GetInstance(scene.Lights[got]).WorldPosition, out var gs) &&
                         Vector2.Distance(gs, new Vector2(x + 1, y + 2)) <= Vector2.Distance(s, new Vector2(x + 1, y + 2)) + 2.0f) nearer++;
                else missed++;
            }
            int coneTried = 0, coneHit = 0;
            var segsTmp = new System.Collections.Generic.List<(Vector3 a, Vector3 b)>();
            for (int i = 0; i < scene.Lights.Count; i++)
            {
                var l = scene.Lights[i];
                if (l.Type != CodeWalker.GameFiles.LightType.Spot) continue;
                var inst = scene.GetInstance(l);
                var p = inst.WorldPosition;
                if (!LightScreenPos_U39(p, out var s0) || s0.X < w * 0.24f || s0.X > w * 0.64f || s0.Y < 80 || s0.Y > h - 80) continue;
                var dir = inst.WorldDirection.LengthSquared() > 1e-6f ? Vector3.Normalize(inst.WorldDirection) : -Vector3.UnitZ;
                MaxIconSegs_U40(p, dir, l, segsTmp);
                foreach (var target in new[] { segsTmp[segsTmp.Count - 1].b, segsTmp[4].a })
                {
                    if (!LightScreenPos_U39(target, out var st)) continue;
                    if (Vector2.Distance(st, s0) < LightPickPx_U39 + 4f) continue;
                    coneTried++;
                    scene.SelectedIndices.Clear();
                    int x = (int)st.X + 3, y = (int)st.Y - 2;
                    OnMouseDownEv(this, new MouseEventArgs(MouseButtons.Right, 1, x, y, 0));
                    OnMouseMoveEv(this, new MouseEventArgs(MouseButtons.Right, 0, x + 2, y + 1, 0));
                    OnMouseUpEv(this, new MouseEventArgs(MouseButtons.Right, 1, x + 2, y + 1, 0));
                    if (scene.SelectedIndex == i) coneHit++;
                }
            }
            Console.WriteLine($"PICKPROBE clicks on a spot's cone ring or target line, away from its centre: {coneTried} tried, {coneHit} picked that light");
            settings.MarkerFadeU36 = fadeWas; settings.MarkerOccludeU36 = occWas;
            Console.WriteLine($"PICKPROBE right-clicks 5 px off a light with a 3 px wobble: {tried} tried, {hit} picked that light, {nearer} picked an even nearer one, {missed} missed; time of day {(Math.Abs(panel.PreviewHour - hourWas) < 0.001f ? "unchanged" : "CHANGED")}");
        }
    }
}
