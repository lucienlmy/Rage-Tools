using System;
using System.Collections.Generic;
using System.Linq;

namespace RageLightEditor.Editor
{
    public partial class Scene
    {
        public List<LoadedFile> AutoSave_U54(List<string> errors)
        {
            var saved = new List<LoadedFile>();
            if (ExternalEditing) return saved;
            MarkChangedFiles_U47();
            foreach (var f in Files.ToArray())
            {
                if (f == null || f.ReadOnly || !f.Dirty || IsUnsaved(f)) continue;
                try
                {
                    SaveFile(f, f.Path);
                    saved.Add(f);
                }
                catch (Exception ex) { errors?.Add(f.Name + ": " + ex.Message); }
            }
            return saved;
        }
    }
}
