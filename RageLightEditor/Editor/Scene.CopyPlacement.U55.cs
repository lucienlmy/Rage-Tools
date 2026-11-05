using System;
using CodeWalker.GameFiles;
using SharpDX;

namespace RageLightEditor.Editor
{
    public partial class Scene
    {
        public static Matrix PlacementOf_U55(LoadedFile f, int copy) =>
            copy <= 0 ? (f.HasPlacement ? f.Placement : Matrix.Identity) : f.ExtraPlacements[copy - 1];

        public static DrawableBase DrawableOf_U55(LoadedFile f) =>
            f?.Drawable ?? (DrawableBase)f?.Ydr?.Drawable ?? f?.Yft?.Fragment?.Drawable;

        public static int NearestCopy_U55(LoadedFile f, Vector3 world)
        {
            if (f == null || f.ExtraPlacements.Count == 0) return 0;
            var d = DrawableOf_U55(f);
            bool box = d != null && d.BoundingBoxMax.X >= d.BoundingBoxMin.X && d.BoundingBoxMax.Y >= d.BoundingBoxMin.Y;
            int best = 0;
            float bestD = float.MaxValue, bestPivot = float.MaxValue;
            for (int k = 0; k <= f.ExtraPlacements.Count; k++)
            {
                if (f.HiddenPlacements_U42.Contains(k)) continue;
                var lp = Vector3.TransformCoordinate(world, Matrix.Invert(PlacementOf_U55(f, k)));
                float pivot = lp.Length();
                float dist = box ? (lp - Vector3.Clamp(lp, d.BoundingBoxMin, d.BoundingBoxMax)).Length() : pivot;
                if (dist < bestD - 1e-4f || (Math.Abs(dist - bestD) <= 1e-4f && pivot < bestPivot))
                {
                    bestD = dist; bestPivot = pivot; best = k;
                }
            }
            return best;
        }

        public void MakePrimaryCopy_U55(LoadedFile f, int copy)
        {
            if (f == null || copy <= 0 || copy > f.ExtraPlacements.Count) return;
            var first = PlacementOf_U55(f, 0);
            f.Placement = f.ExtraPlacements[copy - 1];
            f.HasPlacement = true;
            f.ExtraPlacements[copy - 1] = first;
            bool h0 = f.HiddenPlacements_U42.Contains(0), hk = f.HiddenPlacements_U42.Contains(copy);
            f.HiddenPlacements_U42.Remove(0); f.HiddenPlacements_U42.Remove(copy);
            if (hk) f.HiddenPlacements_U42.Add(0);
            if (h0) f.HiddenPlacements_U42.Add(copy);
            GeometryVersion++;
        }

        private Matrix LightFrame_U55(LightAttributes l, LoadedFile owner)
        {
            var frame = owner != null && owner.HasPlacement ? owner.Placement : Matrix.Identity;
            var skeleton = owner?.Skeleton;
            if (l.BoneId != 0 && skeleton?.BonesMap != null && skeleton.BonesMap.TryGetValue(l.BoneId, out var bone) && bone != null)
                frame = bone.AnimTransform * frame;
            return frame;
        }

        public void PutLightAtWorld_U55(LightAttributes l, LoadedFile target, Vector3 worldPos, Vector3 worldDir, Vector3 worldTan)
        {
            var skeleton = target?.Skeleton;
            if (l.BoneId != 0 && (skeleton?.BonesMap == null || !skeleton.BonesMap.ContainsKey(l.BoneId))) l.BoneId = 0;
            ownerOf[l] = target;
            var inv = Matrix.Invert(LightFrame_U55(l, target));
            l.Position = Vector3.TransformCoordinate(worldPos, inv);
            var dir = Vector3.TransformNormal(worldDir, inv);
            var tan = Vector3.TransformNormal(worldTan, inv);
            if (dir.LengthSquared() > 1e-12f) l.Direction = Vector3.Normalize(dir);
            if (tan.LengthSquared() > 1e-12f) l.Tangent = Vector3.Normalize(tan);
        }
    }
}
