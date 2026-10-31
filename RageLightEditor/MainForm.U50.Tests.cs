using System;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private void VolumeScaleTest_U50(Action<string, bool, string> check)
        {
            float a80 = 80f * (float)Math.PI / 180f;
            float spot = VolumeScale_U50(0.5f, 0.3f, 5.5f, a80, 2);
            float expect = 0.5f * 0.3f / (5.5f * (float)Math.Sin(a80)) / 16f * 0.3f * 2f;
            check("light volumes: a spot volume is scaled the way lights.cpp does it (/ radius*sin(cone), /16, x light intensity)",
                  Math.Abs(spot - expect) < 1e-6f, $"{spot} vs {expect}");
            float point = VolumeScale_U50(1f, 2f, 4f, 0f, 1);
            check("light volumes: a point volume is divided by its radius instead", Math.Abs(point - 1f * 2f / 4f / 16f * 2f * 2f) < 1e-6f, $"{point}");
            check("light volumes: the sub's green light is now a faint haze, not a fog (over 100x dimmer than before)",
                  spot * 100f < 0.5f * 0.3f, $"{spot} vs old {0.5f * 0.3f}");
        }
    }
}
