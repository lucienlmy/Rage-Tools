using System.Collections.Generic;
using System.Numerics;
using CodeWalker.GameFiles;
using ImGuiNET;

namespace RageLightEditor.Editor
{
    public sealed class WorldEditedModel_U32
    {
        public uint Hash;
        public string Name;
        public DrawableBase Drawable;
        public string SavedPath;
        public bool Unsaved;
        public int Lights;
        public string Ext => WorldLights.SaveExtension(Drawable);
        public string FileName => Name + Ext;
        public bool CanSave => WorldLights.CanSave(Drawable);
    }

    public partial class LightPanel
    {
        public readonly List<WorldEditedModel_U32> WorldEditedModels_U32 = new List<WorldEditedModel_U32>();
        public uint RequestSaveEditedModel_U32;
        public bool RequestSaveEditedModels_U32;
        public string WorldEditedModelsStatus_U32;

        public int WorldEditedModelsUnsaved_U32
        {
            get
            {
                int n = 0;
                foreach (var m in WorldEditedModels_U32) if (m.Unsaved && m.CanSave) n++;
                return n;
            }
        }

        private void DrawWorldEditedModels_U32()
        {
            var list = WorldEditedModels_U32;
            if (list.Count == 0) return;
            int unsaved = WorldEditedModelsUnsaved_U32;
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.TextDisabled($"EDITED MODELS ({list.Count})");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Every .ydr / .yft whose lights you changed in the world.\nSave them one by one, or all at once.");
            const int maxRows = 8;
            bool scrolls = list.Count > maxRows;
            if (scrolls) ImGui.BeginChild("##editedmodels", new Vector2(0, ImGui.GetFrameHeightWithSpacing() * maxRows + 4), ImGuiChildFlags.None);
            foreach (var m in list)
            {
                ImGui.PushID(unchecked((int)m.Hash));
                bool can = m.Unsaved && m.CanSave;
                if (!can) ImGui.BeginDisabled();
                if (ImGui.Button("Save")) RequestSaveEditedModel_U32 = m.Hash;
                if (!can) ImGui.EndDisabled();
                ImGui.SameLine();
                if (m.Unsaved) ImGui.TextColored(UiTheme.Warn, m.FileName + "  *");
                else ImGui.TextDisabled(m.FileName);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip((m.Unsaved ? "Unsaved light changes" : "Saved") + $"  -  {m.Lights} light{(m.Lights == 1 ? "" : "s")}" +
                                     (string.IsNullOrEmpty(m.SavedPath) ? "" : "\n" + m.SavedPath) +
                                     (m.CanSave ? "" : "\nThis model cannot be written as a loose file."));
                ImGui.PopID();
            }
            if (scrolls) ImGui.EndChild();
            if (unsaved == 0) ImGui.BeginDisabled();
            if (ImGui.Button(unsaved > 0 ? $"Save all models ({unsaved})" : "Save all models", new Vector2(-1, 0))) RequestSaveEditedModels_U32 = true;
            if (unsaved == 0) ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip("Writes every unsaved model. A model saved before goes back to its own file;\nthe others go to the output folder below (you are asked for one if none is set).");
            if (!string.IsNullOrEmpty(WorldEditedModelsStatus_U32)) ImGui.TextWrapped(WorldEditedModelsStatus_U32);
        }
    }
}
