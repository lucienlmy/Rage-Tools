using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CodeWalker.GameFiles;
using RageLightEditor.Editor;
using RageLightEditor.Rendering;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private List<GameMloInfo_U49> gameMlos_U49;

        internal List<GameMloInfo_U49> GameMloList_U49()
        {
            if (gameMlos_U49 != null) return gameMlos_U49;
            var c = gameFiles?.Cache;
            if (gameFiles == null || !gameFiles.Ready || c?.YtypDict == null) return null;
            var byHash = new Dictionary<uint, GameMloInfo_U49>();
            foreach (var ytyp in c.YtypDict.Values)
            {
                if (ytyp?.AllArchetypes == null) continue;
                foreach (var a in ytyp.AllArchetypes)
                {
                    if (!(a is MloArchetype m) || byHash.ContainsKey(a.Hash)) continue;
                    byHash[a.Hash] = new GameMloInfo_U49
                    {
                        Hash = a.Hash,
                        Name = string.IsNullOrEmpty(a.Name) ? a.Hash.ToString() : a.Name,
                        Ytyp = ytyp.Name ?? "",
                        Rooms = Math.Max(0, (m.rooms?.Length ?? 0) - 1),
                        Entities = m.entities?.Length ?? 0,
                        Arch = m,
                    };
                }
            }
            gameMlos_U49 = byHash.Values.OrderBy(m => m.Name.All(char.IsDigit)).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ToList();
            Console.WriteLine($"GAMEMLOS {gameMlos_U49.Count} interiors in the game files");
            return gameMlos_U49;
        }

        private void ServiceGameMlos_U49()
        {
            if (panel == null) return;
            if (panel.GameMloSource_U49 == null) panel.GameMloSource_U49 = GameMloList_U49;
            var want = Environment.GetEnvironmentVariable("RLE_GAMEMLO");
            if (!string.IsNullOrEmpty(want) && screenshotPath != null && !gameMloProbeDone_U49 && GameMloList_U49() is List<GameMloInfo_U49> list)
            {
                gameMloProbeDone_U49 = true;
                panel.RequestImportGameMlo_U49 = list.FirstOrDefault(m => string.Equals(m.Name, want, StringComparison.OrdinalIgnoreCase));
                if (panel.RequestImportGameMlo_U49 == null) Console.WriteLine($"GAMEMLO {want} is not in the list");
            }
            var pick = panel.RequestImportGameMlo_U49;
            if (pick == null) return;
            panel.RequestImportGameMlo_U49 = null;
            ImportGameMlo_U49(pick);
        }

        private bool gameMloProbeDone_U49;

        private void ImportGameMlo_U49(GameMloInfo_U49 pick)
        {
            if (pick?.Arch == null || gameFiles == null || !gameFiles.Ready) return;
            try
            {
                Cursor = Cursors.WaitCursor;
                panel.MloStatus = "Importing " + pick.Name + "...";
                ResetIfImportKindChanged("ytyp");
                bool wasEmpty = !scene.Files.Any(f => f.FromMlo);
                mloImporter.ImportPropLights = panel.ImportPropLights;
                mloImporter.ImportAllProps = panel.ImportAllProps;
                mloImporter.ImportVegetation = panel.ImportVegetation;
                ResetTimecycleForImport();

                var r = mloImporter.ImportGameMlo_U49(pick.Arch, out var model, s => panel.MloStatus = s);
                bool first = true;
                foreach (var mesh in model.Meshes)
                {
                    model.Bounds = first ? mesh.WorldBounds : SharpDX.BoundingBox.Merge(model.Bounds, mesh.WorldBounds);
                    first = false;
                }
                scene.AppendMlo(model, r);
                NoteYtypImport_R1("GTA V\\" + pick.Name + ".ytyp", r, model);
                foreach (var p in r.Props)
                    scene.AddImportedProp(p.Path, p.Ydr, p.Yft, p.Model, p.Skeleton, p.Lights,
                        p.FirstWorld, p.Placed, p.ReadOnly, p.Name, p.Placements.Skip(1).ToList(),
                        drawable: p.Drawable);

                for (int i = 0; i < r.RoomTimecycleHashes.Count; i++)
                {
                    var mi = timecycle.FindModifier(r.RoomTimecycleHashes[i], i < r.RoomTimecycles.Count ? r.RoomTimecycles[i] : null);
                    if (mi >= 0) { timecycle.SelectedModifier = mi; break; }
                }
                if (scene.Lights.Count > 64 && panel.ShadowMode == 0) { panel.ShadowMode = 2; shadowPickDirty = true; }

                panel.MloStatus = $"Imported {pick.Name}: {r.Placed}/{r.EntityCount} placed in {r.Seconds:0.00}s\n" +
                                  $"Scene now: {scene.Files.Count(f => f.FromMlo)} light props, {scene.Lights.Count} lights" +
                                  (r.Missing > 0 ? $"\n{r.Missing} not found: {string.Join(", ", r.MissingNames.Take(4))}" : "");
                Console.WriteLine($"GAMEMLO imported {pick.Name} ({pick.Ytyp}): placed {r.Placed}/{r.EntityCount}, missing {r.Missing}, light props {r.Props.Count}, lights {scene.Lights.Count}, rooms {r.Interiors.FirstOrDefault()?.Arch?.rooms?.Length ?? 0}, meshes {model.Meshes.Count}");
                if (screenshotPath != null)
                    foreach (var l in scene.Lights.Where(l => (l.Flags & LightDefs.FlagDrawVolume) != 0 && l.VolumeIntensity > 0).Take(12))
                    {
                        var w = scene.GetInstance(l).WorldPosition;
                        Console.WriteLine($"GAMEMLO volume light {scene.OwnerFile(l)?.Name} type {l.Type} at {w.X:0.00},{w.Y:0.00},{w.Z:0.00} intensity {l.Intensity:0.##} volume {l.VolumeIntensity:0.##} size {l.VolumeSizeScale:0.##} falloff {l.Falloff:0.##} cone {l.ConeOuterAngle:0.#} colour {l.ColorR},{l.ColorG},{l.ColorB}");
                    }
                if (wasEmpty) FrameMlo(r, model);
            }
            catch (Exception ex)
            {
                panel.MloStatus = "Import failed: " + ex.Message;
                Console.WriteLine("GAMEMLO import failed: " + ex);
            }
            finally { Cursor = Cursors.Default; }
        }
    }
}
