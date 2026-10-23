using System;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private void MarkerFadeTest_U34(Action<string, bool, string> check)
        {
            check("light markers: full strength up close", MarkerFade_U34(5f) == 1f && MarkerFade_U34(MarkerFadeStart_U34) == 1f, MarkerFade_U34(5f).ToString());
            float mid = MarkerFade_U34((MarkerFadeStart_U34 + MarkerFadeEnd_U34) * 0.5f);
            check("light markers: half faded half way out", Math.Abs(mid - 0.5f) < 0.01f, mid.ToString("0.00"));
            check("light markers: gone far away", MarkerFade_U34(MarkerFadeEnd_U34) == 0f && MarkerFade_U34(200f) == 0f, MarkerFade_U34(200f).ToString());
        }
    }
}
