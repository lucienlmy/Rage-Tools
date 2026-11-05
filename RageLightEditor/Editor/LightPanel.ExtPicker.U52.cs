using System.Numerics;
using CodeWalker.GameFiles;
using ImGuiNET;

namespace RageLightEditor.Editor
{
    public partial class LightPanel
    {
        public bool ExtPicking_U52;
        public string ExtHoverName_U52;
        public bool ExtScrollToPick_U52;

        private void DrawExtPicker_U52()
        {
            bool on = ExtPicking_U52;
            if (on) ImGui.PushStyleColor(ImGuiCol.Button, UiTheme.Accent);
            if (ImGui.Button(on ? "Picking - click an object in the viewport##u52" : "Pick an object in the viewport##u52", new Vector2(-1, 0)))
                ExtPicking_U52 = !ExtPicking_U52;
            if (on) ImGui.PopStyleColor();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Hover the scene to see what each object is, then click one:\n" +
                                 "its archetype opens here, placed exactly where that object sits,\n" +
                                 "so the extensions you add land on it. Esc or right-click stops.");
            if (on)
                ImGui.TextDisabled(string.IsNullOrEmpty(ExtHoverName_U52) ? "under the cursor: nothing" : "under the cursor: " + ExtHoverName_U52);
        }

        private void DrawShaftCorners_U52(MetaWrapper sel)
        {
            if (!(sel is MCExtensionDefLightShaft ls)) return;
            ImGui.TextDisabled("CORNERS");
            ImGui.TextWrapped("Stand inside, where the light falls, and go round the window clockwise: A, B, C, D - one corner after the next, never across. Direction points into the room.");
            var d = ls._Data;
            if (!ShaftWinding_U52.HasArea(d)) return;
            if (ShaftWinding_U52.AgainstDirection(d))
            {
                ImGui.TextColored(new Vector4(1f, 0.65f, 0.3f, 1f), "The corners go round the wrong way for its direction - the game won't draw this shaft.");
                if (ImGui.Button("Fix the corner order##u52", new Vector2(-1, 0)))
                {
                    Ext.PushUndo_V70("Fix the corner order");
                    ShaftWinding_U52.Reverse(ls);
                    ArchEdited = true;
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Swaps B and D. The shaft stays where it is.");
            }
            else if ((d.flags & ShaftWinding_U52.FlagDrawFrontAndBehind) == 0)
                ImGui.TextDisabled("Corner order is right. It shows only from the room side (flag 64 shows it from both).");
            else
                ImGui.TextDisabled("Corner order is right.");
        }
    }
}
