using System;
using System.Collections.Generic;
using System.Linq;
using CodeWalker.GameFiles;
using RageLightEditor.Editor;
using RageLightEditor.Rendering;
using SharpDX;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private bool projTexProbeDone_U51;
        private bool projTexProbeLogged_U51;
        private LightAttributes projTexProbeLight_U51;

        private CodeWalker.GameFiles.Texture ResolveProjectedTexture_U51(uint hash) =>
            scene.FindTexture(hash) ?? gameFiles?.FindTexture(hash, 0);

        internal static List<int> ProjectedTextureOrder_U51(IList<LightAttributes> sources, int count, Func<LightAttributes, bool> selected, Func<int, float> distSq)
        {
            var order = new List<int>();
            for (int i = 0; i < count && i < sources.Count; i++)
                if (sources[i] != null && sources[i].ProjectedTextureHash.Hash != 0 && sources[i].Type != LightType.Capsule) order.Add(i);
            order.Sort((a, b) =>
            {
                int s = selected(sources[b]).CompareTo(selected(sources[a]));
                return s != 0 ? s : distSq(a).CompareTo(distSq(b));
            });
            return order;
        }

        private void AssignProjectedTextures_U51(int lightCount)
        {
            Array.Clear(projTexSrvs, 0, projTexSrvs.Length);
            if (NoProjTex_U25) return;
            var cam = camera.Position;
            var slots = new Dictionary<uint, int>();
            foreach (int i in ProjectedTextureOrder_U51(gpuLightSources, lightCount, scene.IsLightSelected, k => Vector3.DistanceSquared(cam, gpuLights[k].Position)))
            {
                uint hash = gpuLightSources[i].ProjectedTextureHash.Hash;
                if (!slots.TryGetValue(hash, out int slot))
                {
                    if (slots.Count >= GpuLight.MaxProjTextures) continue;
                    var tex = ResolveProjectedTexture_U51(hash);
                    var srv = tex != null ? textureLoader.GetSRV(tex) : null;
                    if (srv == null) continue;
                    slot = slots.Count;
                    slots[hash] = slot;
                    projTexSrvs[slot] = srv;
                }
                gpuLights[i].ProjTexIndex = slot;
            }
            if (screenshotPath != null && projTexProbeLight_U51 != null && !projTexProbeLogged_U51)
            {
                int gi = gpuLightSources.IndexOf(projTexProbeLight_U51);
                if (gi >= 0)
                {
                    projTexProbeLogged_U51 = true;
                    var t = ResolveProjectedTexture_U51(projTexProbeLight_U51.ProjectedTextureHash.Hash);
                    Console.WriteLine($"PROJTEX light {gi} type {projTexProbeLight_U51.Type} slot {gpuLights[gi].ProjTexIndex} texture {(t != null ? $"{t.Name} {t.Width}x{t.Height} {t.Format}" : "NOT FOUND")} slots used {slots.Count}");
                }
            }
        }

        private void ServiceProjTexProbe_U51()
        {
            if (panel != null && panel.ProjTexInGame_U51 == null)
                panel.ProjTexInGame_U51 = h => gameFiles?.FindTexture(h, 0) != null;
            var want = Environment.GetEnvironmentVariable("RLE_PROJPROBE");
            if (string.IsNullOrEmpty(want) || screenshotPath == null || projTexProbeDone_U51 || scene == null || scene.Lights.Count == 0) return;
            if (gameFiles == null || !gameFiles.Ready) return;
            projTexProbeDone_U51 = true;
            var bits = want.Split(';');
            var kind = bits.Length > 1 && bits[1] == "point" ? LightType.Point : LightType.Spot;
            var l = scene.Lights.Where(x => x != null && x.Type == kind).OrderByDescending(x => x.Falloff * x.Intensity).FirstOrDefault();
            if (l == null) { Console.WriteLine("PROJTEX no light of that type"); return; }
            if (bits[0] != "none")
            {
                var clean = bits[0].Contains('.') ? scene.ImportTexture(bits[0]) : bits[0].ToLowerInvariant();
                JenkIndex.Ensure(clean);
                l.ProjectedTextureHash = new MetaHash(JenkHash.GenHash(clean));
                l.Flags |= LightDefs.FlagTextureProjection;
            }
            if (bits.Length > 3 && bits[3] == "solo")
            {
                foreach (var o in scene.Lights) if (o != null && !ReferenceEquals(o, l)) o.Intensity = 0f;
                l.Intensity = 30f; l.Falloff = 10f;
            }
            if (bits.Length > 2 && float.TryParse(bits[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var cone))
                l.ConeOuterAngle = cone;
            scene.SelectedIndex = scene.Lights.IndexOf(l);
            projTexProbeLight_U51 = l;
            var p = scene.GetInstance(l);
            Console.WriteLine($"PROJTEX chose {l.Type} at {p.WorldPosition.X:0.00},{p.WorldPosition.Y:0.00},{p.WorldPosition.Z:0.00} dir {p.WorldDirection.X:0.00},{p.WorldDirection.Y:0.00},{p.WorldDirection.Z:0.00} intensity {l.Intensity:0.##} falloff {l.Falloff:0.##} cone {l.ConeOuterAngle:0.#}");
        }
    }
}
