// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using UnityEngine.UI; // Required for Text
using TMPro; // Required for TMP_FontAsset

namespace Modules.ArtStyle
{
    /// <summary>
    /// Defines how a Text or TMP_Text component should be styled by the ArtSetApplicator.
    /// </summary>
    [System.Serializable]
    public class TextArtElement
    {
        [Tooltip("Reference to the target Text or TMP_Text component.")]
        public Component targetText; // Use Component to allow both Text and TMP_Text

        [Header("Item Selection")]
        [Tooltip("ID of the FontItemType (defined in ArtSetType) to use for the font.")]
        public string fontItemId;
        [Tooltip("ID of the ColourItemType (defined in ArtSetType) to use for the color.")]
        public string colourItemId;

        [Header("Overrides")]
        [Tooltip("Legacy Font to use if Font Mode is OverrideFont and target is Text.")]
        public Font overrideFont;
        [Tooltip("TMP Font Asset to use if Font Mode is OverrideFont and target is TMP_Text.")]
        public TMP_FontAsset overrideTmpFont;

        [Tooltip("Color to use if Colour Mode is OverrideColour.")]
        public Color overrideColour = Color.black; // Default override to black for text
    }
}
