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
            for (int i = 0; i < scene.Lights.Count; i++)
            {
                var l = scene.Lights[i];
                var ownerFile = scene.OwnerFile(l);
                if (ownerFile != null && !ownerFile.Visible) continue;
                var inst = scene.GetInstance(l);
                var p = inst.WorldPosition;
                var dir = inst.WorldDirection.LengthSquared() > 1e-6f ? Vector3.Normalize(inst.WorldDirection) : -Vector3.UnitZ;
                if (scene.IsSelected(i))
                {
                    lightIcons_U26.Add((p, dir, l.Type, GizmoStyle.Hot));
                    continue;
                }
                var toCam = cam - p;
                float dist = toCam.Length();
                float fade = MarkerFade_U34(dist);
                if (fade <= 0.02f) continue;
                if (dist > 1e-4f) p += toCam / dist * Math.Min(MarkerPullMax_U34, dist * 0.5f);
                var g = GizmoStyle.Y;
                lightIconsDepth_U34.Add((p, dir, l.Type, new Vector4(g.X, g.Y, g.Z, fade)));
            }
        }
    }
}
