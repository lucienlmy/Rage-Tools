using System;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private const float LightUnits_U50 = 2.0f;

        internal static float VolumeScale_U50(float volumeIntensity, float lightIntensity, float radius, float outerAngleRad, int type)
        {
            float size = type == 2 ? radius * (float)Math.Sin(outerAngleRad) : radius;
            float shaderIntensity = volumeIntensity * lightIntensity / Math.Max(size, 1e-3f) / 16.0f;
            return shaderIntensity * lightIntensity * LightUnits_U50;
        }
    }
}
