// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq; // Required for Linq FindIndex

// Moved using directive here, wrapped in #if
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Modules.ArtStyle
{
    /// <summary>
    /// Applies an ArtSet to UI elements based on defined element mappings.
    /// Determines the ArtSet via overrides or the global ArtSetting.
    /// </summary>
    public class ArtSetApplicator : MonoBehaviour
    {
        [Header("Source Configuration")]
        [Tooltip("If true, uses the active ArtStyle from ArtSetting and the ArtSet Index Filter.")]
        [SerializeField] private bool _useArtSetting = true;
        [Tooltip("ArtStyle to use if Use Art Setting is false.")]
        [SerializeField] private ArtStyle _artStyleOverride = null;
        [Tooltip("Selects the ArtSet from the chosen ArtStyle's list by index. Used when 'Use Art Setting' is true or 'Art Style Override' is set.")]
        [SerializeField] private int _artSetIndexFilter = 0; // ADDED
        [Tooltip("Direct ArtSet reference used only if 'Use Art Setting' is false AND 'Art Style Override' is null. Lowest priority.")]
        [SerializeField] private ArtSet _artSetFallback = null; // Renamed

        [Header("Behaviour")] // ADDED Header
        [Tooltip("If true, automatically applies the style whenever the resolved ArtSet changes.")]
        [SerializeField] private bool _applyOnArtSetChange = true; // ADDED

        [Header("Default Fallbacks")]
        [Tooltip("Fallback item if a required PicItem is missing.")]
        [SerializeField] private PicItem _defaultPicItem = null;
        [Tooltip("Fallback item if a required ColourItem is missing.")]
        [SerializeField] private ColourItem _defaultColourItem = null;
        [Tooltip("Fallback item if a required FontItem is missing.")]
        [SerializeField] private FontItem _defaultFontItem = null;
        [Tooltip("Fallback item if a required AnimationItem is missing.")]
        [SerializeField] private AnimationItem _defaultAnimationItem = null;

        [Header("Element Mappings")]
        [SerializeField] internal List<ImageArtElement> _imageElements = new List<ImageArtElement>(); // Changed to internal
        [SerializeField] internal List<LegacyTextArtElement> _legacyTextElements = new List<LegacyTextArtElement>(); // Changed to internal
        [SerializeField] internal List<TMPTextArtElement> _tmpTextElements = new List<TMPTextArtElement>(); // Changed to internal
        [SerializeField] internal List<AnimationElement> _animationElements = new List<AnimationElement>(); // Changed to internal

        // Runtime Cache
        private ArtSet _resolvedArtSet = null;
        private bool _isSubscribed = false;

        // --- Unity Lifecycle ---

        private void Awake()
        {
            // Initial resolve attempt
             ResolveActiveArtSet();
        }

        private void OnEnable()
        {
            SubscribeToSettingChanges();
            // Apply style immediately if in play mode
            if (Application.isPlaying)
            {
                ApplyStyleInternal(false); // No force rebuild
            }
            
        }

        private void OnDisable()
        {
            UnsubscribeFromSettingChanges();
        }

        // --- Public API ---

        /// <summary>
        /// Applies the currently resolved ArtSet to all mapped elements.
        /// Forces a rebuild of lookup dictionaries if necessary.
        /// </summary>
        public void ApplyStyle()
        {
             ApplyStyleInternal(true); // Force rebuild when called externally
        }

        /// <summary>
        /// Plays the animation associated with the target RectTransform.
        /// </summary>
        /// <param name="target">The RectTransform of the UI element to animate.</param>
        public void PlayAnimation(RectTransform target)
        {
            if (target == null)
            {
                Debug.LogWarning($"[ArtSetApplicator] PlayAnimation called with null target on {gameObject.name}", gameObject);
                return;
            }

            // Find the element targeting this transform
            int index = _animationElements.FindIndex(e => e.targetTransform == target);
            if (index == -1)
            {
                Debug.LogWarning($"[ArtSetApplicator] No AnimationElement found for target {target.name} on {gameObject.name}", gameObject);
                return;
            }

            AnimationElement element = _animationElements[index];

            // Ensure dictionaries are ready (only build if needed)
            // if (_needsRebuild) BuildLookupDictionaries(); // REMOVED

            // Resolve Item using new override flag
            AnimationItem itemToPlay = ResolveAnimationItem(element); // Now resolves via index

            if (itemToPlay == null)
            {
                Debug.LogError($"[ArtSetApplicator] No AnimationItem (resolved, override, or default) available to play for {target.name} on {gameObject.name}", gameObject);
                return;
            }

            // Resolve Duration (always comes from the final item)
            float duration = itemToPlay.Duration;

            // Find Applicator Component
            AnimationApplicator applicator = target.GetComponent<AnimationApplicator>();
            if (applicator == null)
            {
                // Try adding if missing? Or rely on editor button? Let's rely on button for now.
                Debug.LogError($"[ArtSetApplicator] Missing AnimationApplicator component on target {target.name}. Add one or use 'Setup Animation Components' button.", target.gameObject);
                return;
            }

            // Ensure CanvasGroup exists
            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
             if (canvasGroup == null && itemToPlay.UseFade) // Only strictly required if fading
             {
                 Debug.LogError($"[ArtSetApplicator] Missing CanvasGroup component on target {target.name}, required for fade animation. Add one or use 'Setup Animation Components' button.", target.gameObject);
                 return;
             }


            // Play
            applicator.Play(itemToPlay, duration);
        }


        // --- Internal Logic ---

        /// <summary>
        /// Core method to apply styles. Can force a rebuild of dictionaries.
        /// </summary>
        private void ApplyStyleInternal(bool forceRebuild) // forceRebuild parameter is now unused but kept for compatibility if called elsewhere
        {
             // if (forceRebuild || _needsRebuild) // REMOVED Check
             // {
                 ResolveActiveArtSet(); // Always resolve before applying
                 // BuildLookupDictionaries(); // REMOVED
                 // _needsRebuild = false; // REMOVED
             // }

             if (_resolvedArtSet == null)
             {
                 Debug.LogWarning($"[ArtSetApplicator] No ArtSet resolved on {gameObject.name}. Cannot apply style. Check Source Configuration.", gameObject);
                 return; // Stop if no set is resolved
             }

             ApplyImageElements();
             ApplyLegacyTextElements();
             ApplyTMPTextElements();
             // ApplyAnimationElements(); // No direct application needed for animation, only PlayAnimation
        }

        /// <summary>
        /// Determines the ArtSet to use based on overrides and settings. Follows Editor logic.
        /// Now uses index filter instead of type filter.
        /// </summary>
        public void ResolveActiveArtSet()
        {
            ArtSet previouslyResolved = _resolvedArtSet;
            ArtSet newResolvedSet = null;
            ArtStyle styleToUse = null;
            string resolutionReason = "Unknown"; // For logging

            // 1. Determine Style Source (Setting or Override)
            if (_useArtSetting)
            {
                // Scenario 1: Use ArtSetting
                if (ArtSetting.Instance != null)
                {
                    styleToUse = ArtSetting.Instance.ActiveArtStyle;
                    resolutionReason = "ArtSetting";
                }
                // else: styleToUse remains null, handled below
            }
            else
            {
                // Scenario 2 or 3: Use Override or Fallback
                styleToUse = _artStyleOverride; // This might be null
                resolutionReason = styleToUse != null ? "StyleOverride" : "FallbackAttempt";
            }

            // 2. Find Set within Style using Index Filter (if a style source exists)
            if (styleToUse != null)
            {
                // Applies to Scenario 1 and Scenario 2 (when _artStyleOverride is set)
                if (styleToUse.ArtSets != null && _artSetIndexFilter >= 0 && _artSetIndexFilter < styleToUse.ArtSets.Count)
                {
                    newResolvedSet = styleToUse.ArtSets[_artSetIndexFilter];
                    resolutionReason += $"+Index[{_artSetIndexFilter}]";
                }
                else
                {
                    // Index out of bounds or ArtSets list is null
                    Debug.LogWarning($"[ArtSetApplicator] ArtSetIndexFilter ({_artSetIndexFilter}) is out of bounds for ArtStyle '{styleToUse.name}' ({styleToUse.ArtSets?.Count ?? 0} sets). Resolving to null.", gameObject);
                    resolutionReason += "+IndexOutOfBounds";
                    newResolvedSet = null; // Ensure it's null if index is invalid
                }
                // newResolvedSet = styleToUse.FindArtSetByType(_artSetTypeFilter); // OLD LOGIC
            }

            // 3. Use Fallback if no set resolved yet AND in the correct state
            // (Applies only to Scenario 3: _useArtSetting is false AND _artStyleOverride is null)
            if (newResolvedSet == null && !_useArtSetting && styleToUse == null) // styleToUse is null here only if _artStyleOverride was null
            {
                newResolvedSet = _artSetFallback;
                resolutionReason = "Fallback";
            }

            // Update _resolvedArtSet and flag for rebuild if changed
            _resolvedArtSet = newResolvedSet;
            if (previouslyResolved != _resolvedArtSet)
            {
                // _needsRebuild = true; // REMOVED - No longer needed
                // Optional: Log the resolution result
                // Debug.Log($"[ArtSetApplicator] Resolved ArtSet to '{_resolvedArtSet?.name ?? "NULL"}' using {resolutionReason} on {gameObject.name}", gameObject);

                // --- Apply automatically if flag is set ---
                if (_applyOnArtSetChange)
                {
                    // Debug.Log($"[ArtSetApplicator] Applying style automatically due to resolved set change on {gameObject.name}", gameObject);
                    ApplyStyleInternal(true); // Apply immediately, no need to force rebuild again
                    // force rebuild anyway
                    
                }
                // -----------------------------------------
            }
        }


        private void ApplyImageElements()
        {
            foreach (var element in _imageElements)
            {
                if (element.targetImage == null) continue;

                // Resolve and Apply Sprite
                element.targetImage.sprite = ResolveSprite(element);
                // Optional: Enable/disable image based on sprite presence
                // element.targetImage.enabled = (element.targetImage.sprite != null);

                // Resolve and Apply Colour
                element.targetImage.color = ResolveImageColour(element);
            }
        }

        /// <summary>
        /// Applies styles to legacy Text elements.
        /// </summary>
        private void ApplyLegacyTextElements()
        {
            foreach (var element in _legacyTextElements)
            {
                if (element.targetText == null) continue;

                // Resolve and Apply Font
                element.targetText.font = ResolveLegacyFont(element);

                // Resolve and Apply Colour - Pass indices
                element.targetText.color = ResolveTextColour(element.colourItemIndex, element.fontItemIndex, element.overrideColourFlag, element.useFontDefaultColourFlag, element.overrideColour);
            }
        }

        /// <summary>
        /// Applies styles to TMP_Text elements.
        /// </summary>
        private void ApplyTMPTextElements()
        {
            foreach (var element in _tmpTextElements)
            {
                if (element.targetText == null) continue;

                // Resolve and Apply Font
                element.targetText.font = ResolveTMPFont(element);

                // Resolve and Apply Colour - Pass indices
                element.targetText.color = ResolveTextColour(element.colourItemIndex, element.fontItemIndex, element.overrideColourFlag, element.useFontDefaultColourFlag, element.overrideColour);
            }
        }

        // --- Value Resolution Helpers ---

        internal Sprite ResolveSprite(ImageArtElement element) // Changed to internal
        {
            if (element.overrideSpriteFlag)
            {
                return element.overrideSprite;
            }

            // Resolve via Index
            int picIndex = element.picItemIndex;
            PicItem resolvedPicItem = null;

            if (_resolvedArtSet?.PicSet?.Items != null)
            {
                var picItems = _resolvedArtSet.PicSet.Items;
                int targetListIndex = (picIndex == -1) ? 0 : picIndex + 1; // Map dropdown index (-1=None, 0=Type0, 1=Type1...) to list index (0=Default, 1=Type0, 2=Type1...)

                if (targetListIndex >= 0 && targetListIndex < picItems.Count)
                {
                    resolvedPicItem = picItems[targetListIndex];
                }
                else if (picItems.Count > 0) // Fallback to Item[0] if index invalid
                {
                     resolvedPicItem = picItems[0];
                     // Optional: Log warning about invalid index
                     // Debug.LogWarning($"[ArtSetApplicator] PicItem index {targetListIndex} (Type Index {picIndex}) out of bounds for ArtSet '{_resolvedArtSet.name}'. Using default Item[0].", gameObject);
                }
            }

            // Use resolved item or fallback to applicator default
            PicItem finalItem = resolvedPicItem ?? _defaultPicItem;
            return (finalItem != null) ? finalItem.Sprite : null;
        }

        internal Color ResolveImageColour(ImageArtElement element) // Changed to internal
        {
            if (element.overrideColourFlag)
            {
                return element.overrideColour;
            }

            if (element.usePicDefaultColourFlag)
            {
                // Resolve PicItem first using its index to get its default color
                int picIndex = element.picItemIndex;
                PicItem resolvedPicItemForColour = null;
                 if (_resolvedArtSet?.PicSet?.Items != null)
                 {
                     var picItems = _resolvedArtSet.PicSet.Items;
                     int targetListIndex = (picIndex == -1) ? 0 : picIndex + 1;
                     if (targetListIndex >= 0 && targetListIndex < picItems.Count)
                     {
                         resolvedPicItemForColour = picItems[targetListIndex];
                     }
                     else if (picItems.Count > 0) // Fallback to Item[0] if index invalid
                     {
                          resolvedPicItemForColour = picItems[0];
                     }
                 }

                 // Use PicItem's default color or fallback to applicator default color
                 if (resolvedPicItemForColour != null)
                 {
                     return resolvedPicItemForColour.DefaultColour;
                 }
                 else
                 {
                      // Fallback if PicItem couldn't be resolved at all
                      return (_defaultColourItem != null) ? _defaultColourItem.Colour : Color.white;
                 }
            }

            // Resolve via ColourItem Index
            int colourIndex = element.colourItemIndex;
            ColourItem resolvedColourItem = null;

             if (_resolvedArtSet?.ColourSet?.Items != null)
            {
                var colourItems = _resolvedArtSet.ColourSet.Items;
                int targetListIndex = (colourIndex == -1) ? 0 : colourIndex + 1; // Map dropdown index to list index

                if (targetListIndex >= 0 && targetListIndex < colourItems.Count)
                {
                    resolvedColourItem = colourItems[targetListIndex];
                }
                 else if (colourItems.Count > 0) // Fallback to Item[0] if index invalid
                {
                     resolvedColourItem = colourItems[0];
                     // Optional: Log warning about invalid index
                     // Debug.LogWarning($"[ArtSetApplicator] ColourItem index {targetListIndex} (Type Index {colourIndex}) out of bounds for ArtSet '{_resolvedArtSet.name}'. Using default Item[0].", gameObject);
                }
            }

            // Use resolved item or fallback to applicator default
            ColourItem finalItem = resolvedColourItem ?? _defaultColourItem;
            return (finalItem != null) ? finalItem.Colour : Color.white;
        }

         internal Font ResolveLegacyFont(LegacyTextArtElement element) // Changed to internal
         {
             if (element.overrideFontFlag)
             {
                 return element.overrideFont;
             }

             // Resolve via Index
             int fontIndex = element.fontItemIndex;
             FontItem resolvedFontItem = null;

             if (_resolvedArtSet?.FontSet?.Items != null)
             {
                 var fontItems = _resolvedArtSet.FontSet.Items;
                 int targetListIndex = (fontIndex == -1) ? 0 : fontIndex + 1;

                 if (targetListIndex >= 0 && targetListIndex < fontItems.Count)
                 {
                     resolvedFontItem = fontItems[targetListIndex];
                 }
                 else if (fontItems.Count > 0) // Fallback to Item[0]
                 {
                      resolvedFontItem = fontItems[0];
                 }
             }

             // Use resolved item or fallback to applicator default
             FontItem finalItem = resolvedFontItem ?? _defaultFontItem;
             return (finalItem != null) ? finalItem.Font : null;
         }

         internal TMP_FontAsset ResolveTMPFont(TMPTextArtElement element) // Changed to internal
         {
             if (element.overrideFontFlag)
             {
                 return element.overrideTmpFont;
             }

             // Resolve via Index
             int fontIndex = element.fontItemIndex;
             FontItem resolvedFontItem = null;

             if (_resolvedArtSet?.FontSet?.Items != null)
             {
                 var fontItems = _resolvedArtSet.FontSet.Items;
                 int targetListIndex = (fontIndex == -1) ? 0 : fontIndex + 1;

                 if (targetListIndex >= 0 && targetListIndex < fontItems.Count)
                 {
                     resolvedFontItem = fontItems[targetListIndex];
                 }
                 else if (fontItems.Count > 0) // Fallback to Item[0]
                 {
                      resolvedFontItem = fontItems[0];
                 }
             }

             // Use resolved item or fallback to applicator default
             FontItem finalItem = resolvedFontItem ?? _defaultFontItem;
             return (finalItem != null) ? finalItem.TmpFont : null;
         }

        // Updated to accept indices
        internal Color ResolveTextColour(int colourItemIndex, int fontItemIndex, bool overrideColourFlag, bool useFontDefaultColourFlag, Color overrideColour) // Changed to internal
        {
             if (overrideColourFlag)
             {
                 return overrideColour;
             }

             if (useFontDefaultColourFlag)
             {
                 // Resolve FontItem first using its index
                 FontItem resolvedFontItem = null;
                 if (_resolvedArtSet?.FontSet?.Items != null)
                 {
                     var fontItems = _resolvedArtSet.FontSet.Items;
                     int targetListIndex = (fontItemIndex == -1) ? 0 : fontItemIndex + 1;
                     if (targetListIndex >= 0 && targetListIndex < fontItems.Count)
                     {
                         resolvedFontItem = fontItems[targetListIndex];
                     }
                     else if (fontItems.Count > 0) // Fallback to Item[0]
                     {
                          resolvedFontItem = fontItems[0];
                     }
                 }
                 FontItem fontItemForColour = resolvedFontItem ?? _defaultFontItem; // Use default if specific not found

                 if (fontItemForColour != null)
                 {
                     return fontItemForColour.DefaultColour;
                 }
                 // Fall through if FontItem couldn't be resolved at all
             }

             // Resolve via ColourItem Index
             ColourItem resolvedColourItem = null;
             if (_resolvedArtSet?.ColourSet?.Items != null)
             {
                 var colourItems = _resolvedArtSet.ColourSet.Items;
                 int targetListIndex = (colourItemIndex == -1) ? 0 : colourItemIndex + 1;
                 if (targetListIndex >= 0 && targetListIndex < colourItems.Count)
                 {
                     resolvedColourItem = colourItems[targetListIndex];
                 }
                 else if (colourItems.Count > 0) // Fallback to Item[0]
                 {
                      resolvedColourItem = colourItems[0];
                 }
             }

             // Use resolved item or fallback to applicator default
             ColourItem finalItem = resolvedColourItem ?? _defaultColourItem;
             return (finalItem != null) ? finalItem.Colour : Color.black; // Default black for text
        }

         internal AnimationItem ResolveAnimationItem(AnimationElement element) // Changed to internal
         {
             if (element.overrideAnimationFlag)
             {
                 // Use override item, but fallback to default if override is null
                 return element.overrideAnimationItem ?? _defaultAnimationItem;
             }

             // Resolve via Index
             int animIndex = element.animationItemIndex;
             AnimationItem resolvedAnimItem = null;

             if (_resolvedArtSet?.AnimationSet?.Items != null)
             {
                 var animItems = _resolvedArtSet.AnimationSet.Items;
                 int targetListIndex = (animIndex == -1) ? 0 : animIndex + 1;

                 if (targetListIndex >= 0 && targetListIndex < animItems.Count)
                 {
                     resolvedAnimItem = animItems[targetListIndex];
                 }
                 else if (animItems.Count > 0) // Fallback to Item[0]
                 {
                      resolvedAnimItem = animItems[0];
                 }
             }

             // Use resolved item or fallback to applicator default
             return resolvedAnimItem ?? _defaultAnimationItem;
         }


        // --- Event Handling ---

        public void SubscribeToSettingChanges()
        {
            // No changes needed here unless ArtSetting itself changes structure
            if (ArtSetting.Instance != null && !_isSubscribed)
            {
                ArtSetting.Instance.OnActiveStyleChanged.AddListener(HandleActiveStyleChanged);
                _isSubscribed = true;
            }
        }

        private void UnsubscribeFromSettingChanges()
        {
            // No changes needed here
            if (ArtSetting.Instance != null && _isSubscribed)
            {
                ArtSetting.Instance.OnActiveStyleChanged.RemoveListener(HandleActiveStyleChanged);
                _isSubscribed = false;
            }
        }

        private void HandleActiveStyleChanged(ArtStyle oldStyle, ArtStyle newStyle)
        {
            Debug.Log("Event listened, active style changed.");
            // Only react if using the setting
            if (_useArtSetting)
            {
                 // The active style changed, so we definitely need to resolve the ArtSet again
                 ResolveActiveArtSet(); // This might trigger ApplyStyleInternal if _applyOnArtSetChange is true and the set actually changed
                 // _needsRebuild = true; // REMOVED
                 // We don't automatically apply here anymore unless ResolveActiveArtSet triggers it.
                 // ApplyStyleInternal(true);
                 // Optional: Log that a change was detected
                 Debug.Log($"[ArtSetApplicator] ArtSetting style changed on {gameObject.name}. Style will be updated based on 'Apply On Art Set Change' setting.", gameObject);

                 // Update the editor view if possible (requires editor communication or repaint)
                 #if UNITY_EDITOR
                 UnityEditor.EditorUtility.SetDirty(this); // Mark component dirty to encourage repaint
                 #endif
            }
        }

        // --- Editor Support ---

        /// <summary>
        /// [Editor Only] Applies the style immediately. Called by the editor button.
        /// </summary>
        [ContextMenu("Apply Style Now (Editor)")]
        public void ApplyStyleEditor()
        {
            Debug.Log($"[ArtSetApplicator] Applying style via editor button on {gameObject.name}", gameObject);
            ApplyStyleInternal(true); // Force rebuild and apply
        }

#if UNITY_EDITOR
        /// <summary>
        /// [Editor Only] Ensures necessary components are present for animation elements.
        /// </summary>
        [ContextMenu("Setup Animation Components (Editor)")]
         public void SetupElementComponentsEditor()
         {
             // Use Undo.AddComponent
             Debug.Log($"[ArtSetApplicator] Setting up components for AnimationElements on {gameObject.name}", gameObject);
             int addedGroups = 0;
             int addedApplicators = 0;
             foreach (var element in _animationElements)
             {
                 if (element.targetTransform != null)
                 {
                     GameObject targetGO = element.targetTransform.gameObject;
                     if (targetGO.GetComponent<CanvasGroup>() == null)
                     {
                         Undo.AddComponent<CanvasGroup>(targetGO);
                         addedGroups++;
                     }
                     if (targetGO.GetComponent<AnimationApplicator>() == null)
                     {
                         Undo.AddComponent<AnimationApplicator>(targetGO);
                         addedApplicators++;
                     }
                 }
             }
              if (addedGroups > 0 || addedApplicators > 0)
              {
                  Debug.Log($"[ArtSetApplicator] Setup complete. Added {addedGroups} CanvasGroup(s) and {addedApplicators} AnimationApplicator(s).", gameObject);
              }
              else
              {
                   Debug.Log($"[ArtSetApplicator] Setup complete. All required components already present.", gameObject);
              }
         }
#else
        // Provide a stub or warning if called outside the editor
        public void SetupElementComponentsEditor()
        {
            Debug.LogWarning("[ArtSetApplicator] SetupElementComponentsEditor called outside of Editor.");
        }
#endif // End of conditional compilation block


        // Public getters/properties for editor script
        public ArtSet ResolvedArtSet => _resolvedArtSet; // Renamed for clarity
        public int ArtSetIndexFilter { get => _artSetIndexFilter; set => _artSetIndexFilter = value; } // ADDED
        public bool UseArtSetting => _useArtSetting;
        public ArtStyle ArtStyleOverride { get => _artStyleOverride; set => _artStyleOverride = value; }
        public ArtSet ArtSetFallback { get => _artSetFallback; set => _artSetFallback = value; } // Renamed property
    } // Closing brace for ArtSetApplicator class - Ensure this exists and is correctly placed
} // Closing brace for Modules.ArtStyle namespace - Ensure this exists and is correctly placed
