using System;
using System.Collections.Generic;
using System.Linq;
using CodeWalker.GameFiles;
using ImGuiNET;
using RageLightEditor.Editor;
using SharpDX;
using NVector2 = System.Numerics.Vector2;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private long shadowRoomsTick_U44;
        private int shadowRoomsGeometry_U44 = -1;
        private bool shadowRoomsLogged_U44;
        internal static int byEntity_U44, byBox_U44;

        internal static List<RoomLights_U44> CountRoomLights_U44(Scene scene) => CountRoomLights_U44(scene, null);

        internal static List<RoomLights_U44> CountRoomLights_U44(Scene scene, List<(int light, int copy, RoomLights_U44 room)> copiesOut)
        {
            var list = new List<RoomLights_U44>();
            var interiors = scene?.MloInfo?.Interiors;
            if (interiors == null || interiors.Count == 0) return list;
            var map = new Dictionary<(MloPlacedInterior, int), RoomLights_U44>();
            foreach (var it in interiors)
            {
                var rooms = it?.Arch?.rooms;
                if (rooms == null) continue;
                for (int r = 1; r < rooms.Length; r++)
                {
                    if (rooms[r] == null) continue;
                    var e = new RoomLights_U44 { Interior = it.Name, Room = r, Name = string.IsNullOrEmpty(rooms[r].RoomName) ? $"room {r}" : rooms[r].RoomName };
                    map[(it, r)] = e;
                    list.Add(e);
                }
            }
            byEntity_U44 = byBox_U44 = 0;
            var byPos = new Dictionary<(long, long, long), List<MloEntityInfo>>();
            foreach (var en in scene.RoomEntities_U44)
            {
                if (en?.Interior_U44 == null || en.Room_U44 < 0) continue;
                var k = PosKey_U44(en.Position);
                if (!byPos.TryGetValue(k, out var bucket)) byPos[k] = bucket = new List<MloEntityInfo>();
                bucket.Add(en);
                if (en.Room_U44 > 0 && map.TryGetValue((en.Interior_U44, en.Room_U44), out var owned) && !RoomHasBox_U44(en.Interior_U44, en.Room_U44))
                {
                    var pb = new BoundingBox(en.Position - new Vector3(0.5f), en.Position + new Vector3(0.5f));
                    owned.Box = owned.HasBox ? BoundingBox.Merge(owned.Box, pb) : pb;
                    owned.HasBox = true;
                }
            }
            for (int i = 0; i < scene.Lights.Count; i++)
            {
                var l = scene.Lights[i];
                if (l == null) continue;
                var owner = scene.OwnerFile(l);
                int copies = 1 + (owner?.ExtraPlacements.Count ?? 0);
                for (int c = 0; c < copies; c++)
                {
                    if (owner != null && owner.HiddenPlacements_U42.Contains(c)) continue;
                    var placement = c == 0
                        ? ((owner != null && owner.HasPlacement) ? owner.Placement : (Matrix?)null)
                        : owner.ExtraPlacements[c - 1];
                    RoomLights_U44 e = null;
                    var ent = placement.HasValue ? EntityAt_U44(byPos, placement.Value.TranslationVector, owner?.Path == null ? null : System.IO.Path.GetFileNameWithoutExtension(owner.Path)) : null;
                    if (ent != null)
                    {
                        if (ent.Room_U44 <= 0 || !map.TryGetValue((ent.Interior_U44, ent.Room_U44), out e)) continue;
                        byEntity_U44++;
                    }
                    else
                    {
                        var p = scene.GetInstanceAt(l, placement).WorldPosition;
                        if (FindCameraRoom_K2(interiors, p, out var which, out int ri) == null) continue;
                        if (!map.TryGetValue((which, ri), out e)) continue;
                        byBox_U44++;
                    }
                    e.Lights++;
                    if (RoomLights_U44.CastsShadow(l)) e.Shadow++;
                    copiesOut?.Add((i, c, e));
                }
            }
            return list;
        }

        private static bool RoomHasBox_U44(MloPlacedInterior it, int r)
        {
            var rm = it?.Arch?.rooms != null && r < it.Arch.rooms.Length ? it.Arch.rooms[r] : null;
            if (rm == null) return false;
            var mn = rm._Data.bbMin; var mx = rm._Data.bbMax;
            return mx.X > mn.X && mx.Y > mn.Y && mx.Z > mn.Z;
        }

        internal static RoomLights_U44 RoomAt_U44(List<RoomLights_U44> rooms, IReadOnlyList<MloPlacedInterior> interiors, Vector3? pos)
        {
            if (pos == null || rooms == null || interiors == null) return null;
            var p = pos.Value;
            RoomLights_U44 here = null;
            float best = float.MaxValue;
            foreach (var e in rooms)
            {
                float vol;
                if (e.HasBox)
                {
                    if (e.Box.Contains(p) == ContainmentType.Disjoint) continue;
                    var d = e.Box.Maximum - e.Box.Minimum;
                    vol = d.X * d.Y * d.Z;
                }
                else
                {
                    var it = interiors.FirstOrDefault(i => i?.Name == e.Interior && RoomHasBox_U44(i, e.Room) && Inside_U44(i, e.Room, p));
                    if (it == null) continue;
                    var rm = it.Arch.rooms[e.Room]._Data;
                    var d = rm.bbMax - rm.bbMin;
                    vol = d.X * d.Y * d.Z;
                }
                if (vol < best) { best = vol; here = e; }
            }
            return here;
        }

        private static bool Inside_U44(MloPlacedInterior it, int r, Vector3 world)
        {
            var rm = it.Arch.rooms[r]._Data;
            var l = it.ToLocal(world);
            return l.X >= rm.bbMin.X && l.Y >= rm.bbMin.Y && l.Z >= rm.bbMin.Z && l.X <= rm.bbMax.X && l.Y <= rm.bbMax.Y && l.Z <= rm.bbMax.Z;
        }

        private static (long, long, long) PosKey_U44(Vector3 p) =>
            ((long)Math.Round(p.X * 100f), (long)Math.Round(p.Y * 100f), (long)Math.Round(p.Z * 100f));

        private static MloEntityInfo EntityAt_U44(Dictionary<(long, long, long), List<MloEntityInfo>> byPos, Vector3 p, string name)
        {
            if (!byPos.TryGetValue(PosKey_U44(p), out var bucket)) return null;
            foreach (var en in bucket)
                if (string.Equals(en.ArchetypeName, name, StringComparison.OrdinalIgnoreCase)) return en;
            return bucket.Count == 1 ? bucket[0] : null;
        }

        private void UpdateShadowRooms_U44()
        {
            if (panel == null || scene == null) return;
            var interiors = scene.MloInfo?.Interiors;
            if (interiors == null || interiors.Count == 0 || scene.Lights.Count == 0)
            {
                panel.ShadowRooms_U44.Clear();
                panel.CameraShadowRoom_U44 = null;
                return;
            }
            long now = Environment.TickCount64;
            if (now - shadowRoomsTick_U44 >= 500 || shadowRoomsGeometry_U44 != scene.GeometryVersion)
            {
                shadowRoomsTick_U44 = now;
                shadowRoomsGeometry_U44 = scene.GeometryVersion;
                panel.ShadowRooms_U44 = CountRoomLights_U44(scene);
                if (screenshotPath != null && !shadowRoomsLogged_U44 && panel.ShadowRooms_U44.Count > 0)
                {
                    shadowRoomsLogged_U44 = true;
                    Console.WriteLine($"SHADOWROOMS light copies placed by their entity's room {byEntity_U44}, by room box {byBox_U44}");
                    foreach (var e in panel.ShadowRooms_U44.OrderByDescending(e => e.Shadow))
                        Console.WriteLine($"SHADOWROOMS {e.Interior} / {e.Name}: lights {e.Lights}, shadow {e.Shadow}{(e.Shadow > RoomLights_U44.Limit ? "  OVER" : "")}{(e.HasBox ? $"  (box from its props, centre {e.Box.Center.X:0.0},{e.Box.Center.Y:0.0},{e.Box.Center.Z:0.0})" : "")}");
                }
            }
            var here = RoomAt_U44(panel.ShadowRooms_U44, interiors, camera?.Position);
            if (screenshotPath != null && !ReferenceEquals(here, panel.CameraShadowRoom_U44) && here != null)
                Console.WriteLine($"SHADOWROOMS camera room: {here.Name} {here.Shadow}/{RoomLights_U44.Limit}");
            panel.CameraShadowRoom_U44 = here;
        }

        private void DrawShadowLimitBadge_U44(System.Numerics.Vector4 r)
        {
            var e = panel.CameraShadowRoom_U44;
            if (e == null || panel.WorldMode || panel.MloMode || panel.MaterialMode) return;
            bool over = e.Shadow > RoomLights_U44.Limit;
            string text = over
                ? $"{e.Name}: {e.Shadow} shadow lights - over the GTA V limit of {RoomLights_U44.Limit}"
                : $"{e.Name}: {e.Shadow}/{RoomLights_U44.Limit} shadow lights";
            var dl = ImGui.GetBackgroundDrawList();
            var ts = ImGui.CalcTextSize(text);
            var pos = new NVector2((r.X + r.Z - ts.X) * 0.5f, r.Y + 34);
            uint bg = over ? 0xD02828B4u : 0xA0000000u;
            uint fg = over ? 0xFFFFFFFFu : 0xFFD6D6D6u;
            dl.AddRectFilled(pos - new NVector2(10, 5), pos + ts + new NVector2(10, 5), bg, 4f);
            dl.AddText(pos, fg, text);
        }
    }
}
