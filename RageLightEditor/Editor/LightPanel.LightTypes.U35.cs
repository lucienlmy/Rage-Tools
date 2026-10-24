using System.Numerics;
using ImGuiNET;

namespace RageLightEditor.Editor
{
    public partial class LightPanel
    {
        public bool BrushSelect_U37;
        public float BrushRadius_U37 = 40f;
        public Vector2? BrushCursor_U37;

        private void DrawBrushCursor_U37()
        {
            if (!BrushSelect_U37 || BrushCursor_U37 == null || WorldMode || MloMode || MaterialMode) return;
            var dl = ImGui.GetForegroundDrawList();
            var c = BrushCursor_U37.Value;
            bool alt = ImGui.GetIO().KeyAlt;
            uint col = alt ? 0xE05A5AF0u : 0xE05FCB8Bu;
            dl.AddCircle(c, BrushRadius_U37, 0xC0000000u, 48, 3.0f);
            dl.AddCircle(c, BrushRadius_U37, col, 48, 1.5f);
        }

        private uint bulkFlagMask_U35;
        private int bulkFlagType_U35;
        private string bulkFlagStatus_U35;

        private static readonly string[] BulkTypeNames_U35 = { "All lights", "Point lights", "Spot lights", "Capsule lights" };

        private void DrawLightTypes_U35()
        {
            ImGui.TextDisabled("SHOW");
            for (int t = 0; t < 3; t++)
            {
                bool on = scene.ShowType_U35[t];
                if (ImGui.Checkbox($"{Scene.TypeNames_U35[t]}##lt_show{t}", ref on)) scene.ShowType_U35[t] = on;
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip($"Off: {Scene.TypeNames_U35[t].ToLowerInvariant()} lights stop lighting the viewport and lose their markers,\nand a click can't pick them. Nothing in your files changes.");
                ImGui.SameLine(130);
                int count = scene.CountType_U35(t);
                ImGui.TextDisabled($"{count}");
                ImGui.SameLine(175);
                if (count == 0) ImGui.BeginDisabled();
                if (ImGui.SmallButton($"Select all##lt_sel{t}"))
                {
                    if (!scene.ShowType_U35[t]) scene.ShowType_U35[t] = true;
                    int n = scene.SelectType_U35(t);
                    bulkFlagStatus_U35 = $"Selected {n} {Scene.TypeNames_U35[t].ToLowerInvariant()} light{(n == 1 ? "" : "s")} - Light parameters now edits them all together.";
                }
                if (count == 0) ImGui.EndDisabled();
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip($"Select every {Scene.TypeNames_U35[t].ToLowerInvariant()} light. Then any value you change in Light parameters\nis set on all of them; their other values stay as they are.");
                ImGui.SameLine();
                int selOfType = 0;
                foreach (var si in scene.SelectedIndices)
                    if (si >= 0 && si < scene.Lights.Count && scene.Lights[si] != null && Scene.TypeSlot_U35(scene.Lights[si].Type) == t) selOfType++;
                if (selOfType == 0) ImGui.BeginDisabled();
                if (ImGui.SmallButton($"Deselect##lt_desel{t}"))
                {
                    int n = scene.DeselectType_U35(t);
                    bulkFlagStatus_U35 = $"Took {n} {Scene.TypeNames_U35[t].ToLowerInvariant()} light{(n == 1 ? "" : "s")} out of the selection.";
                }
                if (selOfType == 0) ImGui.EndDisabled();
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip($"Take every {Scene.TypeNames_U35[t].ToLowerInvariant()} light out of the selection ({selOfType} selected now).\nThe other types stay selected.");
            }

            ImGui.Spacing();
            ImGui.TextDisabled("SELECTION");
            float half = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) * 0.5f;
            bool any = scene.SelectedIndices.Count > 0;
            if (!any) ImGui.BeginDisabled();
            if (ImGui.Button($"Deselect all ({scene.SelectedIndices.Count})##lt_desel", new Vector2(half, 0)))
            {
                scene.SelectedIndices.Clear();
                bulkFlagStatus_U35 = "Nothing selected.";
            }
            if (!any) ImGui.EndDisabled();
            ImGui.SameLine();
            bool brush = BrushSelect_U37;
            if (brush) ImGui.PushStyleColor(ImGuiCol.Button, UiTheme.Accent);
            if (ImGui.Button((brush ? "Brush select: on" : "Brush select") + "##lt_brush", new Vector2(half, 0))) BrushSelect_U37 = !BrushSelect_U37;
            if (brush) ImGui.PopStyleColor();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Drag with the left mouse button over lights to add them to the selection.\nHold Alt while dragging to take them out again. Turn it off to turn the camera with the left button.");
            if (BrushSelect_U37)
            {
                ImGui.SetNextItemWidth(-90);
                UiSlider_U28.Float("Brush size##lt_brushr", ref BrushRadius_U37, 10f, 200f, "%.0f px");
            }
            ImGui.TextDisabled("Ctrl + right-click a light adds or removes just that one.");

            ImGui.Spacing();
            ImGui.TextDisabled("MARKERS");
            bool fade = settings.MarkerFadeU36;
            if (ImGui.Checkbox("Fade with distance##lt_fade", ref fade)) { settings.MarkerFadeU36 = fade; settings.Save(); }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("On: markers fade out between 15 and 35 m, so you mostly see the room you're in.\nOff: every marker shows at full strength however far it is.");
            bool occ = settings.MarkerOccludeU36;
            if (ImGui.Checkbox("Hide behind walls##lt_occ", ref occ)) { settings.MarkerOccludeU36 = occ; settings.Save(); }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("On: a marker only shows where you can see the light.\nOff: markers draw on top of everything, through walls and floors.");

            ImGui.Spacing();
            ImGui.TextDisabled("SHADOWS");
            DrawAutoShadows_U45();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.TextDisabled("FLAGS ON EVERY LIGHT OF A TYPE");
            ImGui.SetNextItemWidth(-1);
            ImGui.Combo("##lt_type", ref bulkFlagType_U35, BulkTypeNames_U35, BulkTypeNames_U35.Length);
            ImGui.Columns(2, "lt_flagcols", false);
            foreach (var fd in LightDefs.Flags)
            {
                bool set = (bulkFlagMask_U35 & (1u << fd.Bit)) != 0;
                if (ImGui.Checkbox($"{fd.Name}##lt_flag{fd.Bit}", ref set))
                    bulkFlagMask_U35 = set ? bulkFlagMask_U35 | (1u << fd.Bit) : bulkFlagMask_U35 & ~(1u << fd.Bit);
                if (!string.IsNullOrEmpty(fd.Tooltip) && ImGui.IsItemHovered())
                    ImGui.SetTooltip($"bit {fd.Bit} (0x{1u << fd.Bit:X}): {fd.Tooltip}");
                ImGui.NextColumn();
            }
            ImGui.Columns(1);

            int slot = bulkFlagType_U35 - 1;
            int targets = scene.CountType_U35(slot);
            bool can = bulkFlagMask_U35 != 0 && targets > 0;
            if (!can) ImGui.BeginDisabled();
            float w = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) * 0.5f;
            if (ImGui.Button($"Turn on ({targets})##lt_on", new Vector2(w, 0))) ApplyBulkFlags_U35(slot, true);
            ImGui.SameLine();
            if (ImGui.Button($"Turn off ({targets})##lt_off", new Vector2(w, 0))) ApplyBulkFlags_U35(slot, false);
            if (!can) ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip("Tick the flags above, pick which lights, then turn them on or off for all of them at once.\nOne Undo puts every light back.");
            if (!string.IsNullOrEmpty(bulkFlagStatus_U35)) ImGui.TextWrapped(bulkFlagStatus_U35);
        }

        private void ApplyBulkFlags_U35(int slot, bool on)
        {
            int n = scene.SetFlagsForType_U35(slot, bulkFlagMask_U35, on);
            FlagsChanged = true;
            string which = BulkTypeNames_U35[slot + 1].ToLowerInvariant();
            bulkFlagStatus_U35 = n == 0
                ? $"Nothing changed - every one of the {which} already had that."
                : $"Turned {(on ? "on" : "off")} for {n} of the {which}. Save to keep it.";
        }
    }
}
