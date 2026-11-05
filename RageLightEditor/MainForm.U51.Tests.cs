using System;
using System.Collections.Generic;
using CodeWalker.GameFiles;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private void ProjTexTest_U51(Action<string, bool, string> check)
        {
            LightAttributes L(LightType t, uint h) => new LightAttributes { Type = t, ProjectedTextureHash = new MetaHash(h), Flags = 0 };
            var far = L(LightType.Spot, 1); var near = L(LightType.Spot, 2); var sel = L(LightType.Spot, 3);
            var capsule = L(LightType.Capsule, 4); var none = L(LightType.Spot, 0); var point = L(LightType.Point, 5);
            var sources = new List<LightAttributes> { far, near, sel, capsule, none, point };
            float[] dist = { 100f, 1f, 500f, 0.1f, 0.1f, 50f };
            var order = ProjectedTextureOrder_U51(sources, sources.Count, x => ReferenceEquals(x, sel), i => dist[i]);
            check("projected texture: the selected light gets a texture slot first, then the nearest lights",
                  order.Count == 4 && order[0] == 2 && order[1] == 1 && order[2] == 5 && order[3] == 0, string.Join(",", order));
            check("projected texture: a hash counts without flag 5 (the game sets it itself); capsules and lights with no hash don't take a slot",
                  !order.Contains(3) && !order.Contains(4) && order.Contains(0), string.Join(",", order));

            float lum0 = 0.2f, lum1 = 1.0f;
            float gameSwing = (float)(Math.Pow(2.0, GameExposureStops(lum0, 0f, -3.5f, 5f)) / Math.Pow(2.0, GameExposureStops(lum1, 0f, -3.5f, 5f)));
            float oldSwing = (0.72f / lum0) / (0.72f / lum1);
            check("exposure: the game curve moves a surface far less between a dark view and a bright one than the old average rule",
                  gameSwing < 2.0f && oldSwing >= 5.0f - 1e-3f, $"game {gameSwing:0.00}x, old {oldSwing:0.00}x");
        }
    }
}
