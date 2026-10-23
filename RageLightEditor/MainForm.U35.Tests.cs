using System;
using System.IO;
using System.Linq;
using CodeWalker.GameFiles;
using RageLightEditor.Editor;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private void LightTypesTest_U35(Action<string, bool, string> check)
        {
            string dir = Path.Combine(Path.GetTempPath(), "rle_u35_types");
            try
            {
                Directory.CreateDirectory(dir);
                var a = Path.Combine(dir, "rle_u35_a.ydr");
                var b = Path.Combine(dir, "rle_u35_b.ydr");
                if (!File.Exists(a)) TestSceneGenerator.Run(a);
                if (!File.Exists(b)) TestSceneGenerator.Run(b);
                var sc = new Scene(modelRenderer, null);
                sc.LoadModelFile(a, additive: false);
                sc.LoadModelFile(b, additive: true);
                sc.Lights[0].Type = LightType.Spot;
                sc.Lights[1].Type = LightType.Point;
                sc.Lights[sc.Lights.Count - 1].Type = LightType.Spot;
                foreach (var f in sc.Files) f.Dirty = false;

                int spots = sc.CountType_U35(1), all = sc.CountType_U35(-1);
                check("light types: lights are counted per type", spots >= 2 && all == sc.Lights.Count && spots < all, $"{spots} spot of {all}");

                var gpu = new Rendering.GpuLight[1024];
                int lit = sc.BuildGpuLights(gpu, 12, false, 0f, null, SharpDX.Vector3.Zero);
                sc.ShowType_U35[1] = false;
                int litNoSpots = sc.BuildGpuLights(gpu, 12, false, 0f, null, SharpDX.Vector3.Zero);
                sc.ShowType_U35[1] = true;
                check("light types: hiding spots stops them lighting the viewport", litNoSpots < lit && litNoSpots > 0, $"{lit} -> {litNoSpots}");

                const uint shadows = 1u << 6;
                foreach (var l in sc.Lights) l.Flags &= ~shadows;
                int n = sc.SetFlagsForType_U35(1, shadows, true);
                bool spotsOn = sc.Lights.Where(l => l.Type == LightType.Spot).All(l => (l.Flags & shadows) != 0);
                bool othersOff = sc.Lights.Where(l => l.Type != LightType.Spot).All(l => (l.Flags & shadows) == 0);
                check("light types: a flag turns on for every spot light and nothing else", n == spots && spotsOn && othersOff, $"{n} changed");
                check("light types: ...and the props they live on are marked unsaved", sc.Files.All(f => f.Dirty), string.Join(",", sc.Files.Select(f => f.Dirty)));
                sc.Undo();
                check("light types: one undo puts every light back", sc.Lights.All(l => (l.Flags & shadows) == 0), "");
                int none = sc.SetFlagsForType_U35(1, 0, true);
                check("light types: no flags ticked changes nothing", none == 0, "");

                foreach (var l in sc.Lights) l.Intensity = 3f;
                sc.Lights[0].Falloff = 7f;
                int picked = sc.SelectType_U35(1);
                check("light types: Select all picks every spot light and nothing else",
                      picked == spots && sc.SelectedIndices.All(i => sc.Lights[i].Type == LightType.Spot), $"{picked} selected");
                var prim = sc.SelectedLight;
                var before = Scene.CloneLight(prim);
                prim.Intensity = 42f;
                sc.ApplyEditToSelection(before, prim);
                check("light types: ...so one change in Light parameters sets it on all of them",
                      sc.Lights.Where(l => l.Type == LightType.Spot).All(l => l.Intensity == 42f) &&
                      sc.Lights.Where(l => l.Type != LightType.Spot).All(l => l.Intensity == 3f), "");
                check("light types: ...and leaves their other values alone", sc.Lights[0].Falloff == 7f || ReferenceEquals(sc.Lights[0], prim), $"falloff {sc.Lights[0].Falloff}");
                sc.SelectedIndices.Clear();
                for (int i = 0; i < sc.Lights.Count; i++) sc.SelectedIndices.Add(i);
                int dropped = sc.DeselectType_U35(1);
                check("light types: Deselect takes only that type out of the selection",
                      dropped == spots && sc.SelectedIndices.Count == all - spots && sc.SelectedIndices.All(i => sc.Lights[i].Type != LightType.Spot),
                      $"dropped {dropped}, {sc.SelectedIndices.Count} left");
            }
            catch (Exception ex) { check("light types: the test ran", false, ex.ToString()); }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }
    }
}
