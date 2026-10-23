using System;
using System.Windows.Forms;
using SharpDX;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private bool brushing_U37;

        private bool BrushSelectActive_U37 =>
            panel != null && panel.BrushSelect_U37 && !panel.WorldMode && !panel.MloMode && !panel.MaterialMode;

        private bool BrushSelectDown_U37(int x, int y)
        {
            if (!BrushSelectActive_U37) return false;
            brushing_U37 = true;
            BrushApply_U37(x, y, (ModifierKeys & Keys.Alt) != 0);
            return true;
        }

        private void BrushSelectMove_U37(int x, int y, bool leftHeld)
        {
            panel.BrushCursor_U37 = BrushSelectActive_U37 ? new System.Numerics.Vector2(x, y) : (System.Numerics.Vector2?)null;
            if (brushing_U37 && leftHeld) BrushApply_U37(x, y, (ModifierKeys & Keys.Alt) != 0);
        }

        private void BrushSelectUp_U37() => brushing_U37 = false;

        internal int BrushApply_U37(int x, int y, bool remove)
        {
            if (deviceResources == null) return 0;
            float w = deviceResources.Width, h = deviceResources.Height;
            float r = panel.BrushRadius_U37;
            var vp = camera.ViewProjMatrix;
            var cam = camera.Position;
            int changed = 0;
            for (int i = 0; i < scene.Lights.Count; i++)
            {
                var l = scene.Lights[i];
                if (l == null || !scene.TypeShown_U35(l.Type)) continue;
                var f = scene.OwnerFile(l);
                if (f != null && !f.Visible) continue;
                var p = scene.GetInstance(l).WorldPosition;
                var c = Vector4.Transform(new Vector4(p, 1f), vp);
                if (c.W <= 1e-4f) continue;
                float sx = (c.X / c.W * 0.5f + 0.5f) * w;
                float sy = (1f - (c.Y / c.W * 0.5f + 0.5f)) * h;
                float dx = sx - x, dy = sy - y;
                if (dx * dx + dy * dy > r * r) continue;
                float dist = Vector3.Distance(cam, p);
                if (settings.MarkerFadeU36 && MarkerFade_U34(dist) <= 0.02f) continue;
                if (settings.MarkerOccludeU36 && LightHidden_U37(p, dist)) continue;
                if (remove) { if (scene.SelectedIndices.Remove(i)) changed++; }
                else if (!scene.SelectedIndices.Contains(i)) { scene.SelectedIndices.Add(i); changed++; }
            }
            if (changed > 0) panel.MloStatus = $"{scene.SelectedIndices.Count} light{(scene.SelectedIndices.Count == 1 ? "" : "s")} selected";
            return changed;
        }

        private int brushProbeFrames_U37;
        private static readonly bool BrushProbeOn_U37 = Environment.GetEnvironmentVariable("RLE_BRUSHPROBE") == "1";

        private void BrushProbe_U37()
        {
            if (!BrushProbeOn_U37 || brushProbeFrames_U37 < 0 || scene.Lights.Count == 0) return;
            if (++brushProbeFrames_U37 < 3) return;
            brushProbeFrames_U37 = -1;
            float rWas = panel.BrushRadius_U37;
            bool occWas = settings.MarkerOccludeU36;
            int cx = deviceResources.Width / 2, cy = deviceResources.Height / 2;
            panel.BrushRadius_U37 = Math.Max(deviceResources.Width, deviceResources.Height);
            scene.SelectedIndices.Clear();
            settings.MarkerOccludeU36 = true;
            int seen = BrushApply_U37(cx, cy, false);
            scene.SelectedIndices.Clear();
            settings.MarkerOccludeU36 = false;
            int all = BrushApply_U37(cx, cy, false);
            int removed = BrushApply_U37(cx, cy, true);
            settings.MarkerOccludeU36 = occWas;
            panel.BrushRadius_U37 = 30f;
            scene.SelectedIndices.Clear();
            int small = BrushApply_U37(cx, cy, false);
            panel.BrushRadius_U37 = rWas;
            Console.WriteLine($"BRUSHPROBE whole screen: {seen} visible lights selected with walls hiding, {all} with walls ignored; Alt removed {removed}, left {scene.SelectedIndices.Count - small}; a 30 px brush at the centre took {small}");
        }

        private bool LightHidden_U37(Vector3 p, float dist)
        {
            if (dist <= MarkerPullMax_U34) return false;
            var ray = new Ray(camera.Position, (p - camera.Position) / dist);
            float limit = dist - MarkerPullMax_U34;
            bool Blocks(Rendering.RenderModel m)
            {
                if (m == null) return false;
                foreach (var mesh in m.Meshes)
                {
                    if (!mesh.Visible) continue;
                    var b = mesh.WorldBounds;
                    if (b.Maximum.X <= b.Minimum.X) continue;
                    if (!ray.Intersects(ref b, out float bt) || bt > limit) continue;
                    if (mesh.RayHit(ref ray, out float t) && t < limit) return true;
                }
                return false;
            }
            foreach (var f in scene.Files)
                if (f.Visible && Blocks(f.Model)) return true;
            return scene.MloVisible && Blocks(scene.MloModel);
        }
    }
}
