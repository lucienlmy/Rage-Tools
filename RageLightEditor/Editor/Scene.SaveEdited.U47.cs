using System.Collections.Generic;
using System.Linq;
using CodeWalker.GameFiles;

namespace RageLightEditor.Editor
{
    public partial class Scene
    {
        public List<LoadedFile> LastSaved_U47 = new List<LoadedFile>();

        public void RememberSaved_U47(LoadedFile f)
        {
            if (f == null) return;
            f.SavedLights_U47 = Lights.Where(l => ReferenceEquals(OwnerFile(l), f)).Select(CloneLight).ToList();
        }

        public void MarkChangedFiles_U47()
        {
            var now = new Dictionary<LoadedFile, List<LightAttributes>>();
            foreach (var l in Lights)
            {
                var f = OwnerFile(l);
                if (f == null) continue;
                if (!now.TryGetValue(f, out var list)) now[f] = list = new List<LightAttributes>();
                list.Add(l);
            }
            foreach (var f in Files)
            {
                if (f.Dirty) continue;
                if (f.SavedLights_U47 == null) { f.Dirty = true; continue; }
                now.TryGetValue(f, out var cur);
                if (!SameLights_U47(f.SavedLights_U47, cur)) f.Dirty = true;
            }
        }

        public void MarkAllDirty_U47()
        {
            if (ExternalEditing) return;
            foreach (var f in Files) f.Dirty = true;
        }

        private static bool SameLights_U47(List<LightAttributes> saved, List<LightAttributes> cur)
        {
            int n = cur?.Count ?? 0;
            if (saved.Count != n) return false;
            for (int i = 0; i < n; i++)
            {
                var a = saved[i]; var b = cur[i];
                if (!LightParamsEqual(a, b) || a.Position != b.Position || a.Direction != b.Direction ||
                    a.Tangent != b.Tangent || a.BoneId != b.BoneId) return false;
            }
            return true;
        }
    }
}
