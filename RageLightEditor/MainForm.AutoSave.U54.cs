using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private const double AutoSaveSeconds_U54 = 30.0;
        private double autoSaveDue_U54 = -1;

        private bool AutoSaveBlocked_U54() =>
            IsHeadless || DebugSeqTest || Environment.GetEnvironmentVariable("RLE_SHELLTEST") != null;

        private void ServiceAutoSave_U54()
        {
            if (panel == null || lightScene == null || !panel.AutoSaveLights_U54 || AutoSaveBlocked_U54()) { autoSaveDue_U54 = -1; return; }
            double now = Environment.TickCount64 / 1000.0;
            if (autoSaveDue_U54 < 0) { autoSaveDue_U54 = now + AutoSaveSeconds_U54; return; }
            if (now < autoSaveDue_U54) return;
            if ((gizmo?.Dragging ?? false) || worldGizmo.Dragging || Control.MouseButtons != MouseButtons.None) return;
            autoSaveDue_U54 = now + AutoSaveSeconds_U54;
            var errors = new List<string>();
            var saved = lightScene.AutoSave_U54(errors);
            if (saved.Count > 0)
                panel.MloStatus = $"Auto saved {saved.Count} prop{(saved.Count == 1 ? "" : "s")} at {DateTime.Now:HH:mm:ss}: " +
                                  string.Join(", ", saved.Take(4).Select(f => f.Name)) + (saved.Count > 4 ? " ..." : "");
            if (errors.Count > 0)
            {
                panel.MloStatus = "Auto save could not write " + string.Join("; ", errors.Take(2));
                foreach (var e in errors) Editor.AppLog_U21.Error("Auto save: " + e);
            }
        }

        private void AutoSaveTest_U54(Action<string, bool, string> check)
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "rle_u54_autosave");
            try
            {
                System.IO.Directory.CreateDirectory(dir);
                var a = System.IO.Path.Combine(dir, "rle_u54_a.ydr");
                var b = System.IO.Path.Combine(dir, "rle_u54_b.ydr");
                if (System.IO.File.Exists(a)) System.IO.File.Delete(a);
                TestSceneGenerator.Run(a);
                System.IO.File.Copy(a, b, true);
                var sc = new Editor.Scene(modelRenderer, null);
                sc.LoadModelFile(a, additive: false);
                sc.LoadModelFile(b, additive: true);
                var fa = sc.Files[0]; var fb = sc.Files[1];
                var ta = System.IO.File.GetLastWriteTimeUtc(a); var tb = System.IO.File.GetLastWriteTimeUtc(b);
                System.Threading.Thread.Sleep(30);
                var errors = new List<string>();
                var none = sc.AutoSave_U54(errors);
                check("auto save: with nothing changed it writes nothing", none.Count == 0 && errors.Count == 0 &&
                      System.IO.File.GetLastWriteTimeUtc(a) == ta, none.Count.ToString());
                var l = sc.Lights.First(x => sc.OwnerFile(x) == fa);
                sc.PushUndo();
                float want = l.Intensity + 7f;
                l.Intensity = want;
                var saved = sc.AutoSave_U54(errors);
                check("auto save: it writes only the prop whose lights changed",
                      saved.Count == 1 && ReferenceEquals(saved[0], fa) && !fa.Dirty &&
                      System.IO.File.GetLastWriteTimeUtc(a) != ta && System.IO.File.GetLastWriteTimeUtc(b) == tb,
                      $"saved {saved.Count}, errors {errors.Count}");
                var back = new Editor.Scene(modelRenderer, null);
                back.LoadModelFile(a, additive: false);
                check("auto save: the change is in the file", back.Lights.Any(x => Math.Abs(x.Intensity - want) < 1e-3f), "");
                var proxy = sc.AddLightProxy("rle_u54_never_written");
                sc.AutoSave_U54(errors);
                check("auto save: it never creates a file that was not on disk",
                      !System.IO.File.Exists(proxy.Path) && !System.IO.File.Exists(System.IO.Path.Combine(dir, proxy.Path)), proxy.Path);
                check("auto save: it never runs during tests or probes", AutoSaveBlocked_U54(), "");
            }
            catch (Exception ex) { check("auto save: the test ran", false, ex.ToString()); }
        }
    }
}
