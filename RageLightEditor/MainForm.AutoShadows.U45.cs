using System;
using System.Collections.Generic;
using System.Linq;
using CodeWalker.GameFiles;
using RageLightEditor.Editor;

namespace RageLightEditor
{
    public partial class MainForm
    {
        public const float AutoShadowMinReach_U45 = 2.0f;
        public const float AutoShadowMinIntensity_U45 = 0.25f;
        private const uint ShadowOnFlags_U45 = 0x180u;

        internal static bool SpotNeedsShadow_U45(LightAttributes l) =>
            l.Falloff >= AutoShadowMinReach_U45 && l.Intensity >= AutoShadowMinIntensity_U45 && (l.Flags & LightDefs.FlagCoronaOnly) == 0;

        internal static float SpotShadowScore_U45(LightAttributes l)
        {
            float cone = (float)Math.Sin(Math.Min(Math.Max(l.ConeOuterAngle, 1f), 90f) * Math.PI / 180.0);
            return l.Intensity * l.Falloff * l.Falloff * Math.Max(cone, 0.15f);
        }

        internal static (int on, int off, int overBudget) AutoSpotShadows_U45(Scene sc)
        {
            var copies = new List<(int light, RoomLights_U44 room)>();
            CountRoomLights_U44(sc, copies);
            var roomsOf = new Dictionary<int, List<RoomLights_U44>>();
            var used = new Dictionary<RoomLights_U44, int>();
            foreach (var (li, room) in copies)
            {
                if (!roomsOf.TryGetValue(li, out var rl)) roomsOf[li] = rl = new List<RoomLights_U44>();
                rl.Add(room);
                var l = sc.Lights[li];
                if (l.Type != LightType.Spot && RoomLights_U44.CastsShadow(l))
                    used[room] = used.TryGetValue(room, out int u) ? u + 1 : 1;
            }

            var spots = Enumerable.Range(0, sc.Lights.Count).Where(i => sc.Lights[i] != null && sc.Lights[i].Type == LightType.Spot).ToList();
            var give = new HashSet<int>();
            int overBudget = 0;
            foreach (int i in spots.Where(i => SpotNeedsShadow_U45(sc.Lights[i])).OrderByDescending(i => SpotShadowScore_U45(sc.Lights[i])))
            {
                var need = roomsOf.TryGetValue(i, out var rl) ? rl.GroupBy(r => r).Select(g => (room: g.Key, n: g.Count())).ToList() : new List<(RoomLights_U44 room, int n)>();
                if (need.Any(x => (used.TryGetValue(x.room, out int u) ? u : 0) + x.n > RoomLights_U44.Limit)) { overBudget++; continue; }
                foreach (var x in need) used[x.room] = (used.TryGetValue(x.room, out int u) ? u : 0) + x.n;
                give.Add(i);
            }

            sc.PushUndo();
            int on = 0, off = 0;
            foreach (int i in spots)
            {
                var l = sc.Lights[i];
                uint before = l.Flags;
                if (give.Contains(i))
                {
                    if ((l.Flags & RoomLights_U44.ShadowMask) == 0) l.Flags |= ShadowOnFlags_U45;
                    sc.ShadowsDisabled.Remove(l);
                    on++;
                }
                else
                {
                    l.Flags &= ~RoomLights_U44.ShadowMask;
                    sc.ShadowsDisabled.Add(l);
                    off++;
                }
                if (l.Flags != before) { var f = sc.OwnerFile(l); if (f != null) f.Dirty = true; }
            }
            return (on, off, overBudget);
        }

        private bool autoShadowProbeDone_U45;

        private void ServiceAutoShadows_U45()
        {
            if (panel != null && screenshotPath != null && !autoShadowProbeDone_U45 && panel.ShadowRooms_U44.Count > 0 &&
                Environment.GetEnvironmentVariable("RLE_AUTOSHADOWPROBE") == "1")
            {
                autoShadowProbeDone_U45 = true;
                panel.RequestAutoShadows_U45 = true;
                shadowRoomsLogged_U44 = false;
            }
            if (panel == null || !panel.RequestAutoShadows_U45) return;
            panel.RequestAutoShadows_U45 = false;
            if (scene == null || scene.Lights.Count == 0) return;
            var (on, off, over) = AutoSpotShadows_U45(scene);
            panel.FlagsChanged = true;
            shadowRoomsTick_U44 = 0;
            panel.AutoShadowsStatus_U45 = $"Shadows on for {on} spot light{(on == 1 ? "" : "s")}, off for {off}" +
                (over > 0 ? $" ({over} of those would have pushed a room over {RoomLights_U44.Limit})" : "") + ". Save to keep it, or Undo.";
            Console.WriteLine($"AUTOSHADOWS {panel.AutoShadowsStatus_U45}");
        }
    }
}
