// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using UnityEngine.UI; // Required for Image

namespace Modules.ArtStyle
{
    /// <summary>
    /// Defines how an Image component should be styled by the ArtSetApplicator.
    /// </summary>
    [System.Serializable]
    public class ImageArtElement
    {
        [Tooltip("Reference to the target Image component.")]
        public Image targetImage;

        [Header("Item Selection")]
        // [Tooltip("ID of the PicItemType (defined in ArtSetType) to use for the sprite.")]
        // public string picItemId; // REMOVED
        // [Tooltip("ID of the ColourItemType (defined in ArtSetType) to use for the tint.")]
        // public string colourItemId; // REMOVED

        [Tooltip("Index corresponding to the PicItemType list in the ArtSetType. -1 means use Set's default (Item[0]).")]
        [HideInInspector] public int picItemIndex = -1; // ADDED, default to None/Default
        [Tooltip("Index corresponding to the ColourItemType list in the ArtSetType. -1 means use Set's default (Item[0]).")]
        [HideInInspector] public int colourItemIndex = -1; // ADDED, default to None/Default


        // --- New Flags for Editor Control ---
        [HideInInspector] public bool overrideSpriteFlag = false; // Set by editor dropdown
        [HideInInspector] public bool overrideColourFlag = false; // Set by editor dropdown
        [HideInInspector] public bool usePicDefaultColourFlag = false; // Set by editor dropdown "[USE PIC DEFAULT]"

        // --- Overrides (Values used if corresponding flag is true) ---
        [Tooltip("Sprite to use if overriding.")]
        public Sprite overrideSprite;

        [Tooltip("Color to use if overriding.")]
        public Color overrideColour = Color.white;
    }
}
