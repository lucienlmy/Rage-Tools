using System;
using System.IO;
using System.Linq;
using CodeWalker.GameFiles;
using RageLightEditor.Editor;
using SharpDX;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private void AutoShadowsTest_U45(Action<string, bool, string> check)
        {
            string dir = Path.Combine(Path.GetTempPath(), "rle_u45_auto");
            try
            {
                Directory.CreateDirectory(dir);
                var a = Path.Combine(dir, "rle_u45_a.ydr");
                if (!File.Exists(a)) TestSceneGenerator.Run(a);
                var sc = new Scene(modelRenderer, null);
                sc.LoadModelFile(a, additive: false);
                if (sc.Lights.Count < 3) { check("auto shadows: the test scene has three lights", false, sc.Lights.Count.ToString()); return; }
                var f = sc.Files[0];
                for (int k = 0; k < 14; k++) f.ExtraPlacements.Add(Matrix.Translation(3f * (k + 1), 0f, 0f));
                var l0 = sc.Lights[0]; var l1 = sc.Lights[1]; var l2 = sc.Lights[2];
                foreach (var l in new[] { l0, l1, l2 }) { l.Type = LightType.Spot; l.Falloff = 6f; l.ConeOuterAngle = 40f; l.Flags = 0x200u; }
                l0.Intensity = 8f; l1.Intensity = 4f; l2.Intensity = 2f;
                for (int i = 3; i < sc.Lights.Count; i++) sc.Lights[i].Flags = 0x200u;

                var hall = new MCMloRoomDef { RoomName = "hall" };
                hall._Data.bbMin = new Vector3(-500f); hall._Data.bbMax = new Vector3(500f);
                var arch = new MloArchetype { rooms = new[] { new MCMloRoomDef { RoomName = "limbo" }, hall } };
                var info = new MloImportResult();
                info.Interiors.Add(new MloPlacedInterior { Arch = arch, Position = Vector3.Zero, Orientation = Quaternion.Identity });
                sc.SetMloInfo_R1(info);

                bool Sh(LightAttributes l) => RoomLights_U44.CastsShadow(l);
                var (on, off, over) = AutoSpotShadows_U45(sc);
                var room = CountRoomLights_U44(sc).First(e => e.Name == "hall");
                check("auto shadows: the strongest spot lights get shadows until the room would pass 32",
                      Sh(l0) && Sh(l1) && !Sh(l2) && over == 1 && room.Shadow <= RoomLights_U44.Limit,
                      $"l0 {Sh(l0)}, l1 {Sh(l1)}, l2 {Sh(l2)}, over budget {over}, room shadows {room.Shadow}");
                check("auto shadows: the files are marked unsaved and flags use the game's shadow bits",
                      f.Dirty && (l0.Flags & 0x180u) == 0x180u && (l0.Flags & 0x200u) != 0 && (l2.Flags & RoomLights_U44.ShadowMask) == 0,
                      $"dirty {f.Dirty}, l0 0x{l0.Flags:X}, l2 0x{l2.Flags:X}");

                l1.Falloff = 1f;
                AutoSpotShadows_U45(sc);
                check("auto shadows: a spot that only reaches 1 m loses its shadow and frees room for the next one",
                      Sh(l0) && !Sh(l1) && Sh(l2), $"l0 {Sh(l0)}, l1 {Sh(l1)}, l2 {Sh(l2)}");

                var p = sc.Lights.FirstOrDefault(l => l != null && l.Type != LightType.Spot);
                if (p != null)
                {
                    p.Flags = 0x40u;
                    AutoSpotShadows_U45(sc);
                    check("auto shadows: point and capsule lights keep their own shadow flags", p.Flags == 0x40u, $"0x{p.Flags:X}");
                }

                sc.Undo();
                var u0 = sc.Lights[0];
                check("auto shadows: one Undo puts the flags back", p != null ? sc.Lights.Any(l => l.Type != LightType.Spot && l.Flags == 0x40u) && Sh(u0) : Sh(u0), $"l0 0x{u0.Flags:X}");
            }
            catch (Exception ex) { check("auto shadows: the test ran", false, ex.ToString()); }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }
    }
}
