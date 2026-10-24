using System;
using System.IO;
using System.Linq;
using RageLightEditor.Editor;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private void MultiEditTest_U43(Action<string, bool, string> check)
        {
            string dir = Path.Combine(Path.GetTempPath(), "rle_u43_multi");
            try
            {
                Directory.CreateDirectory(dir);
                var a = Path.Combine(dir, "rle_u43_a.ydr");
                if (!File.Exists(a)) TestSceneGenerator.Run(a);
                var sc = new Scene(modelRenderer, null);
                sc.LoadModelFile(a, additive: false);
                if (sc.Lights.Count < 3) { check("multi edit: the test scene has three lights", false, sc.Lights.Count.ToString()); return; }
                var l0 = sc.Lights[0]; var l1 = sc.Lights[1]; var l2 = sc.Lights[2];
                l0.Intensity = 2f; l1.Intensity = 4f; l2.Intensity = 8f;
                l0.ConeOuterAngle = 30f; l1.ConeOuterAngle = 40f; l2.ConeOuterAngle = 50f;
                l0.Flags = 0x40u; l1.Flags = 0x80u; l2.Flags = 0u;
                sc.SelectedIndices.Clear(); sc.SelectedIndices.Add(1); sc.SelectedIndices.Add(2); sc.SelectedIndices.Add(0);

                sc.MultiEditRelative_U43 = true;
                var before = Scene.CloneLight(l0);
                l0.Intensity = 4f;
                l0.ConeOuterAngle = 35f;
                sc.ApplyEditToSelection(before, l0);
                check("multi edit: doubling one light's intensity doubles each light's own (brighter ones stay brighter)",
                      l1.Intensity == 8f && l2.Intensity == 16f, $"{l0.Intensity} / {l1.Intensity} / {l2.Intensity}");
                check("multi edit: cone angles move by the same number of degrees",
                      l1.ConeOuterAngle == 45f && l2.ConeOuterAngle == 55f, $"{l1.ConeOuterAngle} / {l2.ConeOuterAngle}");

                before = Scene.CloneLight(l0);
                l0.Flags |= 0x100u;
                sc.ApplyEditToSelection(before, l0);
                check("multi edit: ticking a flag adds only that flag - each light keeps its other flags",
                      l1.Flags == (0x80u | 0x100u) && l2.Flags == 0x100u, $"0x{l1.Flags:X} / 0x{l2.Flags:X}");

                sc.MultiEditRelative_U43 = false;
                before = Scene.CloneLight(l0);
                l0.Intensity = 5f;
                sc.ApplyEditToSelection(before, l0);
                check("multi edit: with 'keep each light's own values' off, every light gets the exact number",
                      l1.Intensity == 5f && l2.Intensity == 5f, $"{l1.Intensity} / {l2.Intensity}");

                sc.MultiEditRelative_U43 = true;
                l0.Intensity = 2f; l1.Intensity = 4f; l2.Intensity = 8f;
                before = Scene.CloneLight(l0);
                l0.Intensity = 0f;
                sc.ApplyEditToSelection(before, l0);
                bool notAllZero = l1.Intensity > 0f && l2.Intensity > 0f;
                before = Scene.CloneLight(l0);
                l0.Intensity = 2f;
                sc.ApplyEditToSelection(before, l0);
                check("multi edit: dragging down to 0 and back does not flatten the others to the same value",
                      notAllZero && l1.Intensity == 4f && l2.Intensity == 8f, $"{l1.Intensity} / {l2.Intensity}");
            }
            catch (Exception ex) { check("multi edit: the test ran", false, ex.ToString()); }
            finally { try { Directory.Delete(dir, true); } catch { } }
            MultiEditSavesTest_U43(check);
        }

        private void MultiEditSavesTest_U43(Action<string, bool, string> check)
        {
            string dir = Path.Combine(Path.GetTempPath(), "rle_u43_save");
            try
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);
                var a = Path.Combine(dir, "rle_u43_save_a.ydr");
                var b = Path.Combine(dir, "rle_u43_save_b.ydr");
                TestSceneGenerator.Run(a);
                TestSceneGenerator.Run(b);
                var sc = new Scene(modelRenderer, null);
                sc.LoadModelFile(a, additive: false);
                sc.LoadModelFile(b, additive: true);
                var fa = sc.Files[0]; var fb = sc.Files[1];
                int ia = sc.Lights.FindIndex(l => sc.OwnerFile(l) == fa);
                int ib = sc.Lights.FindIndex(l => sc.OwnerFile(l) == fb);
                var la = sc.Lights[ia]; var lb = sc.Lights[ib];
                int idxInB = sc.Lights.Where(l => sc.OwnerFile(l) == fb).ToList().IndexOf(lb);
                foreach (var f in sc.Files) f.Dirty = false;
                sc.SelectedIndices.Clear(); sc.SelectedIndices.Add(ib); sc.SelectedIndices.Add(ia);

                var before = Scene.CloneLight(la);
                la.ShadowFadeDistance = 77; la.LightFadeDistance = 66; la.SpecularFadeDistance = 55; la.VolumetricFadeDistance = 44;
                la.ShadowNearClip = 0.37f; la.ShadowBlur = 9;
                int n = sc.ApplyEditToSelection(before, la);
                check("multi edit: shadow and fade settings reach the other selected light",
                      n == 1 && lb.ShadowFadeDistance == 77 && lb.LightFadeDistance == 66 && lb.SpecularFadeDistance == 55 &&
                      lb.VolumetricFadeDistance == 44 && Math.Abs(lb.ShadowNearClip - 0.37f) < 1e-4f && lb.ShadowBlur == 9,
                      $"shadow fade {lb.ShadowFadeDistance}, light fade {lb.LightFadeDistance}, near clip {lb.ShadowNearClip}, blur {lb.ShadowBlur}");
                check("multi edit: ...and its prop is marked unsaved", fb.Dirty, "");

                sc.Save();
                var back = WorldLights.ReloadLight(b, idxInB, out _);
                check("multi edit: after Save the other prop's file has the new shadow and fade values",
                      back != null && back.ShadowFadeDistance == 77 && back.LightFadeDistance == 66 && back.SpecularFadeDistance == 55 &&
                      back.VolumetricFadeDistance == 44 && Math.Abs(back.ShadowNearClip - 0.37f) < 1e-4f && back.ShadowBlur == 9,
                      back == null ? "no light" : $"shadow fade {back.ShadowFadeDistance}, light fade {back.LightFadeDistance}, specular {back.SpecularFadeDistance}, volumetric {back.VolumetricFadeDistance}, near clip {back.ShadowNearClip}, blur {back.ShadowBlur}");
            }
            catch (Exception ex) { check("multi edit save: the test ran", false, ex.ToString()); }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }
    }
}
