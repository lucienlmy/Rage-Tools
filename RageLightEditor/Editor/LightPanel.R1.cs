using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace RageLightEditor.Editor
{
    public partial class LightPanel
    {
        public string RequestRemoveImportedYtyp_R1;
        public string RequestToggleImport_U42;

        private void DrawImportedYtyps_R1(Scene sc)
        {
            var imports = sc?.Imports_R1?.Entries;
            if (imports == null || imports.Count == 0) return;
            ImGui.Spacing();
            ImGui.TextDisabled($"IMPORTED .YTYPS ({imports.Count})");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Imports add to each other - an interior, its light layer and its props are\n" +
                                 "separate files. Remove takes one back out on its own; Clear takes them all.");
            for (int i = 0; i < imports.Count; i++)
            {
                var e = imports[i];
                if (e == null) continue;
                ImGui.PushID("r1lpimp" + i);
                bool en = e.Enabled;
                if (ImGui.Checkbox("##u42on", ref en)) RequestToggleImport_U42 = e.Path;
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(e.Enabled
                        ? $"Turn {e.Name} off: its walls, entities and lights are hidden but stay loaded.\nTick it again to bring it straight back. Remove takes it out for good."
                        : $"Turn {e.Name} back on.");
                ImGui.SameLine();
                if (DangerButton("Remove", new Vector2(70, 0))) RequestRemoveImportedYtyp_R1 = e.Path;
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip($"Take {e.Name} out of the scene: its archetypes, its entities and the props\n" +
                                     "only it brought. A prop another import also places stays.");
                ImGui.SameLine();
                if (e.Enabled) ImGui.TextUnformatted(e.Name);
                else ImGui.TextDisabled(e.Name + "  (off)");
                if (ImGui.IsItemHovered())
                {
                    var archs = string.Join(", ", e.ArchetypeNames.Take(6));
                    ImGui.SetTooltip(e.Path + "\n" + $"{e.MeshCount} meshes, {e.Props.Count} props, {e.EntityCount} placed" +
                                     (string.IsNullOrEmpty(archs) ? "" : "\narchetypes: " + archs));
                }
                ImGui.PopID();
            }
        }
    }
}

