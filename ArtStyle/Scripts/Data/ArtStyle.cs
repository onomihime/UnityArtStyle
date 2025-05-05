// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using System.Collections.Generic;
using System; // Required for Guid
using System.Linq; // Required for Linq

namespace Modules.ArtStyle
{
    /// <summary>
    /// ScriptableObject representing a complete visual theme (e.g., SciFi, Fantasy).
    /// Contains ArtSets mapped to ArtSetType slots defined in ArtSetting.
    /// </summary>
    [CreateAssetMenu(fileName = "NewArtStyle", menuName = "Art Style/Art Style")]
    public class ArtStyle : ScriptableObject
    {
        [Tooltip("Unique identifier for this ArtStyle.")]
        [SerializeField] private string _id;
        [Tooltip("User-friendly name for this ArtStyle (e.g., SciFiTheme).")]
        [SerializeField] private string _name = "New Art Style";

        // For Target 1, we use a simple list of ArtSet assets.
        // The mapping logic based on ArtSetting slots will be implemented later.
        [Tooltip("List of ArtSet assets belonging to this style.")]
        [SerializeField] private List<ArtSet> _artSets = new List<ArtSet>();

        public string Id => _id;
        public string Name => _name;
        public IReadOnlyList<ArtSet> ArtSets => _artSets; // Consider returning IReadOnlyList
        


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
        }

        /// <summary>
        /// Finds the ArtSet within this style that matches the specified ArtSetType.
        /// </summary>
        /// <param name="type">The ArtSetType to search for.</param>
        /// <returns>The matching ArtSet, or null if not found.</returns>
        public ArtSet FindArtSetByType(ArtSetType type)
        {
            if (type == null)
            {
                // Debug.LogWarning($"[{this.name}] FindArtSetByType called with null type."); // Optional log
                return null;
            }
            if (_artSets == null)
            {
                // Debug.LogWarning($"[{this.name}] FindArtSetByType called but _artSets is null."); // Optional log
                return null;
            }

            // --- Log Lookup Comparison ---
            // Debug.Log($"[{this.name}] Searching for ArtSetType ID: {type.Id} ({type.name})");
            foreach (var set in _artSets)
            {
                if (set == null) continue;
                string setTypeId = set.SetType?.Id ?? "null";
                string setTypeName = set.SetType?.name ?? "null";
                // Debug.Log($"  Checking Set '{set.name}': Has Type ID: {setTypeId} ({setTypeName})");
                if (set.SetType != null && set.SetType.Id == type.Id)
                {
                    // Debug.Log($"    Match found: Returning Set '{set.name}'");
                    return set; // Found the match
                }
            }
            // Debug.Log($"  No match found for Type ID: {type.Id}");
            // ---------------------------

            return null; // Original Linq replaced with loop for logging clarity
            // return _artSets.FirstOrDefault(set => set != null && set.SetType != null && set.SetType.Id == type.Id);
        }

         /// <summary>
        /// Finds the first ArtSet in the list that matches the given ArtSetType ID.
        /// </summary>
        /// <param name="typeId">The ID of the ArtSetType to search for.</param>
        /// <returns>The matching ArtSet, or null if not found.</returns>
        public ArtSet FindArtSetByType(string typeId)
        {
             if (string.IsNullOrEmpty(typeId)) return null;
             return _artSets.FirstOrDefault(set => set != null && set.SetType != null && set.SetType.Id == typeId);
        }
    }
}
