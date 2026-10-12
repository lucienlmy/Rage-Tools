using System;
using System.IO;
using System.Linq;
using CodeWalker.GameFiles;
using RageLightEditor.Editor;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private void EditedModelsTest_U32(Action<string, bool, string> check)
        {
            string dir = Path.Combine(Path.GetTempPath(), "rle_u32_models");
            uint h1 = JenkHash.GenHash("rle_u32_lamp_a"), h2 = JenkHash.GenHash("rle_u32_lamp_b");
            try
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);
                string src = Path.Combine(dir, "src.ydr");
                TestSceneGenerator.Run(src);
                Drawable Load() { var y = new YdrFile(); y.Load(File.ReadAllBytes(src)); return y.Drawable; }
                var d1 = Load(); var d2 = Load();
                var l1 = WorldLights.LightsOf(d1)[0]; var l2 = WorldLights.LightsOf(d2)[0];
                l1.Intensity = 41.5f; l2.Intensity = 73.25f;
                TrackEditedModel_U32(h1, d1, "rle_u32_lamp_a"); worldLightUnsaved.Add(h1);
                TrackEditedModel_U32(h2, d2, "rle_u32_lamp_b"); worldLightUnsaved.Add(h2);
                ServiceEditedModels_U32();
                var m1 = panel.WorldEditedModels_U32.FirstOrDefault(m => m.Hash == h1);
                var m2 = panel.WorldEditedModels_U32.FirstOrDefault(m => m.Hash == h2);
                check("edited models: every model with changed lights is listed as unsaved",
                      m1 != null && m2 != null && m1.Unsaved && m2.Unsaved && m1.FileName == "rle_u32_lamp_a.ydr" && panel.WorldEditedModelsUnsaved_U32 >= 2,
                      string.Join(", ", panel.WorldEditedModels_U32.Select(m => m.FileName + (m.Unsaved ? "*" : ""))));
                if (m1 == null || m2 == null) return;

                string one = Path.Combine(dir, "one", m1.FileName);
                string err = SaveEditedModel_U32(m1, one);
                ServiceEditedModels_U32();
                var back = err == null ? WorldLights.ReloadLight(one, 0, out _) : null;
                check("edited models: one can be saved on its own, the other stays unsaved",
                      err == null && back != null && back.Intensity == 41.5f && !m1.Unsaved && m1.SavedPath == one && m2.Unsaved,
                      err ?? $"intensity {back?.Intensity}, a unsaved {m1.Unsaved}, b unsaved {m2.Unsaved}");

                l1.Intensity = 12.0f;
                MarkLightUnsaved_U32(h1);
                ServiceEditedModels_U32();
                check("edited models: editing a saved model again marks it unsaved again", m1.Unsaved && ReferenceEquals(m1.Drawable, d1), "unsaved " + m1.Unsaved);

                int want = panel.WorldEditedModelsUnsaved_U32;
                string all = Path.Combine(dir, "all");
                int done = SaveEditedModels_U32(all);
                ServiceEditedModels_U32();
                var backA = File.Exists(one) ? WorldLights.ReloadLight(one, 0, out _) : null;
                string pathB = Path.Combine(all, "rle_u32_lamp_b.ydr");
                var backB = File.Exists(pathB) ? WorldLights.ReloadLight(pathB, 0, out _) : null;
                check("edited models: Save all writes every unsaved model",
                      done == want && done >= 2 && panel.WorldEditedModelsUnsaved_U32 == 0, $"saved {done} of {want}, {panel.WorldEditedModelsStatus_U32}");
                check("edited models: ...a model saved before goes back to its own file",
                      backA != null && backA.Intensity == 12.0f && !File.Exists(Path.Combine(all, m1.FileName)), $"intensity {backA?.Intensity}");
                check("edited models: ...and a new one goes to the folder, with its edit",
                      backB != null && backB.Intensity == 73.25f && m2.SavedPath == pathB, $"intensity {backB?.Intensity}");
            }
            catch (Exception ex) { check("edited models: the test ran", false, ex.ToString()); }
            finally
            {
                foreach (var h in new[] { h1, h2 }) { editedModels_U32.Remove(h); worldLightUnsaved.Remove(h); worldLightSavedPath.Remove(h); }
                ServiceEditedModels_U32();
                panel.WorldEditedModelsStatus_U32 = null;
                try { Directory.Delete(dir, true); } catch { }
            }
        }
    }
}
