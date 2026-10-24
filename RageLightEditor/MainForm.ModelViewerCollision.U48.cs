using System.Collections.Generic;
using System.Linq;
using CodeWalker.GameFiles;
using RageLightEditor.Editor;
using RageLightEditor.Rendering;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private readonly List<CollisionMesh> mvEmbedded_U48 = new List<CollisionMesh>();
        private object mvEmbeddedKey_U48;
        private AssetPreview mvEmbeddedPreview_U48;
        private bool mvEmbeddedLogged_U48;

        internal static List<Bounds> EmbeddedBounds_U48(AssetPreview pv)
        {
            var list = new List<Bounds>();
            if (pv == null) return list;
            void Add(Bounds b) { if (b != null && !list.Contains(b)) list.Add(b); }
            if (pv.Drawable is Drawable d) Add(d.Bound);
            if (pv.Drawable is FragDrawable fd)
            {
                Add(fd.Bound);
                Add(fd.OwnerFragment?.PhysicsLODGroup?.PhysicsLOD1?.Bound);
            }
            if (list.Count == 0 && pv.Yft?.Fragment != null)
            {
                Add(pv.Yft.Fragment.Drawable?.Bound);
                Add(pv.Yft.Fragment.PhysicsLODGroup?.PhysicsLOD1?.Bound);
            }
            return list;
        }

        private List<CollisionMesh> EmbeddedCollision_U48()
        {
            var pv = modelViewPreview;
            object key = (object)pv?.Drawable ?? pv?.Yft;
            if (ReferenceEquals(pv, mvEmbeddedPreview_U48) && ReferenceEquals(key, mvEmbeddedKey_U48)) return mvEmbedded_U48;
            mvEmbeddedPreview_U48 = pv;
            mvEmbeddedKey_U48 = key;
            mvEmbeddedLogged_U48 = false;
            mvEmbedded_U48.Clear();
            if (pv == null || pv.Ybn != null) return mvEmbedded_U48;
            var bounds = EmbeddedBounds_U48(pv);
            if (bounds.Count == 0) return mvEmbedded_U48;
            if (collisionView == null) collisionView = new CollisionView(gameFiles);
            var keep = collisionView.Alpha;
            collisionView.Alpha = 0.6f;
            try
            {
                foreach (var b in bounds)
                {
                    var m = collisionView.BuildFromBounds(b);
                    if (m != null && !m.IsEmpty) mvEmbedded_U48.Add(m);
                }
            }
            finally { collisionView.Alpha = keep; }
            return mvEmbedded_U48;
        }

        private void UpdateViewerCollision_U48()
        {
            var meshes = EmbeddedCollision_U48();
            if (screenshotPath != null && meshes.Count > 0 && System.Environment.GetEnvironmentVariable("RLE_MVCOLLISION") == "1") ModelView.ShowCollision_U48 = true;
            ModelView.CollisionParts_U48 = meshes.Count;
            ModelView.CollisionTriangles_U48 = meshes.Sum(m => m.Vertices.Length / 3);
        }

        private void DrawViewerCollision_U48(SharpDX.Direct3D11.DeviceContext ctx, Camera cam)
        {
            if (!ModelView.ShowCollision_U48 || mvEmbedded_U48.Count == 0) return;
            foreach (var m in mvEmbedded_U48) triRenderer.AddTriangles(m.Vertices, SharpDX.Vector3.Zero);
            triRenderer.Flush(ctx, cam.ViewProjMatrix, CommonStates.BlendAlpha, CommonStates.DepthReadOnly);
            if (screenshotPath != null && !mvEmbeddedLogged_U48)
            {
                mvEmbeddedLogged_U48 = true;
                System.Console.WriteLine($"MODELVIEW collision drawn: {mvEmbedded_U48.Count} part(s), {ModelView.CollisionTriangles_U48} triangles");
            }
        }
    }
}
