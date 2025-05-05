// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using System.Collections.Generic;
using System; // Required for Guid

namespace Modules.ArtStyle
{
    //-------------------------------------------------
    // Item Type Definitions (Not ScriptableObjects)
    //-------------------------------------------------

    /// <summary>
    /// Base class for defining an item slot within an ArtSetType.
    /// </summary>
    [System.Serializable]
    public abstract class ItemType
    {
        [SerializeField] private string _id;
        [SerializeField] private string _name = "New Item Type";

        public string Id => _id;
        public string Name { get => _name; set => _name = value; } // Allow name editing

        // Constructor or method to ensure ID is set
        public void EnsureId()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = Guid.NewGuid().ToString();
            }
        }
    }

    [System.Serializable] public class PicItemType : ItemType { public PicItemType() { Name = "New Pic Type"; } }
    [System.Serializable] public class ColourItemType : ItemType { public ColourItemType() { Name = "New Colour Type"; } }
    [System.Serializable] public class FontItemType : ItemType { public FontItemType() { Name = "New Font Type"; } }
    [System.Serializable] public class AnimationItemType : ItemType { public AnimationItemType() { Name = "New Anim Type"; } }

    //-------------------------------------------------
    // ArtSetType ScriptableObject
    //-------------------------------------------------

    /// <summary>
    /// ScriptableObject defining the structure (item slots) for a type of ArtSet (e.g., Button, Panel).
    /// </summary>
    [CreateAssetMenu(fileName = "NewArtSetType", menuName = "Art Style/Art Set Type")]
    public class ArtSetType : ScriptableObject
    {
        [Tooltip("Unique identifier for this ArtSetType.")]
        [SerializeField] private string _id;
        [Tooltip("User-friendly name for this ArtSetType (e.g., Button, Panel).")]
        [SerializeField] private string _name = "New Art Set Type";

        [Tooltip("Defines the picture item slots available for this type.")]
        [SerializeField] private List<PicItemType> _picItemTypes = new List<PicItemType>();
        [Tooltip("Defines the colour item slots available for this type.")]
        [SerializeField] private List<ColourItemType> _colourItemTypes = new List<ColourItemType>();
        [Tooltip("Defines the font item slots available for this type.")]
        [SerializeField] private List<FontItemType> _fontItemTypes = new List<FontItemType>();
        [Tooltip("Defines the animation item slots available for this type.")]
        [SerializeField] private List<AnimationItemType> _animationItemTypes = new List<AnimationItemType>();

        public string Id => _id;
        public string Name => _name;
        public List<PicItemType> PicItemTypes => _picItemTypes;
        public List<ColourItemType> ColourItemTypes => _colourItemTypes;
        public List<FontItemType> FontItemTypes => _fontItemTypes;
        public List<AnimationItemType> AnimationItemTypes => _animationItemTypes;

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

            // Ensure all contained ItemTypes have IDs (important for newly added ones in editor)
            EnsureItemTypeIds(_picItemTypes);
            EnsureItemTypeIds(_colourItemTypes);
            EnsureItemTypeIds(_fontItemTypes);
            EnsureItemTypeIds(_animationItemTypes);
        }

        private void EnsureItemTypeIds<T>(List<T> itemTypes) where T : ItemType
        {
            bool changed = false;
            foreach (var itemType in itemTypes)
            {
                if (itemType != null && string.IsNullOrEmpty(itemType.Id))
                {
                    itemType.EnsureId();
                    changed = true;
                }
            }
#if UNITY_EDITOR
            if (changed) UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        // Method to validate ItemType IDs when modified in the editor
        public void ValidateItemTypeIds()
        {
            EnsureItemTypeIds(_picItemTypes);
            EnsureItemTypeIds(_colourItemTypes);
            EnsureItemTypeIds(_fontItemTypes);
            EnsureItemTypeIds(_animationItemTypes);
        }
    }
}
