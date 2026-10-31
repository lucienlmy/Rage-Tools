using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace RageLightEditor.Editor
{
    public partial class LightPanel
    {
        public Func<List<GameMloInfo_U49>> GameMloSource_U49;
        public bool ShowGameMlos_U49;
        public GameMloInfo_U49 RequestImportGameMlo_U49;
        private string gameMloFilter_U49 = "";
        private uint gameMloPicked_U49;

        private void GameMloButton_U49(bool small)
        {
            bool click = small ? ImGui.SmallButton("GTA V interiors...") : ImGui.Button("GTA V interiors...");
            if (click) ShowGameMlos_U49 = true;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Pick any interior (MLO) that comes with GTA V from a list and import it,\njust like Import MLO does with your own .ytyp.");
        }

        private void DrawGameMloWindow_U49()
        {
            if (!ShowGameMlos_U49) return;
            ImGui.SetNextWindowSize(new Vector2(520, 560), ImGuiCond.FirstUseEver);
            if (!ImGui.Begin("GTA V interiors##u49", ref ShowGameMlos_U49, ImGuiWindowFlags.NoCollapse)) { ImGui.End(); return; }

            var all = GameMloSource_U49?.Invoke();
            if (all == null)
            {
                ImGui.TextWrapped("The game files aren't open yet. Set your GTA V folder and wait for 'Game files ready'.");
                ImGui.End();
                return;
            }

            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##u49filter", "search interiors (v_int_, hei_, apa_, ...)", ref gameMloFilter_U49, 96);
            var f = gameMloFilter_U49.Trim();
            var shown = f.Length == 0 ? all : all.Where(m => m.Name.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                             m.Ytyp.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            ImGui.TextDisabled($"{shown.Count} of {all.Count} interiors");

            float listH = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() * 1.4f;
            var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp;
            GameMloInfo_U49 picked = null;
            if (ImGui.BeginTable("##u49list", 3, flags, new Vector2(0, Math.Max(listH, 120))))
            {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn("Interior", ImGuiTableColumnFlags.WidthStretch, 3f);
                ImGui.TableSetupColumn("Rooms", ImGuiTableColumnFlags.WidthStretch, 0.8f);
                ImGui.TableSetupColumn(".ytyp", ImGuiTableColumnFlags.WidthStretch, 2.2f);
                ImGui.TableHeadersRow();
                unsafe
                {
                    var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
                    clipper.Begin(shown.Count);
                    while (clipper.Step())
                    {
                        for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                        {
                            var m = shown[i];
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            bool sel = m.Hash == gameMloPicked_U49;
                            if (ImGui.Selectable($"{m.Name}##u49r{m.Hash}", sel, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick))
                            {
                                gameMloPicked_U49 = m.Hash;
                                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) RequestImportGameMlo_U49 = m;
                            }
                            ImGui.TableNextColumn();
                            ImGui.TextDisabled(m.Rooms.ToString());
                            ImGui.TableNextColumn();
                            ImGui.TextDisabled(m.Ytyp);
                        }
                    }
                    clipper.End();
                    clipper.Destroy();
                }
                ImGui.EndTable();
            }
            picked = all.FirstOrDefault(m => m.Hash == gameMloPicked_U49);

            if (picked == null) ImGui.BeginDisabled();
            if (ImGui.Button(picked != null ? $"Import {picked.Name}" : "Import", new Vector2(-1, 0))) RequestImportGameMlo_U49 = picked;
            if (picked == null) ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip("Adds the interior to the scene (double-click a row does the same).\nIts props with lights become editable, like Import MLO.");
            ImGui.End();
        }
    }
}
