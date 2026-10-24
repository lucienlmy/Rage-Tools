using ImGuiNET;

namespace RageLightEditor.Editor
{
    public partial class ModelViewer
    {
        private void DrawCollisionToggle_U48(string label)
        {
            bool none = CollisionParts_U48 == 0;
            if (none) ImGui.BeginDisabled();
            ImGui.Checkbox(label, ref ShowCollision_U48);
            if (none) ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(none
                    ? "This model carries no collision of its own. Map models keep theirs in a separate .ybn - open that file to see it."
                    : $"Shows the collision built into this model ({CollisionTriangles_U48:N0} triangles), coloured by material, over the model.");
        }
    }
}
