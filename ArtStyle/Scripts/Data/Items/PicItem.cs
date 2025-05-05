// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using System; // Required for Guid

namespace Modules.ArtStyle
{
    /// <summary>
    /// Represents a single picture asset within an Art Style, including a sprite and default tint.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPicItem", menuName = "Art Style/Items/Picture Item")]
    public class PicItem : ScriptableObject // REMOVED: , ArtSetApplicator.IArtItem
    {
        [SerializeField]
        [Tooltip("Unique persistent identifier (GUID). Auto-generated.")]
        private string _id;

        [SerializeField]
        [Tooltip("User-friendly name for identification in editors.")]
        private string _name = "New Pic Item";

        [SerializeField]
        [Tooltip("The image sprite asset.")]
        private Sprite _sprite;

        [SerializeField]
        [Tooltip("Default tint color to apply to the sprite.")]
        private Color _defaultColour = Color.white;

        /// <summary>
        /// Unique persistent identifier (GUID). Should be auto-generated and read-only in editors.
        /// </summary>
        public string Id => _id; // No longer implementing interface property explicitly

        /// <summary>
        /// User-friendly name for identification in editors.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// The image sprite asset.
        /// </summary>
        public Sprite Sprite => _sprite;

        /// <summary>
        /// Default tint color to apply to the sprite.
        /// </summary>
        public Color DefaultColour => _defaultColour;

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
