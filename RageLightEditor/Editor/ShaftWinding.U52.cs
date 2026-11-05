using System;
using CodeWalker.GameFiles;
using SharpDX;

namespace RageLightEditor.Editor
{
    public static class ShaftWinding_U52
    {
        public const uint FlagDrawFrontAndBehind = 1u << 6;

        public static Vector3 GameNormal(Vector3 a, Vector3 b, Vector3 c, Vector3 d) => Vector3.Cross(c - d, a - d);

        public static bool DrawnFor(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 beam, Vector3 camPos, uint flags)
        {
            var n = GameNormal(a, b, c, d);
            if (Vector3.Dot(n, beam) < 0.0f) return false;
            if ((flags & FlagDrawFrontAndBehind) == 0 && Vector3.Dot(n, camPos - d) < 0.0f) return false;
            return true;
        }

        public static bool AgainstDirection(in CExtensionDefLightShaft s)
        {
            if (s.direction.LengthSquared() < 1e-8f) return false;
            return Vector3.Dot(GameNormal(s.cornerA, s.cornerB, s.cornerC, s.cornerD), s.direction) < 0.0f;
        }

        public static bool HasArea(in CExtensionDefLightShaft s) =>
            GameNormal(s.cornerA, s.cornerB, s.cornerC, s.cornerD).LengthSquared() > 1e-8f;

        public static void Reverse(MCExtensionDefLightShaft w)
        {
            var d = w._Data;
            var b = d.cornerB;
            d.cornerB = d.cornerD;
            d.cornerD = b;
            w._Data = d;
        }

        public static bool FaceTowards(MCExtensionDefLightShaft w, Vector3 insideLocal)
        {
            var d = w._Data;
            var n = GameNormal(d.cornerA, d.cornerB, d.cornerC, d.cornerD);
            if (n.LengthSquared() < 1e-8f) return false;
            bool flipped = false;
            if (Vector3.Dot(n, insideLocal - d.cornerD) < 0.0f) { Reverse(w); flipped = true; d = w._Data; n = -n; }
            if (d.direction.LengthSquared() < 1e-8f)
            {
                d.direction = Vector3.Normalize(n);
                w._Data = d;
            }
            return flipped;
        }

        public static void SelfTest(Action<string, bool, string> check)
        {
            var a = new Vector3(0, 0, 0); var b = new Vector3(0, 0, 1); var c = new Vector3(1, 0, 1); var d = new Vector3(1, 0, 0);
            var n = GameNormal(a, b, c, d);
            check("shaft corners: A-B-C-D going clockwise as you look at them faces you (the game's own normal)",
                  Vector3.Dot(n, new Vector3(0, -1, 0)) > 0.0f, n.ToString());
            check("shaft corners: the game only draws it when the beam leaves on the side the corners face",
                  DrawnFor(a, b, c, d, new Vector3(0, -1, 0), new Vector3(0.5f, -3, 0.5f), 0) &&
                  !DrawnFor(a, b, c, d, new Vector3(0, 1, 0), new Vector3(0.5f, -3, 0.5f), 0), "");
            check("shaft corners: ...and only from that side unless 'draw in front and behind' (flag 64) is set",
                  !DrawnFor(a, b, c, d, new Vector3(0, -1, 0), new Vector3(0.5f, 3, 0.5f), 0) &&
                  DrawnFor(a, b, c, d, new Vector3(0, -1, 0), new Vector3(0.5f, 3, 0.5f), FlagDrawFrontAndBehind), "");

            var w = new MCExtensionDefLightShaft();
            var data = w._Data;
            data.cornerA = a; data.cornerB = b; data.cornerC = c; data.cornerD = d;
            data.direction = new Vector3(0, 1, 0);
            w._Data = data;
            bool against = AgainstDirection(w._Data);
            Reverse(w);
            check("shaft corners: 'Fix the corner order' turns a wrong-way shaft round without moving it",
                  against && !AgainstDirection(w._Data) && w._Data.cornerA == a && w._Data.cornerC == c && w._Data.cornerB == d && w._Data.cornerD == b,
                  $"was against {against}");

            var z = new MCExtensionDefLightShaft();
            var zd = z._Data;
            zd.cornerA = a; zd.cornerB = d; zd.cornerC = c; zd.cornerD = b;
            z._Data = zd;
            bool flipped = FaceTowards(z, new Vector3(0.5f, -2, 0.5f));
            check("shaft corners: after snapping, the corners turn to face where you stood and an empty direction points into the room",
                  flipped && Vector3.Dot(z._Data.direction, new Vector3(0, -1, 0)) > 0.99f &&
                  DrawnFor(z._Data.cornerA, z._Data.cornerB, z._Data.cornerC, z._Data.cornerD, z._Data.direction, new Vector3(0.5f, -2, 0.5f), 0),
                  z._Data.direction.ToString());
        }
    }
}
