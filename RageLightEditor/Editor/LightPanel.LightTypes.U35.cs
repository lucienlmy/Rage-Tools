using System.Numerics;
using ImGuiNET;

namespace RageLightEditor.Editor
{
    public partial class LightPanel
    {
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
                ImGui.TextDisabled($"{scene.CountType_U35(t)}");
            }

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
