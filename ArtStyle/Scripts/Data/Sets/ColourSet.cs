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
    /// Contains a collection of ColourItems mapped to ColourItemType slots.
    /// </summary>
    [CreateAssetMenu(fileName = "NewColourSet", menuName = "Art Style/Sets/Colour Set")]
    public class ColourSet : ScriptableObject // REMOVED: : ArtSetApplicator.BaseSet<ColourItem>
    {
        [SerializeField]
        [Tooltip("Unique persistent identifier (GUID). Auto-generated.")]
        private string _id; // Optional: ID for the set itself

        [SerializeField]
        [Tooltip("List of items. Index 0 is the default/fallback.")]
        private List<ColourItem> _items = new List<ColourItem>();

        /// <summary>
        /// Provides access to the list of items. Index 0 is the default/fallback.
        /// </summary>
        public List<ColourItem> Items => _items; // Keep the Items property

        public string Id => _id; // Optional

        private void OnEnable()
        {
            // Ensure ID is generated if missing
            if (string.IsNullOrEmpty(_id))
            {
                _id = Guid.NewGuid().ToString();
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
            // Ensure default slot exists
            if (_items == null) _items = new List<ColourItem>();
            if (_items.Count == 0)
            {
                 _items.Add(null); // Add a placeholder for the default slot
                 #if UNITY_EDITOR
                 UnityEditor.EditorUtility.SetDirty(this);
                 #endif
            }
        }
    }
}
