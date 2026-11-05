using System;
using System.IO;
using System.Linq;
using RageLightEditor.Editor;
using SharpDX;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private void CopyLightsTest_U55(Action<string, bool, string> check)
        {
            string dir = Path.Combine(Path.GetTempPath(), "rle_u55_copies");
            try
            {
                Directory.CreateDirectory(dir);
                var a = Path.Combine(dir, "rle_u55_lamp.ydr");
                var b = Path.Combine(dir, "rle_u55_table.ydr");
                if (File.Exists(a)) File.Delete(a);
                TestSceneGenerator.Run(a);
                File.Copy(a, b, true);
                var sc = new Scene(modelRenderer, null);
                sc.LoadModelFile(a, additive: false);
                sc.LoadModelFile(b, additive: true);
                var lamp = sc.Files[0]; var table = sc.Files[1];
                Matrix At(float yawDeg, Vector3 pos) => Matrix.RotationZ(MathUtil.DegreesToRadians(yawDeg)) * Matrix.Translation(pos);
                lamp.Placement = At(15f, new Vector3(-30, 4, 0)); lamp.HasPlacement = true;
                table.Placement = At(0f, new Vector3(0, 0, 0)); table.HasPlacement = true;
                table.ExtraPlacements.Add(At(90f, new Vector3(20, 0, 0)));
                table.ExtraPlacements.Add(At(200f, new Vector3(40, 10, 2)));

                var light = sc.LightsOf_V35(lamp).First();
                light.Direction = Vector3.Normalize(new Vector3(0.3f, 0.2f, -1f));
                var tableBox = Scene.DrawableOf_U55(table);
                var onCopy2 = Vector3.TransformCoordinate((tableBox.BoundingBoxMin + tableBox.BoundingBoxMax) * 0.5f + new Vector3(0.1f, 0.2f, 0.3f), Scene.PlacementOf_U55(table, 2));
                light.Position = Vector3.TransformCoordinate(onCopy2, Matrix.Invert(lamp.Placement));
                var before = sc.GetInstance(light);
                var copy2Before = Scene.PlacementOf_U55(table, 2);

                var gpu = new Rendering.GpuLight[256];
                int g0 = sc.BuildGpuLights(gpu, 12, false, 0f, null, Vector3.Zero);
                sc.MoveLightsToProp_V35(new[] { light }, table, keepWorldPosition: true);
                int g1 = sc.BuildGpuLights(gpu, 12, false, 0f, null, Vector3.Zero);
                var after = sc.GetInstance(light);
                check("repeated props: a light moved onto the copy you are working on stays exactly where it was",
                      Vector3.Distance(before.WorldPosition, after.WorldPosition) < 1e-3f &&
                      Vector3.Dot(before.WorldDirection, after.WorldDirection) > 0.9999f,
                      $"drift {Vector3.Distance(before.WorldPosition, after.WorldPosition):0.####} m");
                check("repeated props: ...and that copy becomes the one you edit",
                      SameMatrix_U55(table.Placement, copy2Before), table.Placement.TranslationVector.ToString());

                var expectLocal = Vector3.TransformCoordinate(onCopy2, Matrix.Invert(copy2Before));
                float worst = 0f;
                for (int k = 0; k <= table.ExtraPlacements.Count; k++)
                {
                    var p = Scene.PlacementOf_U55(table, k);
                    var want = Vector3.TransformCoordinate(expectLocal, p);
                    var got = sc.GetInstanceAt(light, p).WorldPosition;
                    worst = Math.Max(worst, Vector3.Distance(want, got));
                }
                check("repeated props: every copy gets the light at the same spot on it (turned with the copy)",
                      worst < 1e-3f, $"worst {worst:0.####} m");
                check("repeated props: it is drawn once per copy (one prop had it, now three copies do)", g1 - g0 == 2, $"{g0} -> {g1}");

                sc.ActiveFile = table;
                var spawnOn1 = Vector3.TransformCoordinate(new Vector3(0.2f, -0.1f, 0.5f), Scene.PlacementOf_U55(table, 1));
                var c1 = Scene.PlacementOf_U55(table, 1);
                sc.NextLightSpawnPos = spawnOn1;
                var added = sc.AddLight(1);
                check("repeated props: a new light added on a copy appears where you are, not on another copy",
                      Vector3.Distance(sc.GetInstance(added).WorldPosition, spawnOn1) < 1e-3f && SameMatrix_U55(table.Placement, c1),
                      sc.GetInstance(added).WorldPosition.ToString());
                try { sc.Dispose(); } catch { }
            }
            catch (Exception ex) { check("repeated props: the test ran", false, ex.ToString()); }
        }

        private static bool SameMatrix_U55(Matrix x, Matrix y)
        {
            for (int i = 0; i < 16; i++) if (Math.Abs(x[i] - y[i]) > 1e-4f) return false;
            return true;
        }
    }
}
