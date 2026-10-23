using System;
using CodeWalker.GameFiles;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private static readonly bool ShadowProbeOn_U41 = Environment.GetEnvironmentVariable("RLE_SHADOWPROBE") == "1";
        private int shadowProbeStage_U41, shadowProbeFrames_U41;
        private LightAttributes shadowProbeLight_U41;
        private uint shadowProbeFlags_U41;
        private bool shadowProbeOff_U41;

        private float ProbeSlot_U41()
        {
            int gi = gpuLightSources.IndexOf(shadowProbeLight_U41);
            return gi >= 0 && gi < gpuLights.Length ? gpuLights[gi].ShadowSlot : -2;
        }

        private void ShadowProbe_U41()
        {
            if (!ShadowProbeOn_U41 || shadowProbeStage_U41 < 0 || scene.Lights.Count == 0) return;
            shadowProbeFrames_U41++;
            if (screenshotPath != null) screenshotFrames = Math.Max(screenshotFrames, 4);
            const uint mask = 0x40u | 0x80u | 0x100u | 0x1000000u;
            switch (shadowProbeStage_U41)
            {
                case 0:
                    if (shadowProbeFrames_U41 < 20) return;
                    for (int gi = 0; gi < gpuLightSources.Count && gi < gpuLights.Length; gi++)
                    {
                        var src = gpuLightSources[gi];
                        if (src == null || src.Type != LightType.Spot || gpuLights[gi].ShadowSlot < 0) continue;
                        shadowProbeLight_U41 = src;
                        break;
                    }
                    if (shadowProbeLight_U41 == null) { Console.WriteLine("SHADOWPROBE no spot light is casting a shadow here"); shadowProbeStage_U41 = -1; return; }
                    shadowProbeFlags_U41 = shadowProbeLight_U41.Flags;
                    shadowProbeLight_U41.Flags &= ~mask;
                    shadowProbeStage_U41 = 1; shadowProbeFrames_U41 = 0;
                    return;
                case 1:
                    if (shadowProbeFrames_U41 < 6) return;
                    shadowProbeOff_U41 = ProbeSlot_U41() < 0;
                    shadowProbeLight_U41.Flags = shadowProbeFlags_U41;
                    shadowProbeStage_U41 = 2; shadowProbeFrames_U41 = 0;
                    return;
                case 2:
                    if (shadowProbeFrames_U41 < 6) return;
                    bool back = ProbeSlot_U41() >= 0;
                    Console.WriteLine($"SHADOWPROBE shadow flags cleared without touching a checkbox: shadow {(shadowProbeOff_U41 ? "turned off" : "STILL ON")}; flags put back: shadow {(back ? "came back" : "DID NOT come back")}");
                    shadowProbeStage_U41 = -1;
                    return;
            }
        }
    }
}
