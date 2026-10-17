using System;
using System.IO;
using System.Linq;
using CodeWalker.GameFiles;
using RageLightEditor.Editor;
using SharpDX;

namespace RageLightEditor
{
    public partial class MainForm
    {
        private void MloLibrariesTest_U33(Action<string, bool, string> check)
        {
            string dir = Path.Combine(Path.GetTempPath(), "rle_u33_libs");
            try
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);
                var lib = ArchetypeBuilder.BuildYtyp("rle_u33_props", new[] { new ArchetypeDef { Name = "rle_u33_can", TextureDict = "rle_u33_can", BbMin = new Vector3(-0.1f), BbMax = new Vector3(0.1f), BsRadius = 0.2f } });
                string libPath = Path.Combine(dir, "rle_u33_props.ytyp");
                File.WriteAllBytes(libPath, lib.Save());

                var ytyp = new YtypFile { Name = "rle_u33_int" };
                var mlo = new MloArchetype();
                var mdef = new CMloArchetypeDef();
                uint h = JenkHash.GenHash("rle_u33_int");
                mdef._BaseArchetypeDef.name = h; mdef._BaseArchetypeDef.assetName = h;
                mdef._BaseArchetypeDef.assetType = rage__fwArchetypeDef__eAssetType.ASSET_TYPE_ASSETLESS;
                mdef._BaseArchetypeDef.bbMin = new Vector3(-4, -4, -1); mdef._BaseArchetypeDef.bbMax = new Vector3(4, 4, 4);
                mlo.Init(ytyp, ref mdef);
                ytyp.AllArchetypes = new Archetype[] { mlo };
                MloEditor.AddRoom(mlo, "limbo");
                string intPath = Path.Combine(dir, "rle_u33_int.ytyp");
                File.WriteAllBytes(intPath, ytyp.Save());

                check("mlo libraries: an interior .ytyp is told apart from a prop library",
                      YtypHasInterior_U33(intPath) && !YtypHasInterior_U33(libPath), "");
                var kept = SplitPropLibraries_U33(new[] { libPath, intPath }, out var libs);
                check("mlo libraries: picked together, only the interior is placed and the library just supplies props",
                      kept.Length == 1 && kept[0] == intPath && libs.Count == 1 && libs[0] == libPath,
                      $"placed {string.Join(",", kept.Select(Path.GetFileName))}, libraries {string.Join(",", libs.Select(Path.GetFileName))}");
                var alone = SplitPropLibraries_U33(new[] { libPath }, out var none);
                check("mlo libraries: a prop library on its own is still laid out to look at",
                      alone.Length == 1 && none.Count == 0, "");
            }
            catch (Exception ex) { check("mlo libraries: the test ran", false, ex.ToString()); }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }
    }
}
