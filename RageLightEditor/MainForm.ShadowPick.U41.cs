using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private int lastShadowOrderSig_U41 = int.MinValue;
        private double lastShadowPickTime_U41 = -1000.0;
        private const double ShadowRepickSeconds_U41 = 0.5;

        internal int ShadowOrderSignature_U41(List<int> order)
        {
            unchecked
            {
                int sig = order.Count * 397;
                foreach (var i in order)
                {
                    var src = i < gpuLightSources.Count ? gpuLightSources[i] : null;
                    sig = sig * 31 + (src == null ? 0 : RuntimeHelpers.GetHashCode(src));
                    sig = sig * 31 + (int)gpuLights[i].Type;
                }
                return sig;
            }
        }
    }
}
