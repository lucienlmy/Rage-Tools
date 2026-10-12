using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CodeWalker.GameFiles;
using RageLightEditor.Editor;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private readonly Dictionary<uint, WorldEditedModel_U32> editedModels_U32 = new Dictionary<uint, WorldEditedModel_U32>();

        private void MarkLightUnsaved_U32(uint hash)
        {
            worldLightUnsaved.Add(hash);
            TrackEditedModel_U32(hash, worldRender.Lights.GetDrawable(hash), null);
        }

        private WorldEditedModel_U32 TrackEditedModel_U32(uint hash, DrawableBase db, string name)
        {
            if (hash == 0 || db == null) return null;
            if (!editedModels_U32.TryGetValue(hash, out var m))
            {
                var arch = ProjWin?.Project?.FindArchetype(new MetaHash(hash), gameFiles?.Cache) ?? gameFiles?.Cache?.GetArchetype(hash);
                m = new WorldEditedModel_U32 { Hash = hash, Name = name ?? arch?.Name ?? new MetaHash(hash).ToString() };
                editedModels_U32[hash] = m;
            }
            m.Drawable = db;
            return m;
        }

        internal void MarkEditedForTest_U32(uint hash, DrawableBase db, string name)
        {
            if (TrackEditedModel_U32(hash, db, name) != null) worldLightUnsaved.Add(hash);
        }

        internal void ForgetEdited_U32(uint hash)
        {
            editedModels_U32.Remove(hash);
            worldLightUnsaved.Remove(hash);
            worldLightSavedPath.Remove(hash);
            panel.WorldEditedModelsStatus_U32 = null;
        }

        private void ServiceEditedModels_U32()
        {
            var list = panel.WorldEditedModels_U32;
            if (list.Count != editedModels_U32.Count) { list.Clear(); list.AddRange(editedModels_U32.Values); }
            foreach (var m in list)
            {
                m.Unsaved = worldLightUnsaved.Contains(m.Hash);
                worldLightSavedPath.TryGetValue(m.Hash, out m.SavedPath);
                m.Lights = WorldLights.LightsOf(m.Drawable)?.Length ?? 0;
            }
            if (panel.RequestSaveEditedModel_U32 != 0)
            {
                uint hash = panel.RequestSaveEditedModel_U32;
                panel.RequestSaveEditedModel_U32 = 0;
                if (editedModels_U32.TryGetValue(hash, out var m)) SaveEditedModelAsk_U32(m);
            }
            if (panel.RequestSaveEditedModels_U32)
            {
                panel.RequestSaveEditedModels_U32 = false;
                SaveEditedModelsAsk_U32();
            }
        }

        private static bool HasFile_U32(WorldEditedModel_U32 m) => !string.IsNullOrEmpty(m.SavedPath) && File.Exists(m.SavedPath);

        private void SaveEditedModelAsk_U32(WorldEditedModel_U32 m)
        {
            string path = HasFile_U32(m) ? m.SavedPath : null;
            if (path == null)
            {
                using var dlg = new SaveFileDialog
                {
                    Filter = m.Ext == ".yft" ? "YFT fragment (*.yft)|*.yft" : "YDR drawable (*.ydr)|*.ydr",
                    FileName = m.FileName,
                    Title = "Save " + m.Name,
                };
                if (!string.IsNullOrEmpty(WorldEdit.OutputFolder) && Directory.Exists(WorldEdit.OutputFolder)) dlg.InitialDirectory = WorldEdit.OutputFolder;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                path = dlg.FileName;
            }
            string err = SaveEditedModel_U32(m, path);
            panel.WorldEditedModelsStatus_U32 = err ?? "saved " + Path.GetFileName(m.SavedPath);
        }

        private void SaveEditedModelsAsk_U32()
        {
            string folder = WorldEdit.OutputFolder;
            bool needFolder = editedModels_U32.Values.Any(m => m.Unsaved && m.CanSave && !HasFile_U32(m));
            if (needFolder && (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)))
            {
                using var dlg = new FolderBrowserDialog { Description = "Folder to save the edited models (.ydr / .yft) in", UseDescriptionForTitle = true };
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                folder = dlg.SelectedPath;
            }
            SaveEditedModels_U32(folder);
        }

        internal int SaveEditedModels_U32(string folder)
        {
            int done = 0;
            var failed = new List<string>();
            foreach (var m in editedModels_U32.Values.ToList())
            {
                if (!m.Unsaved || !m.CanSave) continue;
                string path = HasFile_U32(m) ? m.SavedPath : string.IsNullOrEmpty(folder) ? null : Path.Combine(folder, m.FileName);
                string err = path == null ? "no folder" : SaveEditedModel_U32(m, path);
                if (err == null) done++; else failed.Add(m.FileName + " (" + err + ")");
            }
            panel.WorldEditedModelsStatus_U32 = failed.Count == 0
                ? $"saved {done} model{(done == 1 ? "" : "s")}"
                : $"saved {done}, failed {failed.Count}: " + string.Join(", ", failed);
            return done;
        }

        internal string SaveEditedModel_U32(WorldEditedModel_U32 m, string path)
        {
            if (m?.Drawable == null) return "the model is not loaded";
            if (!m.CanSave) return "this model cannot be written as a loose file";
            try
            {
                path = WorldLights.SaveDrawableAs(m.Drawable, path);
                worldLightSavedPath[m.Hash] = path;
                worldLightUnsaved.Remove(m.Hash);
                m.SavedPath = path;
                m.Unsaved = false;
                Console.WriteLine($"EDITEDMODEL wrote {path}");
                return null;
            }
            catch (Exception ex) { return ex.Message; }
        }
    }
}
