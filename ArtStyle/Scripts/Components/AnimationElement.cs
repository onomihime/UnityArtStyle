// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;

namespace Modules.ArtStyle
{
    /// <summary>
    /// Defines how a UI element (via RectTransform) should be animated by the ArtSetApplicator.
    /// </summary>
    [System.Serializable]
    public class AnimationElement
    {
        [Tooltip("Reference to the target UI element's RectTransform.")]
        public RectTransform targetTransform;

        [Header("Item Selection")]
        [Tooltip("Index corresponding to the AnimationItemType list. -1 means use Set's default.")]
        [HideInInspector] public int animationItemIndex = -1; // ADDED

        // --- New Flags/Overrides for Editor Control ---
        [HideInInspector] public bool overrideAnimationFlag = false; // Set by editor dropdown

        [Tooltip("AnimationItem to use if overriding.")]
        public AnimationItem overrideAnimationItem;
    }
}
