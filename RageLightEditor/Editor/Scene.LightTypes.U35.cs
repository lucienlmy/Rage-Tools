using CodeWalker.GameFiles;

namespace RageLightEditor.Editor
{
    public partial class Scene
    {
        public static readonly string[] TypeNames_U35 = { "Point", "Spot", "Capsule" };

        public readonly bool[] ShowType_U35 = { true, true, true };

        public static int TypeSlot_U35(LightType t) => t == LightType.Spot ? 1 : t == LightType.Capsule ? 2 : 0;

        public bool TypeShown_U35(LightType t) => ShowType_U35[TypeSlot_U35(t)];

        public int CountType_U35(int slot)
        {
            int n = 0;
            foreach (var l in Lights) if (l != null && (slot < 0 || TypeSlot_U35(l.Type) == slot)) n++;
            return n;
        }

        public int SelectType_U35(int slot)
        {
            SelectedIndices.Clear();
            for (int i = 0; i < Lights.Count; i++)
            {
                var l = Lights[i];
                if (l == null || TypeSlot_U35(l.Type) != slot) continue;
                var f = OwnerFile(l);
                if (f != null && !f.Visible) continue;
                SelectedIndices.Add(i);
            }
            return SelectedIndices.Count;
        }

        public int DeselectType_U35(int slot) =>
            SelectedIndices.RemoveAll(i => i >= 0 && i < Lights.Count && Lights[i] != null && TypeSlot_U35(Lights[i].Type) == slot);

        public int SetFlagsForType_U35(int slot, uint mask, bool on)
        {
            if (mask == 0) return 0;
            PushUndo();
            int changed = 0;
            foreach (var l in Lights)
            {
                if (l == null || (slot >= 0 && TypeSlot_U35(l.Type) != slot)) continue;
                uint before = l.Flags;
                l.Flags = on ? before | mask : before & ~mask;
                if (l.Flags == before) continue;
                changed++;
                var f = OwnerFile(l);
                if (f != null) f.Dirty = true;
            }
            return changed;
        }
    }
}
