using System;
using System.Linq;
using RageLightEditor.Editor;

namespace RageLightEditor
{
    public partial class MainForm
    {
        internal static int LightsToMoveOnto_U56(Scene sc, LoadedFile target) =>
            target == null ? 0 : sc.SelectedLightList_V35().Count(l => !ReferenceEquals(sc.OwnerFile(l), target));

        private bool OfferMoveLightsHere_U56(int x, int y)
        {
            if (panel == null || scene == null || scene.SelectedIndices.Count == 0) return false;
            if (panel.MaterialMode || panel.CineMode || panel.ExtensionMode) return false;
            if (LightUnderCursor_U39(x, y) >= 0) return false;
            var target = FindPropUnder(camera.GetPickRay(x, y, deviceResources.Width, deviceResources.Height));
            if (LightsToMoveOnto_U56(scene, target) == 0) return false;
            panel.OfferMoveHere_U56(target);
            return true;
        }

        private void MoveHereTest_U56(Action<string, bool, string> check)
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "rle_u56_movehere");
            try
            {
                System.IO.Directory.CreateDirectory(dir);
                var a = System.IO.Path.Combine(dir, "rle_u56_a.ydr");
                var b = System.IO.Path.Combine(dir, "rle_u56_b.ydr");
                if (System.IO.File.Exists(a)) System.IO.File.Delete(a);
                TestSceneGenerator.Run(a);
                System.IO.File.Copy(a, b, true);
                var sc = new Scene(modelRenderer, null);
                sc.LoadModelFile(a, additive: false);
                sc.LoadModelFile(b, additive: true);
                var fa = sc.Files[0]; var fb = sc.Files[1];
                sc.SelectedIndices.Clear();
                check("right-click move: with no light selected the menu is not offered", LightsToMoveOnto_U56(sc, fb) == 0, "");
                var mine = sc.Lights.Where(l => sc.OwnerFile(l) == fa).Take(2).ToList();
                foreach (var l in mine) sc.SelectedIndices.Add(sc.Lights.IndexOf(l));
                check("right-click move: lights selected on another prop are offered for the prop under the cursor",
                      LightsToMoveOnto_U56(sc, fb) == 2 && LightsToMoveOnto_U56(sc, fa) == 0,
                      $"onto B {LightsToMoveOnto_U56(sc, fb)}, onto A {LightsToMoveOnto_U56(sc, fa)}");
            }
            catch (Exception ex) { check("right-click move: the test ran", false, ex.ToString()); }
        }
    }
}
