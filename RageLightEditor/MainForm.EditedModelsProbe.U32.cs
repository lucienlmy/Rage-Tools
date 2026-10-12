using System;
using System.IO;
using System.Linq;
using RageLightEditor.Editor;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private int editedProbeStage_U32;

        private void ServiceEditedModelsProbe_U32()
        {
            string folder = Environment.GetEnvironmentVariable("RLE_EDITEDMODELSPROBE");
            if (string.IsNullOrEmpty(folder) || editedProbeStage_U32 != 0 || !worldBuilt) return;
            var L = worldRender.Lights;
            var picked = World.Visible
                .Where(e => e?.Archetype != null && L.TryGetDefs(e.Archetype.Hash, out var d) && d != null && d.Length > 0 && d[0].L != null && WorldLights.CanSave(L.GetDrawable(e.Archetype.Hash)))
                .GroupBy(e => e.Archetype.Hash).Select(g => g.First()).ToList();
            var ydr = picked.Where(e => WorldLights.SaveExtension(L.GetDrawable(e.Archetype.Hash)) == ".ydr").Take(2);
            var yft = picked.Where(e => WorldLights.SaveExtension(L.GetDrawable(e.Archetype.Hash)) == ".yft").Take(1);
            var use = ydr.Concat(yft).ToList();
            if (use.Count < 3) return;
            editedProbeStage_U32 = 1;
            var want = new System.Collections.Generic.List<(string file, float intensity)>();
            foreach (var e in use)
            {
                uint hash = e.Archetype.Hash;
                L.TryGetDefs(hash, out var defs);
                defs[0].L.Intensity = (float)Math.Round(defs[0].L.Intensity * 2.0f + 1.0f, 2);
                L.Invalidate(hash);
                MarkLightUnsaved_U32(hash);
                want.Add((editedModels_U32[hash].FileName, defs[0].L.Intensity));
            }
            ServiceEditedModels_U32();
            Console.WriteLine("EDITEDPROBE listed: " + string.Join(", ", panel.WorldEditedModels_U32.Select(m => m.FileName + (m.Unsaved ? "*" : ""))));
            int done = SaveEditedModels_U32(folder);
            ServiceEditedModels_U32();
            foreach (var (file, intensity) in want)
            {
                string path = Path.Combine(folder, file);
                var back = File.Exists(path) ? WorldLights.ReloadLight(path, 0, out _) : null;
                Console.WriteLine($"EDITEDPROBE {file}: written {File.Exists(path)} ({(File.Exists(path) ? new FileInfo(path).Length / 1024 : 0)} KB), intensity {back?.Intensity} want {intensity} {(back != null && back.Intensity == intensity ? "OK" : "FAIL")}");
            }
            Console.WriteLine($"EDITEDPROBE done: saved {done}, still unsaved {panel.WorldEditedModelsUnsaved_U32}, status {panel.WorldEditedModelsStatus_U32}");
        }
    }
}
