// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using UnityEngine.UI; // Required for Text

namespace Modules.ArtStyle
{
    /// <summary>
    /// Defines how a legacy Text component should be styled.
    /// </summary>
    [System.Serializable]
    public class LegacyTextArtElement
    {
        [Tooltip("Reference to the target Text component.")]
        public Text targetText;

        [Header("Item Selection")]
        // [Tooltip("ID of the FontItemType to use.")]
        // public string fontItemId; // REMOVED
        // [Tooltip("ID of the ColourItemType to use for the text color.")]
        // public string colourItemId; // REMOVED

        [Tooltip("Index corresponding to the FontItemType list. -1 means use Set's default.")]
        [HideInInspector] public int fontItemIndex = -1; // ADDED
        [Tooltip("Index corresponding to the ColourItemType list. -1 means use Set's default.")]
        [HideInInspector] public int colourItemIndex = -1; // ADDED

        // --- Flags ---
        [HideInInspector] public bool overrideFontFlag = false;
        [HideInInspector] public bool overrideColourFlag = false;
        [HideInInspector] public bool useFontDefaultColourFlag = false; // Use FontItem's default color

        // --- Overrides ---
        [Tooltip("Font to use if overriding.")]
        public Font overrideFont;
        [Tooltip("Color to use if overriding.")]
        public Color overrideColour = Color.black;
    }
}
