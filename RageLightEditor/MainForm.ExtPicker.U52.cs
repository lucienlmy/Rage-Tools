using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CodeWalker.GameFiles;
using RageLightEditor.Editor;
using RageLightEditor.Rendering;
using SharpDX;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private struct ExtPickHit_U52
        {
            public bool Valid;
            public string Name;
            public uint Hash;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Point;
            public List<RenderMesh> Meshes;
        }

        private Dictionary<RenderMesh, MloEntityInfo> extMeshOwner_U52;
        private int extMeshOwnerVersion_U52 = -1, extMeshOwnerCount_U52 = -1;
        private ExtPickHit_U52 extHover_U52, extPicked_U52;
        private readonly List<RenderMesh> extLit_U52 = new List<RenderMesh>();
        private System.Drawing.Point extHoverAt_U52 = new System.Drawing.Point(-1, -1);
        private Vector3 extHoverCam_U52;
        private static readonly string extPickProbe_U52 = Environment.GetEnvironmentVariable("RLE_EXTPICKAT");
        private bool extPickProbeDone_U52;

        private Dictionary<RenderMesh, MloEntityInfo> ExtMeshOwners_U52()
        {
            if (extMeshOwner_U52 != null && extMeshOwnerVersion_U52 == scene.GeometryVersion &&
                extMeshOwnerCount_U52 == scene.RoomEntities_U44.Count) return extMeshOwner_U52;
            var map = new Dictionary<RenderMesh, MloEntityInfo>();
            foreach (var e in scene.RoomEntities_U44)
                if (e != null) foreach (var m in e.PlacedMeshes_O3) if (m != null) map[m] = e;
            extMeshOwner_U52 = map;
            extMeshOwnerVersion_U52 = scene.GeometryVersion;
            extMeshOwnerCount_U52 = scene.RoomEntities_U44.Count;
            return map;
        }

        private static ExtPickHit_U52 ExtHitFromEntity_U52(MloEntityInfo e, Vector3 point) => new ExtPickHit_U52
        {
            Valid = true,
            Name = e.ArchetypeName ?? e.ArchetypeHash.ToString(),
            Hash = e.ArchetypeHash,
            Position = e.Position,
            Rotation = e.Rotation,
            Point = point,
            Meshes = e.PlacedMeshes_O3,
        };

        private ExtPickHit_U52 ExtPickUnder_U52(Ray ray)
        {
            RenderMesh best = null;
            float bestD = float.MaxValue;
            foreach (var m in scene.AllMeshes)
            {
                if (m == null || !m.Visible || m.NeverDraw) continue;
                var b = m.WorldBounds;
                if (b.Maximum.X <= b.Minimum.X) continue;
                if (!ray.Intersects(ref b, out float bt) || bt > bestD) continue;
                if (m.RayHit(ref ray, out float t) && t < bestD) { bestD = t; best = m; }
            }
            if (best == null) return default;
            var point = ray.Position + ray.Direction * bestD;

            if (ExtMeshOwners_U52().TryGetValue(best, out var ent)) return ExtHitFromEntity_U52(ent, point);

            var file = scene.Files.FirstOrDefault(f => f?.Model != null && f.Model.Meshes.Contains(best));
            if (file != null)
            {
                var stem = Path.GetFileNameWithoutExtension(file.Path ?? file.Name ?? "");
                var hit = new ExtPickHit_U52
                {
                    Valid = true, Name = stem, Hash = JenkHash.GenHash(stem.ToLowerInvariant()),
                    Position = Vector3.Zero, Rotation = Quaternion.Identity, Point = point, Meshes = file.Model.Meshes,
                };
                if (file.HasPlacement && file.Placement.Decompose(out _, out var rot, out var pos))
                { hit.Position = pos; hit.Rotation = rot; }
                return hit;
            }

            if (best.IsMloShell && scene.MloInfo != null && scene.MloInfo.Interiors.Count > 0)
            {
                var i = scene.MloInfo.Interiors.OrderBy(x => (x.Position - point).LengthSquared()).First();
                return new ExtPickHit_U52
                {
                    Valid = true, Name = i.Name, Hash = i.Arch?.Hash ?? 0, Position = i.Position, Rotation = i.Orientation,
                    Point = point, Meshes = scene.MloModel?.Meshes.Where(x => x.IsMloShell).ToList(),
                };
            }
            return default;
        }

        private Archetype ExtResolveArchetype_U52(uint hash)
        {
            var ws = panel.Ext;
            var have = ws.Targets.FirstOrDefault(x => x.Archetype != null && x.Archetype.Hash == hash);
            if (have != null) return have.Archetype;
            foreach (var s in ws.Sources)
            {
                var a = s.Ytyp?.AllArchetypes?.FirstOrDefault(x => x != null && x.Hash == hash);
                if (a != null) return a;
            }
            var from = scene.MloInfo?.Ytyps.FirstOrDefault(y => y.Archetypes.Any(a => a != null && a.Hash == hash));
            if (from != null && !string.IsNullOrEmpty(from.Path) && File.Exists(from.Path) &&
                !ws.Sources.Any(s => string.Equals(s.Path, from.Path, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    var src = ws.AddYtyp(from.Path);
                    var a = src.Ytyp?.AllArchetypes?.FirstOrDefault(x => x != null && x.Hash == hash);
                    if (a != null) return a;
                }
                catch (Exception ex) { AppLog_U21.Error("Extensions: opening " + from.Path, ex); }
            }
            return gameFiles?.Cache?.GetArchetype(hash);
        }

        private int ExtNearestProp_U52(uint hash, Vector3 world)
        {
            var ws = panel.Ext;
            var interior = scene.MloInfo?.Interiors.FirstOrDefault();
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < ws.Props.Count; i++)
            {
                var p = ws.Props[i];
                if (p.ArchHash != hash) continue;
                float d = (p.Position - world).LengthSquared();
                if (interior != null) d = Math.Min(d, (p.Position - interior.ToLocal(world)).LengthSquared());
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        private bool ExtUsePick_U52(ExtPickHit_U52 hit)
        {
            var ws = panel.Ext;
            var arch = ExtResolveArchetype_U52(hit.Hash);
            if (arch == null)
            {
                ws.Say($"{hit.Name}: its archetype is not in the game or in the .ytyp the scene came from", true);
                return false;
            }
            var t = ws.Targets.FirstOrDefault(x => x.Archetype != null && x.Archetype.Hash == hit.Hash) ?? ws.AddGameArchetype(arch);
            ws.Selected = ws.Targets.IndexOf(t);
            ws.SelectedExtension = -1;
            ws.SelectedPointField = null;
            ws.CancelSnap();
            t.Placement = hit.Position;
            t.Orientation = hit.Rotation;
            panel.ExtShownFor_V68 = ws.Selected;
            extPicked_U52 = hit;
            int prop = ExtNearestProp_U52(hit.Hash, hit.Position);
            if (prop >= 0) ws.SelectedProp = prop;
            panel.ExtScrollToPick_U52 = true;
            int n = ArchetypeExtensions_V62.Get(t.Archetype).Length;
            ws.Say($"{t.Name} picked - {n} extension(s) on its archetype" +
                   (t.Editable ? "" : ". It comes from the game: Save as... writes your own .ytyp"));
            return true;
        }

        private void ExtPickClick_U52(int x, int y)
        {
            var hit = ExtPickUnder_U52(camera.GetPickRay(x, y, deviceResources.Width, deviceResources.Height));
            if (!hit.Valid) { panel.Ext.Say("nothing under the cursor - click an object of the scene", true); return; }
            if (ExtUsePick_U52(hit)) panel.ExtPicking_U52 = false;
        }

        private void ExtHoverPick_U52()
        {
            System.Drawing.Point at = lastMouse;
            bool probe = false;
            if (!string.IsNullOrEmpty(extPickProbe_U52) && !extPickProbeDone_U52 && scene.RoomEntities_U44.Count > 0)
            {
                var bits = extPickProbe_U52.Split(',');
                if (bits.Length >= 2 &&
                    float.TryParse(bits[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var fx) &&
                    float.TryParse(bits[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var fy))
                {
                    at = new System.Drawing.Point((int)(fx * deviceResources.Width), (int)(fy * deviceResources.Height));
                    panel.ExtPicking_U52 = true;
                    probe = true;
                    if (screenshotPath != null) screenshotFrames = -1;
                }
            }
            if (!panel.ExtPicking_U52)
            {
                extHover_U52 = default;
                panel.ExtHoverName_U52 = null;
                return;
            }
            if (probe || at != extHoverAt_U52 || camera.Position != extHoverCam_U52)
            {
                extHoverAt_U52 = at;
                extHoverCam_U52 = camera.Position;
                extHover_U52 = !probe && ImGuiWantsMouse ? default
                    : ExtPickUnder_U52(camera.GetPickRay(at.X, at.Y, deviceResources.Width, deviceResources.Height));
                panel.ExtHoverName_U52 = extHover_U52.Valid ? extHover_U52.Name : null;
            }
            if (extHover_U52.Valid)
                DrawWorldLabel(extHover_U52.Point, extHover_U52.Name + "   click to use it", new Vector4(0.3f, 0.95f, 1f, 1f));
            if (probe && ++extPickProbeFrames_U52 >= 30)
            {
                extPickProbeDone_U52 = true;
                var h = extHover_U52;
                Console.WriteLine(h.Valid
                    ? $"EXTPICK hover {h.Name} hash {h.Hash} at {h.Position.X:0.00},{h.Position.Y:0.00},{h.Position.Z:0.00} meshes {h.Meshes?.Count ?? 0}"
                    : "EXTPICK hover nothing");
                if (h.Valid && Environment.GetEnvironmentVariable("RLE_EXTPICKCLICK") == "1")
                {
                    bool ok = ExtUsePick_U52(h);
                    panel.ExtPicking_U52 = !ok;
                    var cur = panel.Ext.Current;
                    Console.WriteLine($"EXTPICK used {ok} selected {cur?.Name ?? "none"} editable {cur?.Editable} placement {cur?.Placement.X:0.00},{cur?.Placement.Y:0.00},{cur?.Placement.Z:0.00} " +
                                      $"prop {(panel.Ext.SelectedProp >= 0 ? panel.Ext.Props[panel.Ext.SelectedProp].Name : "none")} sources {panel.Ext.Sources.Count} status \"{panel.Ext.Status}\"");
                }
                if (screenshotPath != null) screenshotFrames = 8;
            }
        }

        private int extPickProbeFrames_U52;
        private bool extWindScanDone_U52;

        private void ExtShaftWindScan_U52()
        {
            if (extWindScanDone_U52 || Environment.GetEnvironmentVariable("RLE_SHAFTWIND") != "1" ||
                gameFiles == null || !gameFiles.Ready) return;
            extWindScanDone_U52 = true;
            int total = 0, against = 0, flat = 0, noDir = 0, both = 0;
            var samples = new List<string>();
            foreach (var ytyp in gameFiles.Cache.YtypDict.Values)
            {
                if (ytyp?.AllArchetypes == null) continue;
                foreach (var a in ytyp.AllArchetypes)
                    foreach (var w in ArchetypeExtensions_V62.Get(a))
                    {
                        if (!(w is MCExtensionDefLightShaft ls)) continue;
                        var d = ls._Data;
                        total++;
                        if ((d.flags & ShaftWinding_U52.FlagDrawFrontAndBehind) != 0) both++;
                        if (!ShaftWinding_U52.HasArea(d)) { flat++; continue; }
                        if (d.direction.LengthSquared() < 1e-8f) { noDir++; continue; }
                        if (ShaftWinding_U52.AgainstDirection(d)) { against++; if (samples.Count < 6) samples.Add(a.Name); }
                    }
            }
            Console.WriteLine($"SHAFTWIND {total} game shafts: {against} wound against their direction, {flat} flat, {noDir} no direction, {both} with flag 64" +
                              (samples.Count > 0 ? " e.g. " + string.Join(", ", samples) : ""));
        }

        partial void ExtHighlights_U52()
        {
            foreach (var m in extLit_U52) if (m != null) m.Highlight = 0;
            extLit_U52.Clear();
            if (!ExtActive_V68 || photoMode) return;
            void Lit(List<RenderMesh> ms, uint mode)
            {
                if (ms == null) return;
                foreach (var m in ms) if (m != null) { m.Highlight = mode; extLit_U52.Add(m); }
            }
            if (extPicked_U52.Valid && panel.Ext.Current?.Archetype?.Hash == extPicked_U52.Hash) Lit(extPicked_U52.Meshes, 4);
            if (panel.ExtPicking_U52 && extHover_U52.Valid) Lit(extHover_U52.Meshes, 2);
        }

        private bool ExtFocusInScene_U52(ExtensionTarget_V68 t)
        {
            if (t?.Archetype == null) return false;
            uint h = t.Archetype.Hash;
            if (t.Archetype is MloArchetype)
            {
                var i = scene.MloInfo?.Interiors.FirstOrDefault(x => x.Arch?.Hash == h);
                if (i == null) return false;
                t.Placement = i.Position;
                t.Orientation = i.Orientation;
                extPicked_U52 = default;
                panel.Ext.Say($"{t.Name}: the interior itself - its extensions sit from the interior's origin");
                return true;
            }
            var ents = scene.RoomEntities_U44.Where(e => e != null && e.ArchetypeHash == h && e.HasPlacedMeshes_O3).ToList();
            if (ents.Count == 0) return false;
            var ws = panel.Ext;
            var pick = ents[0];
            if (ws.SelectedProp >= 0 && ws.SelectedProp < ws.Props.Count && ws.Props[ws.SelectedProp].ArchHash == h)
            {
                var want = ws.Props[ws.SelectedProp].Position;
                var interior = scene.MloInfo?.Interiors.FirstOrDefault();
                pick = ents.OrderBy(e => Math.Min((e.Position - want).LengthSquared(),
                    interior != null ? (interior.ToLocal(e.Position) - want).LengthSquared() : float.MaxValue)).First();
            }
            t.Placement = pick.Position;
            t.Orientation = pick.Rotation;
            extPicked_U52 = ExtHitFromEntity_U52(pick, pick.Position);
            ExtFramePicked_U52();
            ws.Say($"{t.Name} - shown where it sits in the scene" + (ents.Count > 1 ? $" (1 of {ents.Count} placed)" : ""));
            return true;
        }

        private bool ExtFramePicked_U52()
        {
            if (!extPicked_U52.Valid || extPicked_U52.Meshes == null) return false;
            if (panel.Ext.Current?.Archetype?.Hash != extPicked_U52.Hash) return false;
            var min = new Vector3(float.MaxValue); var max = new Vector3(float.MinValue);
            bool any = false;
            foreach (var m in extPicked_U52.Meshes)
            {
                if (m == null) continue;
                var b = m.WorldBounds;
                if (b.Maximum.X <= b.Minimum.X) continue;
                min = Vector3.Min(min, b.Minimum); max = Vector3.Max(max, b.Maximum); any = true;
            }
            if (!any) return false;
            camera.Target = (min + max) * 0.5f;
            camera.Distance = Math.Clamp((max - min).Length() * 1.6f, 1.0f, 12.0f);
            camera.SnapSmoothing();
            return true;
        }

        private void ExtAfterCornerSnap_U52(Archetype arch, int extIndex, string field, Vector3 cameraLocal)
        {
            if (arch == null || panel.Ext.SnapArmed || string.IsNullOrEmpty(field) ||
                !field.StartsWith("corner", StringComparison.OrdinalIgnoreCase)) return;
            var list = ArchetypeExtensions_V62.Get(arch);
            if (extIndex < 0 || extIndex >= list.Length || !(list[extIndex] is MCExtensionDefLightShaft ls)) return;
            if (ShaftWinding_U52.FaceTowards(ls, cameraLocal))
                panel.Ext.Say("corners set - turned round to B and D swapped so the game draws it from where you stand");
        }

        private void ExtPickerTest_U52(Action<string, bool, string> check)
        {
            ShaftWinding_U52.SelfTest(check);
            var savedEnts = scene.RoomEntities_U44.ToList();
            var ws = panel.Ext;
            int selWas = ws.Selected;
            try
            {
                RenderMesh Quad(Matrix world)
                {
                    var m = new RenderMesh
                    {
                        PickVerts = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, 0.5f) },
                        PickIndices = new ushort[] { 0, 1, 2, 0, 2, 3 },
                        LocalBounds = new BoundingBox(new Vector3(-0.5f, -0.01f, -0.5f), new Vector3(0.5f, 0.01f, 0.5f)),
                        OwnsBuffers = false,
                    };
                    return m.CreateInstance(world);
                }
                var near = new MloEntityInfo { ArchetypeName = "rle_u52_window", ArchetypeHash = JenkHash.GenHash("rle_u52_window"), Position = new Vector3(3, 5, 1), Rotation = Quaternion.RotationAxis(Vector3.UnitZ, 0.5f), Scale = Vector3.One };
                var far = new MloEntityInfo { ArchetypeName = "rle_u52_wall", ArchetypeHash = JenkHash.GenHash("rle_u52_wall"), Position = new Vector3(3, 9, 1), Rotation = Quaternion.Identity, Scale = Vector3.One };
                var mNear = Quad(Matrix.Transformation(Vector3.Zero, Quaternion.Identity, Vector3.One, Vector3.Zero, near.Rotation, near.Position));
                var mFar = Quad(Matrix.Transformation(Vector3.Zero, Quaternion.Identity, Vector3.One, Vector3.Zero, far.Rotation, far.Position));
                var model = new RenderModel();
                model.Meshes.Add(mNear); model.Meshes.Add(mFar);
                near.TrackPlacedMeshes_O3(new RenderModel { Meshes = { mNear } }, 1, Matrix.Identity);
                far.TrackPlacedMeshes_O3(new RenderModel { Meshes = { mFar } }, 1, Matrix.Identity);
                var oldMlo = scene.MloModel; var oldInfo = scene.MloInfo; var oldVisible = scene.MloVisible;
                scene.MloModel = model; scene.MloVisible = true;
                scene.RoomEntities_U44.Clear();
                scene.RoomEntities_U44.Add(near); scene.RoomEntities_U44.Add(far);
                scene.GeometryChanged_V16();
                try
                {
                    var ray = new Ray(new Vector3(3, 0, 1), Vector3.UnitY);
                    var hit = ExtPickUnder_U52(ray);
                    check("extension picker: clicking the scene names the object in front, not the one behind it",
                          hit.Valid && hit.Name == "rle_u52_window" && hit.Meshes != null && hit.Meshes.Contains(mNear),
                          hit.Valid ? hit.Name : "nothing");
                    check("extension picker: ...and knows exactly where that object sits",
                          hit.Valid && (hit.Position - near.Position).Length() < 1e-4f && Math.Abs(Quaternion.Dot(hit.Rotation, near.Rotation)) > 0.9999f,
                          hit.Position.ToString());
                    var miss = ExtPickUnder_U52(new Ray(new Vector3(30, 0, 1), Vector3.UnitY));
                    check("extension picker: empty space picks nothing", !miss.Valid, miss.Name ?? "");

                    var arch = new Archetype();
                    var bd = arch._BaseArchetypeDef;
                    bd.name = new MetaHash(near.ArchetypeHash);
                    arch.Init(null, ref bd);
                    arch._BaseArchetypeDef = bd;
                    var target = ws.AddGameArchetype(arch);
                    ws.Selected = -1;
                    bool used = ExtUsePick_U52(hit);
                    check("extension picker: using the pick selects that archetype and puts its extensions where the object is",
                          used && ReferenceEquals(ws.Current, target) && (target.Placement - near.Position).Length() < 1e-4f &&
                          panel.ExtShownFor_V68 == ws.Selected,
                          $"used {used}, selected {ws.Current?.Name}, placement {target.Placement}");
                    ws.Targets.Remove(target);
                }
                finally
                {
                    scene.MloModel = oldMlo; scene.MloInfo = oldInfo; scene.MloVisible = oldVisible;
                    extPicked_U52 = default;
                }
            }
            catch (Exception ex) { check("extension picker: the test ran", false, ex.ToString()); }
            finally
            {
                scene.RoomEntities_U44.Clear();
                scene.RoomEntities_U44.AddRange(savedEnts);
                scene.GeometryChanged_V16();
                ws.Selected = selWas < ws.Targets.Count ? selWas : -1;
            }
        }
    }
}
