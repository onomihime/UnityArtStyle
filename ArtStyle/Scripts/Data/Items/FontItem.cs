// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using TMPro; // Required for TMP_FontAsset
using System; // Required for Guid

namespace Modules.ArtStyle
{
    /// <summary>
    /// Represents a font asset within an Art Style, supporting both legacy Font and TMP_FontAsset.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFontItem", menuName = "Art Style/Items/Font Item")]
    public class FontItem : ScriptableObject // REMOVED: , ArtSetApplicator.IArtItem - Interface removed from Applicator
    {
        [SerializeField]
        [Tooltip("Unique persistent identifier (GUID). Auto-generated.")]
        private string _id;

        [SerializeField]
        [Tooltip("User-friendly name for identification in editors.")]
        private string _name = "New Font Item";

        [SerializeField]
        [Tooltip("The legacy Unity Font asset.")]
        private Font _font;

        [SerializeField]
        [Tooltip("The TextMeshPro Font Asset.")]
        private TMP_FontAsset _tmpFont;

        [SerializeField]
        [Tooltip("Default color to apply to text using this font.")]
        private Color _defaultColour = Color.black;

        /// <summary>
        /// Unique persistent identifier (GUID).
        /// </summary>
        public string Id => _id; // Implement interface property

        /// <summary>
        /// User-friendly name for identification in editors.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// The legacy Unity Font asset.
        /// </summary>
        public Font Font => _font;

        /// <summary>
        /// The TextMeshPro Font Asset.
        /// </summary>
        public TMP_FontAsset TmpFont => _tmpFont;

        /// <summary>
        /// Default color to apply to text using this font.
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
