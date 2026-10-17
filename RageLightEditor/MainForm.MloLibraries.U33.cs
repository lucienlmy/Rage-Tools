using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeWalker.GameFiles;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private List<string> propLibrariesExtraWas_U33;

        internal static bool YtypHasInterior_U33(string path)
        {
            try
            {
                var y = new YtypFile();
                y.Load(File.ReadAllBytes(path));
                return y.AllArchetypes?.Any(a => a is MloArchetype) == true;
            }
            catch { return false; }
        }

        internal static string[] SplitPropLibraries_U33(string[] paths, out List<string> libraries)
        {
            libraries = new List<string>();
            if (paths == null || paths.Length < 2) return paths;
            var interiors = paths.Where(YtypHasInterior_U33).ToArray();
            if (interiors.Length == 0 || interiors.Length == paths.Length) return paths;
            libraries.AddRange(paths.Except(interiors));
            return interiors;
        }

        private void UsePropLibraries_U33(List<string> libraries)
        {
            propLibrariesExtraWas_U33 = null;
            if (libraries == null || libraries.Count == 0) return;
            propLibrariesExtraWas_U33 = mloImporter.ExtraFolders;
            mloImporter.ExtraFolders = (propLibrariesExtraWas_U33 ?? new List<string>())
                .Concat(libraries.Select(Path.GetDirectoryName))
                .Where(d => !string.IsNullOrEmpty(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void RestorePropLibraries_U33()
        {
            if (propLibrariesExtraWas_U33 == null) return;
            mloImporter.ExtraFolders = propLibrariesExtraWas_U33;
            propLibrariesExtraWas_U33 = null;
        }
    }
}
