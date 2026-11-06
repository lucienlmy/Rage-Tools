using System.Linq;
using ImGuiNET;

namespace RageLightEditor.Editor
{
    public partial class LightPanel
    {
        private LoadedFile moveHereTarget_U56;
        private bool openMoveHere_U56;

        public void OfferMoveHere_U56(LoadedFile target)
        {
            moveHereTarget_U56 = target;
            openMoveHere_U56 = target != null;
        }

        public bool MoveHereOpen_U56 => moveHereTarget_U56 != null;

        private void DrawMoveHerePopup_U56()
        {
            if (openMoveHere_U56)
            {
                ImGui.OpenPopup("##u56movehere");
                openMoveHere_U56 = false;
            }
            if (!ImGui.BeginPopup("##u56movehere"))
            {
                moveHereTarget_U56 = null;
                return;
            }
            var f = moveHereTarget_U56;
            var sel = scene.SelectedLightList_V35();
            int n = sel.Count(l => !ReferenceEquals(scene.OwnerFile(l), f));
            ImGui.TextDisabled(f?.Name ?? "?");
            ImGui.Separator();
            string what = n == 1 ? "the selected light" : $"the {n} selected lights";
            if (n == 0) ImGui.BeginDisabled();
            if (ImGui.MenuItem($"Move {what} here"))
            {
                MoveHere_U56(f, true);
                ImGui.CloseCurrentPopup();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("They stay exactly where they are and are saved into this prop from now on.\n" +
                                 "If the prop is placed more than once, every copy gets them.");
            if (ImGui.MenuItem($"Move {what} here, same spot on the prop"))
            {
                MoveHere_U56(f, false);
                ImGui.CloseCurrentPopup();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("They keep their offset on the prop - for copying a lamp's lights onto an identical lamp.");
            if (n == 0) ImGui.EndDisabled();
            ImGui.Separator();
            if (ImGui.MenuItem("Select this prop"))
            {
                scene.SelectFile(f, false, false);
                ScrollToActiveProp = true;
                MloStatus = "Selected " + f?.Name;
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        private void MoveHere_U56(LoadedFile f, bool keepWorld)
        {
            if (f == null) return;
            scene.PushUndo();
            int moved = scene.MoveLightsToProp_V35(scene.SelectedLightList_V35(), f, keepWorld);
            scene.SelectFile(f, false, false);
            ScrollToActiveProp = true;
            MloStatus = moved > 0
                ? $"Moved {moved} light(s) onto {f.Name}" + (keepWorld ? " - they stayed where they were." : " - they kept their offset on the prop.")
                : "Nothing moved - they are already on that prop.";
        }
    }
}
