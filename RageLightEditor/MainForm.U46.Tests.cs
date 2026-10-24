using System;
using System.IO;
using CodeWalker.GameFiles;
using RageLightEditor.Editor;
using SharpDX;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private void DynamicShadowsTest_U46(Action<string, bool, string> check)
        {
            string dir = Path.Combine(Path.GetTempPath(), "rle_u46_dyn");
            try
            {
                Directory.CreateDirectory(dir);
                var a = Path.Combine(dir, "rle_u46_a.ydr");
                if (!File.Exists(a)) TestSceneGenerator.Run(a);
                var sc = new Scene(modelRenderer, null);
                sc.LoadModelFile(a, additive: false);
                if (sc.Lights.Count < 3) { check("dynamic shadows: the test scene has three lights", false, sc.Lights.Count.ToString()); return; }
                var l0 = sc.Lights[0]; var l1 = sc.Lights[1]; var l2 = sc.Lights[2];
                for (int i = 3; i < sc.Lights.Count; i++) sc.Lights[i].Flags = 0x200u;
                l0.Position = new Vector3(0, 0, 3); l1.Position = new Vector3(1, 0, 3); l2.Position = new Vector3(10, 0, 3);
                foreach (var l in new[] { l0, l1, l2 }) { l.Type = LightType.Spot; l.Falloff = 6f; l.ConeOuterAngle = 40f; l.Flags = 0x180u; l.BoneId = 0; }
                l0.Intensity = 8f; l1.Intensity = 4f; l2.Intensity = 2f;

                bool Dyn(LightAttributes l) => (l.Flags & DynamicShadowFlag_U46) != 0;
                bool Stat(LightAttributes l) => (l.Flags & StaticShadowFlag_U46) != 0;
                var (dyn, stat) = SmartDynamicShadows_U46(sc);
                check("dynamic shadows: of two lights lighting the same spot only the stronger keeps dynamic shadows",
                      Dyn(l0) && !Dyn(l1) && Stat(l1) && dyn == 2 && stat == 1, $"l0 0x{l0.Flags:X}, l1 0x{l1.Flags:X}, l2 0x{l2.Flags:X}, dyn {dyn}, static {stat}");
                check("dynamic shadows: a light far from the others keeps its own dynamic shadows", Dyn(l2), $"l2 0x{l2.Flags:X}");

                var hall = new MCMloRoomDef { RoomName = "hall" };
                hall._Data.bbMin = new Vector3(-500f); hall._Data.bbMax = new Vector3(500f);
                var office = new MCMloRoomDef { RoomName = "office" };
                office._Data.bbMin = new Vector3(0.6f, -1f, 2f); office._Data.bbMax = new Vector3(1.4f, 1f, 4f);
                var arch = new MloArchetype { rooms = new[] { new MCMloRoomDef { RoomName = "limbo" }, hall, office } };
                var info = new MloImportResult();
                info.Interiors.Add(new MloPlacedInterior { Arch = arch, Position = Vector3.Zero, Orientation = Quaternion.Identity });
                sc.SetMloInfo_R1(info);
                SmartDynamicShadows_U46(sc);
                check("dynamic shadows: a light behind a wall in another room is not covered by its neighbour", Dyn(l0) && Dyn(l1), $"l0 0x{l0.Flags:X}, l1 0x{l1.Flags:X}");

                sc.SelectedIndex = 0;
                ApplyLightPick_U46(sc, 1, false, true);
                bool added = sc.IsSelected(0) && sc.IsSelected(1);
                ApplyLightPick_U46(sc, 1, false, true);
                check("pick: Shift + right-click adds a light, and on a selected light takes it out again",
                      added && sc.IsSelected(0) && !sc.IsSelected(1), $"added {added}, now {string.Join(",", sc.SelectedIndices)}");
            }
            catch (Exception ex) { check("dynamic shadows: the test ran", false, ex.ToString()); }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }
    }
}
