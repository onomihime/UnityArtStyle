// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using System.Collections.Generic;

namespace Modules.ArtStyle
{
    /// <summary>
    /// A collection of FontItem assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFontSet", menuName = "Art Style/Sets/Font Set")]
    public class FontSet : ScriptableObject // REMOVED: : ArtSetApplicator.BaseSet<FontItem>
    {
        [SerializeField]
        [Tooltip("List of FontItems in this set. Index 0 is considered the default/fallback.")]
        private List<FontItem> _items = new List<FontItem>();

        /// <summary>
        /// List of FontItems in this set. Index 0 is considered the default/fallback.
        /// </summary>
        public List<FontItem> Items => _items; // Keep the Items property

        // Optional: Add methods for managing items if needed
    }
}
