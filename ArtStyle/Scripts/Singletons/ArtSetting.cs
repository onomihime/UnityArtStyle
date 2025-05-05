// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent
using System.Collections.Generic; // Required for List

namespace Modules.ArtStyle
{
    /// <summary>
    /// Singleton ScriptableObject holding global art style settings,
    /// including managed styles and available ArtSetTypes.
    /// </summary>
    [CreateAssetMenu(fileName = "ArtSetting", menuName = "Art Style/Art Setting")]
    public class ArtSetting : ScriptableObject
    {
        // --- Singleton Access ---
        private static ArtSetting _instance;
        public static ArtSetting Instance => _instance;
        // Removed the Resources.Load logic

        // --- Events ---
        [System.Serializable]
        public class ArtStyleChangedEvent : UnityEvent<ArtStyle, ArtStyle> { } // Old Style, New Style
        public ArtStyleChangedEvent OnActiveStyleChanged = new ArtStyleChangedEvent();

        // --- Configuration ---
        [Header("Structure Definition")]
        [Tooltip("Defines the required ArtSetTypes that all managed ArtStyles should contain.")]
        [SerializeField] private List<ArtSetType> _artSetTypeSlots = new List<ArtSetType>();

        [Header("Managed Styles")]
        [Tooltip("List of ArtStyles managed by this setting.")]
        [SerializeField] private List<ArtStyle> _artStyles = new List<ArtStyle>();
        [Tooltip("Index of the currently active ArtStyle in the list above.")]
        [SerializeField] private int _activeArtStyleIndex = 0;

        // --- Properties ---
        public List<ArtSetType> ArtSetTypeSlots => _artSetTypeSlots;
        public List<ArtStyle> ArtStyles => _artStyles;

        public ArtStyle ActiveArtStyle
        {
            get
            {
                if (_artStyles == null || _artStyles.Count == 0) return null;
                if (_activeArtStyleIndex < 0 || _activeArtStyleIndex >= _artStyles.Count) return null;
                return _artStyles[_activeArtStyleIndex];
            }
            set
            {
                int newIndex = _artStyles.IndexOf(value);
                if (newIndex != -1 && newIndex != _activeArtStyleIndex)
                {
                    ArtStyle oldStyle = ActiveArtStyle;
                    _activeArtStyleIndex = newIndex;
                    Debug.Log($"[ArtSetting] Active ArtStyle changed to: {(value != null ? value.name : "None")}");
                    OnActiveStyleChanged?.Invoke(oldStyle, value);
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this); // Mark dirty when changed programmatically
#endif
                }
                else if (newIndex == -1)
                {
                    Debug.LogWarning($"[ArtSetting] Attempted to set an ArtStyle that is not in the managed list: {(value != null ? value.name : "Null")}", this);
                }
            }
        }

        // --- Singleton Management ---
        private void OnEnable()
        {
            // Handle ScriptableObject singleton pattern
            if (_instance == null)
            {
                _instance = this;
                // Optional: Log initialization
                // Debug.Log($"[ArtSetting] Instance assigned: {this.name}", this);
            }
            else if (_instance != this)
            {
                // A different instance is already assigned. This indicates a potential issue.
                Debug.LogError($"[ArtSetting] Duplicate instance detected! Existing instance: '{_instance.name}', New instance: '{this.name}'. Only one ArtSetting asset should be loaded.", this);
            }

            // Ensure index is valid
            if (_activeArtStyleIndex < 0 || _activeArtStyleIndex >= _artStyles.Count)
            {
                _activeArtStyleIndex = Mathf.Clamp(_activeArtStyleIndex, 0, Mathf.Max(0, _artStyles.Count - 1));
            }
        }

        private void OnDisable()
        {
            // Clear the static instance if this specific instance is disabled/unloaded
            if (_instance == this)
            {
                _instance = null;
                // Optional: Log cleanup
                // Debug.Log($"[ArtSetting] Instance cleared: {this.name}", this);
            }
        }

        // --- Utility Methods ---
        // (Add methods for finding types, styles etc. if needed)
    }
}
