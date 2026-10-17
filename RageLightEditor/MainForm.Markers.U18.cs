using System;
using CodeWalker.GameFiles;
using RageLightEditor.Editor;
using SharpDX;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private void DrawLightMarkers()
        {
            for (int i = 0; i < scene.Lights.Count; i++)
            {
                var l = scene.Lights[i];
                var ownerFile = scene.OwnerFile(l);
                if (ownerFile != null && !ownerFile.Visible) continue;
                var inst = scene.GetInstance(l);
                var dir = inst.WorldDirection.LengthSquared() > 1e-6f ? Vector3.Normalize(inst.WorldDirection) : -Vector3.UnitZ;
                lightIcons_U26.Add((inst.WorldPosition, dir, l.Type, scene.IsSelected(i) ? GizmoStyle.Hot : GizmoStyle.Y));
            }
        }
    }
}
