using System;
using System.Collections.Generic;
using System.Linq;
using CodeWalker.GameFiles;
using RageLightEditor.Editor;
using SharpDX;

namespace RageLightEditor
{
    public partial class MainForm
    {
        public const uint StaticShadowFlag_U46 = 0x80u;
        public const uint DynamicShadowFlag_U46 = 0x100u;
        public const float DynamicOverlap_U46 = 0.75f;

        internal static void ApplyLightPick_U46(Scene sc, int best, bool ctrl, bool shift)
        {
            if (ctrl || shift) sc.ToggleSelect(best);
            else sc.SelectedIndex = best;
        }

        private static float LightScore_U46(LightAttributes l) =>
            l.Type == LightType.Spot ? SpotShadowScore_U45(l) : l.Intensity * l.Falloff * l.Falloff;

        internal static (int dynamic, int staticOnly) SmartDynamicShadows_U46(Scene sc)
        {
            var copies = new List<(int light, int copy, RoomLights_U44 room)>();
            CountRoomLights_U44(sc, copies);
            var roomOf = new Dictionary<(int, int), RoomLights_U44>();
            foreach (var (li, c, room) in copies) roomOf[(li, c)] = room;

            var shadowed = Enumerable.Range(0, sc.Lights.Count)
                .Where(i => sc.Lights[i] != null && RoomLights_U44.CastsShadow(sc.Lights[i]))
                .OrderByDescending(i => LightScore_U46(sc.Lights[i])).ToList();

            var kept = new List<(Vector3 pos, RoomLights_U44 room, float reach)>();
            var give = new HashSet<int>();
            foreach (int i in shadowed)
            {
                var l = sc.Lights[i];
                var owner = sc.OwnerFile(l);
                int n = 1 + (owner?.ExtraPlacements.Count ?? 0);
                var mine = new List<(Vector3 pos, RoomLights_U44 room, float reach)>();
                for (int c = 0; c < n; c++)
                {
                    if (owner != null && owner.HiddenPlacements_U42.Contains(c)) continue;
                    var placement = c == 0
                        ? ((owner != null && owner.HasPlacement) ? owner.Placement : (Matrix?)null)
                        : owner.ExtraPlacements[c - 1];
                    roomOf.TryGetValue((i, c), out var room);
                    mine.Add((sc.GetInstanceAt(l, placement).WorldPosition, room, l.Falloff));
                }
                bool needed = mine.Any(m => !kept.Any(k => ReferenceEquals(k.room, m.room) &&
                    Vector3.Distance(k.pos, m.pos) < DynamicOverlap_U46 * Math.Min(k.reach, m.reach)));
                if (!needed) continue;
                give.Add(i);
                kept.AddRange(mine);
            }

            sc.PushUndo();
            int dyn = 0, stat = 0;
            foreach (int i in shadowed)
            {
                var l = sc.Lights[i];
                uint before = l.Flags;
                if (give.Contains(i)) { l.Flags |= StaticShadowFlag_U46 | DynamicShadowFlag_U46; dyn++; }
                else { l.Flags = (l.Flags & ~DynamicShadowFlag_U46) | StaticShadowFlag_U46; stat++; }
                if (l.Flags != before) { var f = sc.OwnerFile(l); if (f != null) f.Dirty = true; }
            }
            return (dyn, stat);
        }

        private void ServiceDynamicShadows_U46()
        {
            if (panel == null || !panel.RequestDynamicShadows_U46) return;
            panel.RequestDynamicShadows_U46 = false;
            if (scene == null || scene.Lights.Count == 0) return;
            var (dyn, stat) = SmartDynamicShadows_U46(scene);
            panel.FlagsChanged = true;
            shadowRoomsTick_U44 = 0;
            panel.AutoShadowsStatus_U45 = $"Dynamic shadows kept on {dyn} light{(dyn == 1 ? "" : "s")}, one per area. {stat} other shadow light{(stat == 1 ? "" : "s")} now cast static shadows only. Save to keep it, or Undo.";
            Console.WriteLine($"DYNSHADOWS {panel.AutoShadowsStatus_U45}");
        }
    }
}
