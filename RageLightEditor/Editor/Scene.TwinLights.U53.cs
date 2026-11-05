using System;
using System.Collections.Generic;
using System.Linq;
using CodeWalker.GameFiles;

namespace RageLightEditor.Editor
{
    public partial class Scene
    {
        private readonly Dictionary<LoadedFile, List<LightAttributes>> twinSeen_U53 = new Dictionary<LoadedFile, List<LightAttributes>>();

        public static string TwinKey_U53(LoadedFile f) =>
            string.IsNullOrWhiteSpace(f?.Path) ? null : System.IO.Path.GetFileName(f.Path).ToLowerInvariant();

        public int SyncTwinLights_U53()
        {
            var groups = Files.Where(f => f != null && TwinKey_U53(f) != null)
                              .GroupBy(TwinKey_U53).Where(g => g.Count() > 1).ToList();
            if (groups.Count == 0) { twinSeen_U53.Clear(); return 0; }

            var own = new Dictionary<LoadedFile, List<LightAttributes>>();
            foreach (var l in Lights)
            {
                var f = OwnerFile(l);
                if (f == null) continue;
                if (!own.TryGetValue(f, out var list)) own[f] = list = new List<LightAttributes>();
                list.Add(l);
            }
            List<LightAttributes> Cur(LoadedFile f) => own.TryGetValue(f, out var x) ? x : new List<LightAttributes>();

            int copied = 0;
            var live = new HashSet<LoadedFile>();
            foreach (var g in groups)
            {
                var files = g.ToList();
                foreach (var f in files) live.Add(f);
                var changed = files.Where(f => twinSeen_U53.TryGetValue(f, out var seen) && !SameLights_U47(seen, Cur(f))).ToList();
                LoadedFile source = null;
                if (changed.Count == 1) source = changed[0];
                else if (changed.Count > 1 && !changed.All(f => SameLights_U47(Cur(changed[0]), Cur(f))))
                {
                    var sel = OwnerFile(SelectedLight);
                    if (changed.Contains(sel)) source = sel;
                    else if (changed.Contains(ActiveFile)) source = ActiveFile;
                }
                if (source != null)
                {
                    var from = Cur(source);
                    foreach (var t in files)
                    {
                        if (ReferenceEquals(t, source) || SameLights_U47(from, Cur(t))) continue;
                        ReplaceLightsOf_U53(t, from);
                        t.Dirty = t.Dirty || source.Dirty;
                        copied++;
                    }
                    if (copied > 0)
                    {
                        own.Clear();
                        foreach (var l in Lights)
                        {
                            var f = OwnerFile(l);
                            if (f == null) continue;
                            if (!own.TryGetValue(f, out var list)) own[f] = list = new List<LightAttributes>();
                            list.Add(l);
                        }
                    }
                }
                foreach (var f in files) twinSeen_U53[f] = Cur(f).Select(CloneLight).ToList();
            }
            foreach (var k in twinSeen_U53.Keys.Where(k => !live.Contains(k)).ToList()) twinSeen_U53.Remove(k);
            return copied;
        }

        private void ReplaceLightsOf_U53(LoadedFile target, List<LightAttributes> from)
        {
            var keepSel = SelectedIndices.Where(i => i >= 0 && i < Lights.Count).Select(i => Lights[i]).ToList();
            for (int i = Lights.Count - 1; i >= 0; i--)
            {
                var l = Lights[i];
                if (!ReferenceEquals(OwnerFile(l), target)) continue;
                Lights.RemoveAt(i);
                ownerOf.Remove(l);
                instanceGroupOf.Remove(l);
            }
            foreach (var src in from)
            {
                var c = CloneLight(src);
                Lights.Add(c);
                ownerOf[c] = target;
            }
            SelectedIndices.Clear();
            foreach (var l in keepSel)
            {
                int i = Lights.IndexOf(l);
                if (i >= 0) SelectedIndices.Add(i);
            }
        }
    }
}
