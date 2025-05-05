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
    /// Contains a collection of PicItems mapped to PicItemType slots.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPicSet", menuName = "Art Style/Sets/Picture Set")]
    public class PicSet : ScriptableObject // REMOVED: : ArtSetApplicator.BaseSet<PicItem>
    {
        [SerializeField]
        [Tooltip("Unique persistent identifier (GUID). Auto-generated.")]
        private string _id; // Optional: ID for the set itself

        [SerializeField]
        [Tooltip("List of items. Index 0 is the default/fallback.")]
        private List<PicItem> _items = new List<PicItem>();

        /// <summary>
        /// Provides access to the list of items. Index 0 is the default/fallback.
        /// </summary>
        public List<PicItem> Items => _items; // Keep the Items property

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
            if (_items == null) _items = new List<PicItem>();
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
