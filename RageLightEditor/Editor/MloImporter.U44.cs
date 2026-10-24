using System.Collections.Generic;
using CodeWalker.GameFiles;
using SharpDX;

namespace RageLightEditor.Editor
{
    public partial class MloEntityInfo
    {
        public MloPlacedInterior Interior_U44;
        public int Room_U44 = -1;
    }

    public partial class MloImportResult
    {
        private readonly Dictionary<MloArchetype, Dictionary<MCEntityDef, int>> roomsByEntity_U44 = new Dictionary<MloArchetype, Dictionary<MCEntityDef, int>>();

        internal void NoteRoom_U44(MloEntityInfo info, MloArchetype mlo, MCEntityDef ent, int setRoom, Vector3 instPos)
        {
            if (mlo == null || info == null) return;
            foreach (var i in Interiors)
                if (ReferenceEquals(i.Arch, mlo) && (i.Position - instPos).LengthSquared() < 1e-6f) { info.Interior_U44 = i; break; }
            if (setRoom >= 0) { info.Room_U44 = setRoom; return; }
            if (!roomsByEntity_U44.TryGetValue(mlo, out var map))
            {
                map = new Dictionary<MCEntityDef, int>();
                var rooms = mlo.rooms;
                var ents = mlo.entities;
                if (rooms != null && ents != null)
                    for (int r = 0; r < rooms.Length; r++)
                    {
                        var att = rooms[r]?.AttachedObjects;
                        if (att == null) continue;
                        foreach (var o in att)
                            if (o < ents.Length && ents[o] != null && !map.ContainsKey(ents[o])) map[ents[o]] = r;
                    }
                roomsByEntity_U44[mlo] = map;
            }
            if (map.TryGetValue(ent, out int room)) info.Room_U44 = room;
        }
    }
}
