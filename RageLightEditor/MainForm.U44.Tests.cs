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
        private void ShadowRoomsTest_U44(Action<string, bool, string> check)
        {
            string dir = Path.Combine(Path.GetTempPath(), "rle_u44_rooms");
            try
            {
                Directory.CreateDirectory(dir);
                var a = Path.Combine(dir, "rle_u44_a.ydr");
                if (!File.Exists(a)) TestSceneGenerator.Run(a);
                var sc = new Scene(modelRenderer, null);
                sc.LoadModelFile(a, additive: false);
                int n = sc.Lights.Count;
                if (n < 2) { check("shadow limit: the test scene has lights", false, n.ToString()); return; }
                var f = sc.Files[0];
                for (int k = 0; k < 40; k++) f.ExtraPlacements.Add(Matrix.Translation(20f, 0f, 0f));
                foreach (var l in sc.Lights) l.Flags = 0x40u;

                var p0 = sc.GetInstance(sc.Lights[0]).WorldPosition;
                var big = new MCMloRoomDef { RoomName = "hall" };
                big._Data.bbMin = new Vector3(-500f); big._Data.bbMax = new Vector3(500f);
                var small = new MCMloRoomDef { RoomName = "office" };
                small._Data.bbMin = p0 - new Vector3(0.05f); small._Data.bbMax = p0 + new Vector3(0.05f);
                var arch = new MloArchetype { rooms = new[] { new MCMloRoomDef { RoomName = "limbo" }, big, small } };
                var info = new MloImportResult();
                info.Interiors.Add(new MloPlacedInterior { Arch = arch, Position = Vector3.Zero, Orientation = Quaternion.Identity });
                sc.SetMloInfo_R1(info);

                int inSmall = sc.Lights.Count(l => Vector3.Distance(sc.GetInstance(l).WorldPosition, p0) < 0.05f);
                var rooms = CountRoomLights_U44(sc);
                var hall = rooms.FirstOrDefault(e => e.Name == "hall");
                var office = rooms.FirstOrDefault(e => e.Name == "office");
                check("shadow limit: every light copy is counted in the room it sits in (smallest room wins)",
                      hall != null && office != null && office.Lights == inSmall && hall.Lights == n * 41 - inSmall,
                      $"hall {hall?.Lights}, office {office?.Lights}, expected {n * 41 - inSmall} / {inSmall}");
                check("shadow limit: a room with more than 32 shadow lights is over the limit",
                      hall != null && hall.Shadow == hall.Lights && hall.Shadow > RoomLights_U44.Limit && office.Shadow <= RoomLights_U44.Limit,
                      $"hall shadows {hall?.Shadow}, office shadows {office?.Shadow}");

                foreach (var l in sc.Lights) l.Flags = 0x200u;
                sc.Lights[0].Flags = 0x80u;
                f.HiddenPlacements_U42.Add(1);
                rooms = CountRoomLights_U44(sc);
                hall = rooms.First(e => e.Name == "hall");
                check("shadow limit: lights without shadow flags and switched-off copies are not counted",
                      hall.Shadow == 39 && hall.Lights == n * 40 - inSmall,
                      $"hall shadows {hall.Shadow} (expected 39), lights {hall.Lights} (expected {n * 40 - inSmall})");
            }
            catch (Exception ex) { check("shadow limit: the test ran", false, ex.ToString()); }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }
    }
}
