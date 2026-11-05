using System;
using System.IO;
using System.Linq;
using RageLightEditor.Editor;
using SharpDX;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private void ServiceTwinLights_U53()
        {
            var cur = scene;
            cur?.SyncTwinLights_U53();
            if (lightScene != null && !ReferenceEquals(lightScene, cur)) lightScene.SyncTwinLights_U53();
            if (mloScene != null && !ReferenceEquals(mloScene, cur)) mloScene.SyncTwinLights_U53();
        }

        private void TwinLightsTest_U53(Action<string, bool, string> check)
        {
            string dir = Path.Combine(Path.GetTempPath(), "rle_u53_twins");
            try
            {
                Directory.CreateDirectory(dir);
                var a = Path.Combine(dir, "rle_u53_prop.ydr");
                if (!File.Exists(a)) TestSceneGenerator.Run(a);
                var sc = new Scene(modelRenderer, null);
                sc.LoadModelFile(a, additive: false);
                var src = sc.Files[0];
                int n = sc.Lights.Count;
                var copyLights = sc.Lights.Where(l => sc.OwnerFile(l) == src).Select(Scene.CloneLight).ToArray();
                var twin = sc.AddImportedProp(src.Path, src.Ydr, src.Yft, null, src.Skeleton, copyLights,
                                              Matrix.Translation(10f, 0f, 0f), 1, src.ReadOnly, src.Name, null, fromMlo: false);
                int Own(LoadedFile f) => sc.Lights.Count(l => sc.OwnerFile(l) == f);
                int Gpu() => sc.BuildGpuLights(new Rendering.GpuLight[4096], 12, false, 0f, null, Vector3.Zero);
                sc.SyncTwinLights_U53();
                int g0 = Gpu();

                sc.SelectFile(src, false, false);
                var added = sc.AddLight(2);
                added.Position = new Vector3(0.25f, 0.5f, 1.5f);
                int copied = sc.SyncTwinLights_U53();
                var mirror = sc.Lights.LastOrDefault(l => sc.OwnerFile(l) == twin);
                check("duplicated props: a light attached to one copy appears on the duplicate too",
                      copied == 1 && Own(twin) == n + 1 && mirror != null && mirror.Position == added.Position && Gpu() == g0 + 2,
                      $"copied {copied}, twin lights {Own(twin)} of {n + 1}, gpu {g0} -> {Gpu()}");
                check("duplicated props: ...at the same spot on the duplicate, not on top of the first one",
                      mirror != null && Vector3.Distance(sc.GetInstance(mirror).WorldPosition, sc.GetInstance(added).WorldPosition) > 9.9f,
                      mirror == null ? "none" : sc.GetInstance(mirror).WorldPosition.ToString());
                check("duplicated props: the light you are editing stays selected",
                      ReferenceEquals(sc.SelectedLight, added),
                      ReferenceEquals(sc.SelectedLight, added) ? "the added light" : sc.SelectedLight == null ? "nothing selected" : "another light");

                sc.PushUndo();
                mirror.Intensity = 42f;
                sc.SyncTwinLights_U53();
                check("duplicated props: editing the light on the duplicate changes the original too",
                      Math.Abs(added.Intensity - 42f) < 1e-4f || sc.Lights.Any(l => sc.OwnerFile(l) == src && Math.Abs(l.Intensity - 42f) < 1e-4f), "");

                sc.Undo();
                sc.SyncTwinLights_U53();
                check("duplicated props: one Undo takes the change off both copies",
                      !sc.Lights.Any(l => Math.Abs(l.Intensity - 42f) < 1e-4f) && Own(src) == n + 1 && Own(twin) == n + 1,
                      $"src {Own(src)}, twin {Own(twin)}");

                var other = sc.AddImportedProp(Path.Combine(dir, "rle_u53_other.ydr"), null, null, null, null, null,
                                               Matrix.Translation(-10f, 0f, 0f), 1, true, "rle_u53_other.ydr", null, fromMlo: false);
                sc.SelectFile(src, false, false);
                sc.AddLight(1);
                sc.SyncTwinLights_U53();
                check("duplicated props: a different prop does not get the lights", Own(other) == 0, Own(other).ToString());
            }
            catch (Exception ex) { check("duplicated props: the test ran", false, ex.ToString()); }
        }
    }
}
