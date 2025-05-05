// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using System; // Required for Guid

namespace Modules.ArtStyle
{
    /// <summary>
    /// Represents a single color asset within an Art Style.
    /// </summary>
    [CreateAssetMenu(fileName = "NewColourItem", menuName = "Art Style/Items/Colour Item")]
    public class ColourItem : ScriptableObject // REMOVED: , ArtSetApplicator.IArtItem - Interface removed from Applicator
    {
        [SerializeField]
        [Tooltip("Unique persistent identifier (GUID). Auto-generated.")]
        private string _id;

        [SerializeField]
        [Tooltip("User-friendly name for identification in editors.")]
        private string _name = "New Colour Item";

        [SerializeField]
        [Tooltip("The color value.")]
        private Color _colour = Color.white;

        /// <summary>
        /// Unique persistent identifier (GUID).
        /// </summary>
        public string Id => _id; // Implement interface property

        /// <summary>
        /// User-friendly name for identification in editors.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// The color value.
        /// </summary>
        public Color Colour => _colour;

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
    }
}
