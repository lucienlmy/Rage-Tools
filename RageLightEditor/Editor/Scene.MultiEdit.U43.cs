using System;

namespace RageLightEditor.Editor
{
    public partial class Scene
    {
        public bool MultiEditRelative_U43 = true;

        private float Spread_U43(float before, float after, float target)
        {
            if (!MultiEditRelative_U43) return after;
            if (Math.Abs(before) > 1e-4f && Math.Abs(after) > 1e-4f) return Math.Max(0f, target * (after / before));
            return Math.Max(0f, target + (after - before));
        }

        private float Shift_U43(float before, float after, float target, float min, float max) =>
            MultiEditRelative_U43 ? Math.Clamp(target + (after - before), min, max) : after;

        private static uint Bits_U43(uint before, uint after, uint target)
        {
            uint changed = before ^ after;
            return (target & ~changed) | (after & changed);
        }
    }
}
