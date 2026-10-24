using System;
using System.IO;
using System.Linq;
using RageLightEditor.Editor;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private void SaveEditedOnlyTest_U47(Action<string, bool, string> check)
        {
            string dir = Path.Combine(Path.GetTempPath(), "rle_u47_save");
            try
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);
                var a = Path.Combine(dir, "rle_u47_a.ydr");
                var b = Path.Combine(dir, "rle_u47_b.ydr");
                var c = Path.Combine(dir, "rle_u47_c.ydr");
                TestSceneGenerator.Run(a); TestSceneGenerator.Run(b); TestSceneGenerator.Run(c);
                var stamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                foreach (var p in new[] { a, b, c }) File.SetLastWriteTimeUtc(p, stamp);
                var sc = new Scene(modelRenderer, null);
                sc.LoadModelFile(a, additive: false);
                sc.LoadModelFile(b, additive: true);
                sc.LoadModelFile(c, additive: true);
                var fa = sc.Files[0]; var fb = sc.Files[1]; var fc = sc.Files[2];
                var la = sc.Lights.First(l => sc.OwnerFile(l) == fa);
                var lc = sc.Lights.First(l => sc.OwnerFile(l) == fc);

                sc.PushUndo();
                la.Intensity += 3f;
                sc.Dirty = true;
                lc.Position += new SharpDX.Vector3(0.5f, 0, 0);
                sc.Dirty = true;
                check("save edited only: an edit marks only the prop whose light changed",
                      fa.Dirty && !fb.Dirty && fc.Dirty, $"a {fa.Dirty}, b {fb.Dirty}, c {fc.Dirty}");

                sc.Save();
                bool aWritten = File.GetLastWriteTimeUtc(a) != stamp, bWritten = File.GetLastWriteTimeUtc(b) != stamp, cWritten = File.GetLastWriteTimeUtc(c) != stamp;
                check("save edited only: Save all writes the edited props and leaves the untouched one alone",
                      aWritten && !bWritten && cWritten && sc.LastSaved_U47.Count == 2,
                      $"a written {aWritten}, b written {bWritten}, c written {cWritten}, saved {sc.LastSaved_U47.Count}");

                sc.Save();
                check("save edited only: saving again with nothing changed writes nothing", sc.LastSaved_U47.Count == 0, $"saved {sc.LastSaved_U47.Count}");

                sc.Undo();
                check("save edited only: Undo after a save marks the props it changed back",
                      fa.Dirty && !fb.Dirty && fc.Dirty, $"a {fa.Dirty}, b {fb.Dirty}, c {fc.Dirty}");
            }
            catch (Exception ex) { check("save edited only: the test ran", false, ex.ToString()); }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }
    }
}
