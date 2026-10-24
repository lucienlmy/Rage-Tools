using System;
using System.Collections.Generic;
using System.Linq;
using RageLightEditor.Editor;
using SharpDX;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private static bool SameWorld_U42(Matrix a, Matrix b)
        {
            for (int i = 0; i < 16; i++) if (Math.Abs(a[i] - b[i]) > 1e-4f) return false;
            return true;
        }

        internal string SetImportEnabled_U42(Scene sc, string path, bool on)
        {
            var reg = sc?.Imports_R1;
            var entry = reg?.Find(path);
            if (entry == null) return "that .ytyp is not one of the imports";
            entry.Enabled = on;

            int meshes = 0;
            void Show(Rendering.RenderMesh m) { if (m != null && m.Visible != on) { m.Visible = on; meshes++; } }
            if (entry.Model?.Meshes != null) foreach (var m in entry.Model.Meshes) Show(m);

            var propByHash = new Dictionary<uint, MloProp>();
            foreach (var e in reg.Entries) foreach (var p in e.Props) if (p != null) propByHash[p.Hash] = p;
            var fileByKey = new Dictionary<string, LoadedFile>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in sc.Files) if (f != null && f.FromMlo && !string.IsNullOrEmpty(f.Path)) fileByKey[f.Path] = f;

            int copies = 0;
            var touched = new HashSet<LoadedFile>();
            foreach (var info in entry.Result?.Entities ?? new List<MloEntityInfo>())
            {
                if (info == null) continue;
                foreach (var m in info.PlacedMeshes_O3) Show(m);
                if (!propByHash.TryGetValue(info.ArchetypeHash, out var prop)) continue;
                if (!fileByKey.TryGetValue(prop.Path ?? prop.Name, out var file)) continue;
                int idx = -1;
                int count = 1 + file.ExtraPlacements.Count;
                for (int k = 0; k < count && idx < 0; k++)
                {
                    var w = k == 0 ? (file.HasPlacement ? file.Placement : Matrix.Identity) : file.ExtraPlacements[k - 1];
                    if (k == 0 && !file.HasPlacement) continue;
                    if (file.HiddenPlacements_U42.Contains(k) == on && SameWorld_U42(w, info.PlacedAt_O3)) idx = k;
                }
                if (idx < 0) continue;
                if (on) file.HiddenPlacements_U42.Remove(idx); else file.HiddenPlacements_U42.Add(idx);
                copies++;
                touched.Add(file);
            }
            sc.TouchGeometry_U42();
            string msg = $"{(on ? "Turned on" : "Turned off")} {entry.Name}: {meshes} meshes and {copies} prop copies {(on ? "shown" : "hidden")} on {touched.Count} props.";
            Console.WriteLine("IMPORTENABLE " + msg);
            return msg;
        }

        private static readonly bool ToggleProbeOn_U42 = Environment.GetEnvironmentVariable("RLE_IMPORTTOGGLEPROBE") == "1";
        private bool toggleProbeDone_U42;

        private void ToggleProbe_U42()
        {
            if (!ToggleProbeOn_U42 || toggleProbeDone_U42 || (DebugMlo != null && !debugMloDone) || scene?.Imports_R1 == null || scene.Imports_R1.Count < 2) return;
            toggleProbeDone_U42 = true;
            int Meshes() => (scene.MloModel?.Meshes.Count(m => m != null && m.Visible) ?? 0) + scene.Files.Where(f => f.FromMlo).Sum(f => f.Model?.Meshes.Count(m => m != null && m.Visible) ?? 0);
            int Lights() => scene.BuildGpuLights(new Rendering.GpuLight[4096], 12, false, 0f, null, camera.Position);
            int m0 = Meshes(), l0 = Lights();
            var a = scene.Imports_R1.Entries[0];
            var b = scene.Imports_R1.Entries[1];
            SetImportEnabled_U42(scene, a.Path, false);
            int m1 = Meshes(), l1 = Lights();
            SetImportEnabled_U42(scene, b.Path, false);
            int m2 = Meshes(), l2 = Lights();
            foreach (var f in scene.Files.Where(f => f.FromMlo && f.Visible))
            {
                int n = 1 + f.ExtraPlacements.Count;
                int lit = scene.Lights.Count(l => scene.OwnerFile(l) == f);
                if (lit > 0 && f.HiddenPlacements_U42.Count < n)
                    Console.WriteLine($"TOGGLEPROBE still lit with both off: {f.Name} placements {n}, hidden {f.HiddenPlacements_U42.Count}, lights {lit}, hasPlacement {f.HasPlacement}");
            }
            SetImportEnabled_U42(scene, a.Path, true);
            SetImportEnabled_U42(scene, b.Path, true);
            int m3 = Meshes(), l3 = Lights();
            Console.WriteLine($"TOGGLEPROBE visible meshes / lights: both on {m0}/{l0}; {a.Name} off {m1}/{l1}; both off {m2}/{l2}; both back on {m3}/{l3}");
        }

        private void ServiceImportEnable_U42()
        {
            ToggleProbe_U42();
            var req = panel?.RequestToggleImport_U42;
            if (string.IsNullOrEmpty(req)) return;
            panel.RequestToggleImport_U42 = null;
            var e = scene?.Imports_R1?.Find(req);
            if (e == null) return;
            panel.MloStatus = SetImportEnabled_U42(scene, req, !e.Enabled);
        }
    }
}
