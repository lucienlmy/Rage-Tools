using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CodeWalker.GameFiles;
using ImGuiNET;

namespace RageLightEditor.Editor
{
    public partial class LightPanel
    {
        public Func<uint, bool> ProjTexInGame_U51;
        private string projTexSearch_U51 = "";
        private const float WideConeDegrees_U51 = 80f;

        private void ApplyProjectedTexture_U51(LightAttributes l, string name)
        {
            scene.PushUndo();
            if (string.IsNullOrWhiteSpace(name))
            {
                l.ProjectedTextureHash = new MetaHash(0);
                l.Flags &= ~LightDefs.FlagTextureProjection;
            }
            else
            {
                var clean = name.Trim().ToLowerInvariant();
                JenkIndex.Ensure(clean);
                l.ProjectedTextureHash = new MetaHash(JenkHash.GenHash(clean));
                l.Flags |= LightDefs.FlagTextureProjection;
            }
            scene.Dirty = true;
        }

        private static string ProjectedTextureLabel_U51(uint hash, List<string> names)
        {
            if (hash == 0) return "(none)";
            foreach (var n in names)
                if (JenkHash.GenHash(n.ToLowerInvariant()) == hash) return n;
            var known = JenkIndex.TryGetString(hash);
            return string.IsNullOrEmpty(known) ? $"hash {hash:X8}" : known;
        }

        private void DrawProjectedTextureCombo_U51(LightAttributes l, List<string> names)
        {
            uint pth = l.ProjectedTextureHash.Hash;
            if (ImGui.BeginCombo("Projected texture", ProjectedTextureLabel_U51(pth, names), ImGuiComboFlags.HeightLarge))
            {
                if (ImGui.IsWindowAppearing())
                {
                    projTexSearch_U51 = "";
                    ImGui.SetKeyboardFocusHere();
                }
                ImGui.SetNextItemWidth(-1);
                bool enter = ImGui.InputTextWithHint("##u51search", "type a texture name...", ref projTexSearch_U51, 128, ImGuiInputTextFlags.EnterReturnsTrue);
                var typed = projTexSearch_U51.Trim();
                var shown = typed.Length == 0 ? names : names.Where(n => n.IndexOf(typed, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                if (enter && typed.Length > 0)
                {
                    ApplyProjectedTexture_U51(l, typed);
                    ImGui.CloseCurrentPopup();
                }

                if (typed.Length > 0 && !names.Any(n => string.Equals(n, typed, StringComparison.OrdinalIgnoreCase)))
                {
                    uint h = JenkHash.GenHash(typed.ToLowerInvariant());
                    bool inGame = ProjTexInGame_U51?.Invoke(h) == true;
                    if (ImGui.Selectable($"Use \"{typed}\" ({(inGame ? "in the game files" : "not found")})##u51use"))
                        ApplyProjectedTexture_U51(l, typed);
                    ImGui.Separator();
                }
                if (ImGui.Selectable("(none)", pth == 0)) ApplyProjectedTexture_U51(l, null);
                foreach (var n in shown)
                    if (ImGui.Selectable(n, JenkHash.GenHash(n.ToLowerInvariant()) == pth)) ApplyProjectedTexture_U51(l, n);
                ImGui.EndCombo();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Pattern projected by spot and point lights (sets flag 5).\nOpen it and type a name: the list narrows as you type, and Enter uses what you typed.\nIn game the texture must be in the prop's own texture dictionary (its .ytd or a parent of it).");
        }

        private static readonly string projComboProbe_U51 = Environment.GetEnvironmentVariable("RLE_PROJCOMBO");
        private int projComboProbeFrame_U51;

        private void DrawProjComboProbe_U51()
        {
            if (string.IsNullOrEmpty(projComboProbe_U51) || scene.SelectedLight == null) return;
            ImGui.SetNextWindowPos(new Vector2(420, 120), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(460, 90), ImGuiCond.Always);
            if (ImGui.Begin("Projected texture probe##u51"))
            {
                int f = projComboProbeFrame_U51++;
                if (f == 0)
                {
                    ImGui.PushID("Projected texture");
                    uint pid = ImGui.GetID("##ComboPopup");
                    ImGui.PopID();
                    ImGui.OpenPopup(pid);
                }
                var pb = projComboProbe_U51.Split('|');
                if (f == 3) ImGui.GetIO().AddInputCharactersUTF8(pb[0]);
                if (pb.Length > 1 && f == 4) ImGui.GetIO().AddKeyEvent(ImGuiKey.Enter, true);
                if (pb.Length > 1 && f == 5) ImGui.GetIO().AddKeyEvent(ImGuiKey.Enter, false);
                if (f >= 3) Console.WriteLine($"PROJCOMBO frame {f} search '{projTexSearch_U51}' light texture {scene.SelectedLight.ProjectedTextureHash.Hash:X8} flags 0x{scene.SelectedLight.Flags:X}");
                DrawProjectedTextureCombo_U51(scene.SelectedLight, scene.GetTextureNames());
            }
            ImGui.End();
        }

        private void DrawProjectedTextureStatus_U51(LightAttributes l)
        {
            uint h = l.ProjectedTextureHash.Hash;
            if (h == 0) return;
            if (l.Type == LightType.Capsule)
            {
                ImGui.TextColored(new Vector4(1f, 0.65f, 0.3f, 1f), "Capsule lights can't project a texture - the game ignores it.");
                return;
            }
            if (l.Type == LightType.Spot && Math.Max(l.ConeInnerAngle, l.ConeOuterAngle) >= WideConeDegrees_U51)
                ImGui.TextColored(new Vector4(1f, 0.65f, 0.3f, 1f), "Outer cone too wide to see a pattern: at 90 the whole light uses only the texture's centre pixel. Try 60 or less.");
            bool loaded = scene.FindTexture(h) != null;
            if (loaded) ImGui.TextDisabled("Showing the texture from your loaded dictionaries.");
            else if (ProjTexInGame_U51?.Invoke(h) == true) ImGui.TextDisabled("Showing the texture from the game files.");
            else ImGui.TextColored(new Vector4(1f, 0.65f, 0.3f, 1f), "Texture not found - load the .ytd that holds it to see it here.");
        }
    }
}
