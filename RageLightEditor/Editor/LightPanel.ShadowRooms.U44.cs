using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CodeWalker.GameFiles;
using ImGuiNET;

namespace RageLightEditor.Editor
{
    public sealed class RoomLights_U44
    {
        public const int Limit = 32;
        public const uint ShadowMask = 0x40u | 0x80u | 0x100u | 0x1000000u;
        public static bool CastsShadow(LightAttributes l) => (l.Flags & ShadowMask) != 0;

        public string Interior = "";
        public int Room;
        public string Name = "";
        public int Lights;
        public int Shadow;
        public bool HasBox;
        public SharpDX.BoundingBox Box;
    }

    public partial class LightPanel
    {
        public List<RoomLights_U44> ShadowRooms_U44 = new List<RoomLights_U44>();
        public RoomLights_U44 CameraShadowRoom_U44;

        private static readonly Vector4 OverCol_U44 = new Vector4(1.0f, 0.42f, 0.42f, 1f);
        private static readonly Vector4 OkCol_U44 = new Vector4(0.45f, 0.85f, 0.55f, 1f);

        public bool RequestAutoShadows_U45;
        public string AutoShadowsStatus_U45;

        private void DrawAutoShadows_U45()
        {
            int spots = scene.CountType_U35(1);
            if (spots == 0) ImGui.BeginDisabled();
            if (ImGui.Button($"Auto shadows for spot lights ({spots})##u45auto", new Vector2(-1, 0))) RequestAutoShadows_U45 = true;
            if (spots == 0) ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip("Turns shadows on for the spot lights that need them and off for the rest.\n" +
                                 "Needs shadows: reaches at least 2 m and isn't dim. The strongest go first, and a room never goes over " + RoomLights_U44.Limit + " shadow lights.\n" +
                                 "Point and capsule lights are not touched. One Undo puts everything back.");
            if (!string.IsNullOrEmpty(AutoShadowsStatus_U45)) ImGui.TextWrapped(AutoShadowsStatus_U45);
        }

        private void DrawShadowRooms_U44()
        {
            int over = ShadowRooms_U44.Count(e => e.Shadow > RoomLights_U44.Limit);
            ImGui.TextWrapped($"GTA V draws shadows for up to {RoomLights_U44.Limit} lights at once. Rooms with more shadow lights than that are red.");
            if (over > 0) ImGui.TextColored(OverCol_U44, $"{over} room{(over == 1 ? " is" : "s are")} over the limit");
            else ImGui.TextColored(OkCol_U44, "Every room is within the limit");

            var here = CameraShadowRoom_U44;
            if (here != null)
            {
                ImGui.TextDisabled("You are in:");
                ImGui.SameLine();
                ImGui.TextColored(here.Shadow > RoomLights_U44.Limit ? OverCol_U44 : OkCol_U44, $"{here.Name} - {here.Shadow}/{RoomLights_U44.Limit} shadow lights");
            }

            bool many = ShadowRooms_U44.Select(e => e.Interior).Distinct().Count() > 1;
            var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp;
            float h = System.Math.Min(ShadowRooms_U44.Count + 1, 14) * ImGui.GetTextLineHeightWithSpacing() + 8;
            if (ImGui.BeginTable("##u44rooms", 3, flags, new Vector2(0, h)))
            {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn("Room", ImGuiTableColumnFlags.WidthStretch, 3f);
                ImGui.TableSetupColumn("Lights", ImGuiTableColumnFlags.WidthStretch, 1f);
                ImGui.TableSetupColumn("Shadows", ImGuiTableColumnFlags.WidthStretch, 1f);
                ImGui.TableHeadersRow();
                foreach (var e in ShadowRooms_U44.OrderByDescending(e => e.Shadow).ThenByDescending(e => e.Lights))
                {
                    bool bad = e.Shadow > RoomLights_U44.Limit;
                    bool me = ReferenceEquals(e, here);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    string name = (many ? e.Interior + " / " : "") + e.Name + (me ? "  (you)" : "");
                    if (bad) ImGui.TextColored(OverCol_U44, name); else ImGui.TextUnformatted(name);
                    ImGui.TableNextColumn();
                    ImGui.TextDisabled(e.Lights.ToString());
                    ImGui.TableNextColumn();
                    if (bad) ImGui.TextColored(OverCol_U44, $"{e.Shadow}"); else ImGui.TextUnformatted($"{e.Shadow}");
                }
                ImGui.EndTable();
            }
            DrawAutoShadows_U45();
        }
    }
}
