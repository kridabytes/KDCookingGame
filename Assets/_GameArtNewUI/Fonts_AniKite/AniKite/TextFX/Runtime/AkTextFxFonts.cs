using System;
using System.Collections.Generic;
using TMPro;

namespace AniKite.TextFx
{
    /// <summary>
    /// The 24 bundled Text FX fonts: SIL OFL fonts with wide Unicode coverage, shipped as TTF under
    /// Runtime/Fonts (licences in Fonts/OFL). Recipes name them by id ("font": "bangers").
    /// In the editor the SDF assets are made on first use; in a build the
    /// AkTextFx component carries a baked reference, or a game registers its own asset with <see cref="Register"/>.
    /// </summary>
    public static class AkTextFxFonts
    {
        public static readonly string[] Ids =
        {
            "bangers", "baloo", "coiny", "sigmar", "grandstander", "anton", "dela_gothic", "chakra_petch", "tektur", "goldman",
            "exo2", "black_ops", "stencil", "protest_guerrilla", "vt323", "handjet", "tilt_neon", "bungee", "grenze_gotisch",
            "gideon_roman", "road_rage", "pacifico", "patrick_hand", "be_vietnam",
        };

        public static readonly string[] Families =
        {
            "Bangers", "Baloo 2", "Coiny", "Sigmar", "Grandstander", "Anton", "Dela Gothic One", "Chakra Petch", "Tektur", "Goldman",
            "Exo 2", "Black Ops One", "Big Shoulders Stencil", "Protest Guerrilla", "VT323", "Handjet", "Tilt Neon", "Bungee",
            "Grenze Gotisch", "Gideon Roman", "Road Rage", "Pacifico", "Patrick Hand", "Be Vietnam Pro",
        };

        public static readonly string[] Categories =
        {
            "comic", "rounded", "rounded", "comic", "cute", "impact", "impact", "scifi", "scifi", "scifi",
            "scifi", "military", "military", "military", "pixel", "pixel", "neon", "arcade",
            "fantasy", "fantasy", "horror", "script", "hand", "clean",
        };

        static readonly Dictionary<string, TMP_FontAsset> registered = new Dictionary<string, TMP_FontAsset>();

        /// <summary>Editor hook (set by the editor assembly): finds or creates the SDF asset of a bundled font.</summary>
        public static Func<string, TMP_FontAsset> EditorResolver;

        public static bool IsKnown(string id) => Array.IndexOf(Ids, id) >= 0;

        public static void Register(string id, TMP_FontAsset font)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (font == null) registered.Remove(id); else registered[id] = font;
        }

        public static TMP_FontAsset Resolve(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (registered.TryGetValue(id, out var f) && f != null) return f;
            return IsKnown(id) ? EditorResolver?.Invoke(id) : null;
        }
    }
}
