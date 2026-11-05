using ImGuiNET;

namespace RageLightEditor.Editor
{
    public partial class LightPanel
    {
        public bool AutoSaveLights_U54
        {
            get => settings.AutoSaveLights_U54;
            set { settings.AutoSaveLights_U54 = value; settings.Save(); }
        }

        private void DrawAutoSaveMenuItem_U54()
        {
            if (ImGui.MenuItem("Auto save lights every 30 s", null, AutoSaveLights_U54)) AutoSaveLights_U54 = !AutoSaveLights_U54;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Every 30 seconds the Lights page writes the props whose lights you changed,\n" +
                                 "back into their own files. It never makes new files and waits while you drag.");
        }
    }
}
