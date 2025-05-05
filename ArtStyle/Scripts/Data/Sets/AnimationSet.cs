// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using System.Collections.Generic;
using System; // Required for Guid

namespace Modules.ArtStyle
{
    /// <summary>
    /// A collection of AnimationItem assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAnimationSet", menuName = "Art Style/Sets/Animation Set")]
    public class AnimationSet : ScriptableObject // REMOVED: : ArtSetApplicator.BaseSet<AnimationItem>
    {
        [SerializeField]
        [Tooltip("List of AnimationItems in this set. Index 0 is considered the default/fallback.")]
        private List<AnimationItem> _items = new List<AnimationItem>();

        /// <summary>
        /// List of AnimationItems in this set. Index 0 is considered the default/fallback.
        /// </summary>
        public List<AnimationItem> Items => _items; // Keep the Items property

        // Optional: Add methods for managing items if needed
    }
}
