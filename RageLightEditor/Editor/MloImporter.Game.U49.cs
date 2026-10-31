using System;
using System.Diagnostics;
using System.Linq;
using CodeWalker.GameFiles;
using RageLightEditor.Rendering;
using SharpDX;

namespace RageLightEditor.Editor
{
    public sealed class GameMloInfo_U49
    {
        public uint Hash;
        public string Name = "";
        public string Ytyp = "";
        public int Rooms;
        public int Entities;
        public MloArchetype Arch;
    }

    public partial class MloImporter
    {
        public MloImportResult ImportGameMlo_U49(MloArchetype mlo, out RenderModel placedModel, Action<string> progress = null)
        {
            var sw = Stopwatch.StartNew();
            var result = new MloImportResult();
            placedModel = new RenderModel { Name = mlo.Name ?? mlo.Hash.ToString() };
            modelRenderer.TextureFallback = (tex, txd) => local?.FindTexture(tex) ?? game.FindTexture(tex, txd);
            PlaceMlo(mlo, Vector3.Zero, Quaternion.Identity, placedModel, result, progress);
            RecomputeBounds(placedModel);
            foreach (var p in result.Props) RecomputeBounds(p.Model);
            CollectUntextured(result);
            result.UniqueProps = propCache.Count;
            result.LightCount = result.Props.Sum(p => p.LightCount * p.Placed);
            result.Seconds = sw.Elapsed.TotalSeconds;
            return result;
        }
    }
}
