// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using System; // Required for Guid

namespace Modules.ArtStyle
{
    /// <summary>
    /// Groups related asset collections (PicSet, ColourSet, FontSet, AnimationSet)
    /// representing a specific UI theme or component style (e.g., "MainMenuButtons", "GameplayHUD").
    /// </summary>
    [CreateAssetMenu(fileName = "NewArtSet", menuName = "Art Style/Art Set")]
    public class ArtSet : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Unique persistent identifier (GUID). Auto-generated.")]
        private string _id;

        [SerializeField]
        [Tooltip("User-friendly name for the set (e.g., 'MainMenuStyle', 'GameplayHUDStyle').")]
        private string _name = "New Art Set";

        [SerializeField]
        [Tooltip("The type assigned to this ArtSet, used for filtering.")]
        private ArtSetType _setType;

        [SerializeField]
        [Tooltip("Reference to the Picture Set containing image assets.")]
        private PicSet _picSet;

        [SerializeField]
        [Tooltip("Reference to the Colour Set containing color assets.")]
        private ColourSet _colourSet;

        [SerializeField]
        [Tooltip("Reference to the Font Set containing font assets.")]
        private FontSet _fontSet;

        [SerializeField]
        [Tooltip("Reference to the Animation Set containing animation definitions.")]
        private AnimationSet _animationSet;

        /// <summary>
        /// Unique persistent identifier (GUID). Should be auto-generated and read-only in editors.
        /// </summary>
        public string Id => _id;

        /// <summary>
        /// User-friendly name for the set.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// The type assigned to this ArtSet.
        /// </summary>
        public ArtSetType SetType => _setType;

        /// <summary>
        /// Reference to the Picture Set.
        /// </summary>
        public PicSet PicSet => _picSet;

        /// <summary>
        /// Reference to the Colour Set.
        /// </summary>
        public ColourSet ColourSet => _colourSet;

        /// <summary>
        /// Reference to the Font Set.
        /// </summary>
        public FontSet FontSet => _fontSet;

        /// <summary>
        /// Reference to the Animation Set.
        /// </summary>
        public AnimationSet AnimationSet => _animationSet;

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

        // TODO: Implement OnEnable or editor script logic for automatic GUID generation if _id is empty.
        // private void OnEnable() { if (string.IsNullOrEmpty(_id)) _id = System.Guid.NewGuid().ToString(); }
    }
}
