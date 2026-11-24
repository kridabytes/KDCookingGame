using System;
using UnityEngine;

namespace AniKite.TextFx
{
    /// <summary>An imported AkTextFX recipe ("aktextfx/1" JSON). Drop it on an <see cref="AkTextFx"/>.</summary>
    [CreateAssetMenu(menuName = "AniKite/Text FX Preset", fileName = "TextFx")]
    public sealed class AkTextFxPreset : ScriptableObject
    {
        [TextArea(6, 30)] public string json;

        [NonSerialized] AkFxRecipe compiled;
        [NonSerialized] string compiledFrom;

        /// <summary>The compiled recipe (cached until the JSON changes). Throws FormatException on a bad recipe.</summary>
        public AkFxRecipe Recipe
        {
            get
            {
                if (compiled == null || !ReferenceEquals(compiledFrom, json)) { compiled = AkFxRecipe.Compile(json); compiledFrom = json; }
                return compiled;
            }
        }

        void OnValidate() { compiled = null; }
    }
}
