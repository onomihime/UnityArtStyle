// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using UnityEditor;
using UnityEditorInternal; // Required for ReorderableList
using Modules.ArtStyle;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI; // For component type checks
using TMPro; // For component type checks

namespace Modules.ArtStyle.Editors
{
    [CustomEditor(typeof(ArtSetApplicator))]
    public class ArtSetApplicatorEditor : Editor
    {
        // Target reference
        private ArtSetApplicator _applicator;

        // Serialized Properties
        private SerializedProperty _useArtSettingProp;
        private SerializedProperty _artStyleOverrideProp;
        // private SerializedProperty _artSetTypeFilterProp; // REMOVED
        private SerializedProperty _artSetIndexFilterProp; // ADDED
        private SerializedProperty _artSetFallbackProp;
        private SerializedProperty _applyOnArtSetChangeProp; // ADDED
        // Default Items
        private SerializedProperty _defaultPicItemProp;
        private SerializedProperty _defaultColourItemProp;
        private SerializedProperty _defaultFontItemProp;
        private SerializedProperty _defaultAnimationItemProp;
        // Element Lists
        private SerializedProperty _imageElementsProp;
        private SerializedProperty _legacyTextElementsProp;
        private SerializedProperty _tmpTextElementsProp;
        private SerializedProperty _animationElementsProp;

        // Reorderable Lists
        private ReorderableList _imageList;
        private ReorderableList _legacyTextList;
        private ReorderableList _tmpTextList;
        private ReorderableList _animationList;

        // Foldout states
        private bool _showSourceConfig = true;
        private bool _showDefaults = false;
        private bool _showImageElements = true;
        private bool _showLegacyTextElements = true;
        private bool _showTMPTextElements = true;
        private bool _showAnimationElements = true;
        private bool _showDebugLog = false;

        // State for ArtSet dropdown (based on index)
        private int _selectedArtSetIndex = -1; // Renamed
        private string[] _artSetSlotNames = new string[0]; // Renamed
        // private ArtSetType[] _artSetTypeSlotRefs = new ArtSetType[0]; // REMOVED
        private ArtStyle _lastStyleUsedForDropdown = null;

        // --- Add temporary storage for overrides ---
        private ArtStyle _lastArtStyleOverrideValue = null;
        private ArtSet _lastArtSetFallbackValue = null;
        // -----------------------------------------

        // Resolved ArtSet for element editors (now just reflects runtime value)
        private ArtSet _resolvedArtSetForEditor;
        private ArtSetType _resolvedArtSetTypeForEditor; // Still useful for ItemType lookups

        // Debug Log
        private const int MaxLogEntries = 15;
        private static readonly List<string> _actionLog = new List<string>();

        // Constants for dropdown special items
        private const string OverrideLabel = "[ OVERRIDE ]";
        private const string NoneLabel = "[ NONE / DEFAULT ]"; // Use Set's default item
        private const string UsePicDefaultLabel = "[ USE PIC DEFAULT ]";
        private const string UseFontDefaultLabel = "[ USE FONT DEFAULT ]";

        private void OnEnable()
        {
            _applicator = (ArtSetApplicator)target;
            
            _useArtSettingProp = serializedObject.FindProperty("_useArtSetting");
            _artStyleOverrideProp = serializedObject.FindProperty("_artStyleOverride");
            // _artSetTypeFilterProp = serializedObject.FindProperty("_artSetTypeFilter"); // REMOVED
            _artSetIndexFilterProp = serializedObject.FindProperty("_artSetIndexFilter"); // ADDED
            _artSetFallbackProp = serializedObject.FindProperty("_artSetFallback");
            _applyOnArtSetChangeProp = serializedObject.FindProperty("_applyOnArtSetChange"); // ADDED

            _defaultPicItemProp = serializedObject.FindProperty("_defaultPicItem");
            _defaultColourItemProp = serializedObject.FindProperty("_defaultColourItem");
            _defaultFontItemProp = serializedObject.FindProperty("_defaultFontItem");
            _defaultAnimationItemProp = serializedObject.FindProperty("_defaultAnimationItem");

            _imageElementsProp = serializedObject.FindProperty("_imageElements");
            _legacyTextElementsProp = serializedObject.FindProperty("_legacyTextElements");
            _tmpTextElementsProp = serializedObject.FindProperty("_tmpTextElements");
            _animationElementsProp = serializedObject.FindProperty("_animationElements");

            // Initialize ReorderableLists
            _imageList = CreateElementList(_imageElementsProp, "Image Elements", typeof(ImageArtElement), DrawImageElement);
            _legacyTextList = CreateElementList(_legacyTextElementsProp, "Legacy Text Elements", typeof(LegacyTextArtElement), DrawLegacyTextElement);
            _tmpTextList = CreateElementList(_tmpTextElementsProp, "TMP Text Elements", typeof(TMPTextArtElement), DrawTMPTextElement);
            _animationList = CreateElementList(_animationElementsProp, "Animation Elements", typeof(AnimationElement), DrawAnimationElement);

            // Initial update of resolved set for editor state
            UpdateResolvedSetForEditor(); // Update editor display based on current runtime state
            LogAction("Inspector Enabled");
        }

        private ReorderableList CreateElementList(SerializedProperty property, string header, System.Type elementType, ReorderableList.ElementCallbackDelegate drawCallback)
        {
            var list = new ReorderableList(serializedObject, property, true, true, true, true);
            list.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, header);
            list.drawElementCallback = drawCallback;
            list.elementHeightCallback = (int index) => GetElementHeight(property.GetArrayElementAtIndex(index), elementType);
            return list;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            // Update editor display based on runtime state at start of frame
            UpdateResolvedSetForEditor();

            EditorGUI.BeginChangeCheck(); // Start top-level change check

            bool sourceConfigChangedThisFrame = false;

            // --- Source Configuration ---
            _showSourceConfig = EditorGUILayout.Foldout(_showSourceConfig, "Source Configuration", true, EditorStyles.foldoutHeader);
            if (_showSourceConfig)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck(); // Check for changes to the UseArtSetting toggle
                EditorGUILayout.PropertyField(_useArtSettingProp);
                bool useArtSetting = _useArtSettingProp.boolValue;
                
                
                bool useArtSettingToggleChanged = EditorGUI.EndChangeCheck();
                sourceConfigChangedThisFrame |= useArtSettingToggleChanged;

                ArtStyle styleSourceForFilter = null;
                bool showFilterDropdown = false;
                bool showFallbackField = false;

                // Determine UI state based on toggle and overrides
                if (useArtSetting)
                {
                    // Scenario 1: Use ArtSetting is ON
                    if (ArtSetting.Instance != null)
                    {
                        styleSourceForFilter = ArtSetting.Instance.ActiveArtStyle;
                        showFilterDropdown = styleSourceForFilter != null && styleSourceForFilter.ArtSets != null && styleSourceForFilter.ArtSets.Count > 0;

                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField("Active Style (from Setting)", styleSourceForFilter, typeof(ArtStyle), false);
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("ArtSetting asset not found or assigned.", MessageType.Warning);
                        styleSourceForFilter = null;
                        showFilterDropdown = false;
                    }

                    // Store and clear overrides/fallback if toggle just switched ON
                    if (useArtSettingToggleChanged && useArtSetting)
                    {
                        // ... (Store/Clear logic remains the same) ...
                        bool changed = false;
                        _lastArtStyleOverrideValue = _artStyleOverrideProp.objectReferenceValue as ArtStyle;
                        _lastArtSetFallbackValue = _artSetFallbackProp.objectReferenceValue as ArtSet;
                        if (_artStyleOverrideProp.objectReferenceValue != null) { _artStyleOverrideProp.objectReferenceValue = null; changed = true; }
                        if (_artSetFallbackProp.objectReferenceValue != null) { _artSetFallbackProp.objectReferenceValue = null; changed = true; }
                        if (changed) { LogAction("Stored and cleared overrides/fallback due to enabling Use Art Setting"); sourceConfigChangedThisFrame = true; }
                    }
                }
                else // Use ArtSetting is OFF
                {
                    // Restore overrides/fallback if toggle just switched OFF
                    if (useArtSettingToggleChanged && !useArtSetting)
                    {
                        // ... (Restore logic remains the same) ...
                        bool restored = false;
                        if (_artStyleOverrideProp.objectReferenceValue == null && _lastArtStyleOverrideValue != null) { _artStyleOverrideProp.objectReferenceValue = _lastArtStyleOverrideValue; restored = true; }
                        if (_artSetFallbackProp.objectReferenceValue == null && _lastArtSetFallbackValue != null && _artStyleOverrideProp.objectReferenceValue == null) { _artSetFallbackProp.objectReferenceValue = _lastArtSetFallbackValue; restored = true; }
                        if (restored) { LogAction("Restored overrides/fallback due to disabling Use Art Setting"); sourceConfigChangedThisFrame = true; }
                    }

                    EditorGUI.BeginChangeCheck(); // Check changes to style override
                    EditorGUILayout.PropertyField(_artStyleOverrideProp);
                    bool styleOverrideChanged = EditorGUI.EndChangeCheck();
                    sourceConfigChangedThisFrame |= styleOverrideChanged;

                    ArtStyle styleOverride = _artStyleOverrideProp.objectReferenceValue as ArtStyle;

                    if (styleOverride != null)
                    {
                        // Scenario 2: Use ArtSetting OFF, ArtStyleOverride ON
                        styleSourceForFilter = styleOverride;
                        showFilterDropdown = styleSourceForFilter.ArtSets != null && styleSourceForFilter.ArtSets.Count > 0;

                        // Clear fallback if style override just changed/set
                        if (styleOverrideChanged && _artSetFallbackProp.objectReferenceValue != null)
                        {
                            _artSetFallbackProp.objectReferenceValue = null;
                            LogAction("Cleared direct ArtSet fallback because ArtStyle override was set/changed");
                            sourceConfigChangedThisFrame = true;
                        }
                    }
                    else
                    {
                        // Scenario 3: Use ArtSetting OFF, ArtStyleOverride OFF
                        showFallbackField = true;
                        styleSourceForFilter = null;
                        showFilterDropdown = false; // No style, no dropdown

                        // Reset index filter if style override was just cleared
                        if (styleOverrideChanged && _artSetIndexFilterProp.intValue != 0)
                        {
                            _artSetIndexFilterProp.intValue = 0; // Reset index to 0
                            _selectedArtSetIndex = -1; // Reset display index
                            LogAction("Reset ArtSet index filter because ArtStyle override was cleared");
                            sourceConfigChangedThisFrame = true;
                        }
                    }
                }

                // --- Draw Dropdown or Fallback Field ---
                if (showFilterDropdown)
                {
                    // Update dropdown state (force if toggle/style changed)
                    bool forceDropdownUpdate = useArtSettingToggleChanged || (sourceConfigChangedThisFrame && styleSourceForFilter != _lastStyleUsedForDropdown);
                    UpdateArtSetDropdownState(styleSourceForFilter, forceDropdownUpdate); // Pass the style
                    _lastStyleUsedForDropdown = styleSourceForFilter;

                    // Draw the dropdown
                    EditorGUI.BeginChangeCheck();
                    int newIndex = EditorGUILayout.Popup("Art Set Filter (by Index)", _selectedArtSetIndex, _artSetSlotNames);
                    if (EditorGUI.EndChangeCheck()) // Check if dropdown value changed
                    {
                        _selectedArtSetIndex = newIndex;
                        // --- Set Index Property ---
                        _artSetIndexFilterProp.intValue = _selectedArtSetIndex;
                        LogAction($"Set Filter Index Changed: {newIndex} ('{(_selectedArtSetIndex >= 0 && _selectedArtSetIndex < _artSetSlotNames.Length ? _artSetSlotNames[_selectedArtSetIndex] : "INVALID")}')");
                        // ------------------------
                        sourceConfigChangedThisFrame = true;
                    }
                }
                else if (showFallbackField)
                {
                    // Draw the fallback field
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(_artSetFallbackProp, new GUIContent("Art Set Fallback"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        LogAction($"Fallback Field Changed: {_artSetFallbackProp.objectReferenceValue?.name ?? "None"}");
                        sourceConfigChangedThisFrame = true;
                    }
                    // Ensure dropdown state is cleared if fallback is shown
                    UpdateArtSetDropdownState(null, true);
                    _lastStyleUsedForDropdown = null;
                }
                else
                {
                     // Neither dropdown nor fallback shown
                     UpdateArtSetDropdownState(null, true);
                     _lastStyleUsedForDropdown = null;
                     if (styleSourceForFilter != null) // Style exists but has no sets
                     {
                         EditorGUILayout.HelpBox($"Selected Art Style '{styleSourceForFilter.name}' contains no Art Sets.", MessageType.Warning);
                     }
                }


                // === Immediate Update and Re-resolve if Source Config Changed ===
                if (sourceConfigChangedThisFrame)
                {
                    serializedObject.ApplyModifiedProperties(); // Apply changes (toggle, index, overrides, fallback)
                    // --- Trigger Runtime Resolution ---
                    _applicator.ResolveActiveArtSet(); // Tell the runtime component to resolve based on new props
                    // --------------------------------
                    UpdateResolvedSetForEditor(); // Update editor display based on new runtime state
                    EnsureItemTypeCache(true); // Force refresh as resolution might have changed type
                    LogAction("Applied Source Config Changes and Re-resolved Set");
                }
                // ===============================================================

                // Always display the resolved ArtSet (read-only) using the latest _resolvedArtSetForEditor value
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Resolved Art Set", _resolvedArtSetForEditor, typeof(ArtSet), false);
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            } // End Source Configuration Foldout


            // --- Applicator Buttons ---
            
            // --- ADDED Toggle ---
            EditorGUILayout.PropertyField(_applyOnArtSetChangeProp, new GUIContent("Apply On Art Set Change"));
            // --------------------
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Style Now"))
            {
                //_applicator.ApplyStyleEditor(); // Call the editor-specific method
                LogAction("Applied Style");
                SceneView.RepaintAll(); // Force scene view repaint
            }
            if (GUILayout.Button("Setup Animation Components"))
            {
                 Undo.SetCurrentGroupName("Setup Animation Components");
                 _applicator.SetupElementComponentsEditor(); // Call method
                 Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                 LogAction("Setup Animation Components");
                 SceneView.RepaintAll(); // Also repaint after setup, might change component states visually
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

             // --- Default Fallbacks ---
            _showDefaults = EditorGUILayout.Foldout(_showDefaults, "Default Fallback Items", true, EditorStyles.foldoutHeader);
             if (_showDefaults)
             {
                 EditorGUI.indentLevel++;
                 EditorGUILayout.PropertyField(_defaultPicItemProp);
                 EditorGUILayout.PropertyField(_defaultColourItemProp);
                 EditorGUILayout.PropertyField(_defaultFontItemProp);
                 EditorGUILayout.PropertyField(_defaultAnimationItemProp);
                 EditorGUI.indentLevel--;
                 EditorGUILayout.Space();
             }


            // --- Element Lists ---
            _showImageElements = EditorGUILayout.Foldout(_showImageElements, "Image Elements", true, EditorStyles.foldoutHeader);
            if (_showImageElements) _imageList.DoLayoutList();

            _showLegacyTextElements = EditorGUILayout.Foldout(_showLegacyTextElements, "Legacy Text Elements", true, EditorStyles.foldoutHeader);
            if (_showLegacyTextElements) _legacyTextList.DoLayoutList();

            _showTMPTextElements = EditorGUILayout.Foldout(_showTMPTextElements, "TMP Text Elements", true, EditorStyles.foldoutHeader);
            if (_showTMPTextElements) _tmpTextList.DoLayoutList();

            _showAnimationElements = EditorGUILayout.Foldout(_showAnimationElements, "Animation Elements", true, EditorStyles.foldoutHeader);
            if (_showAnimationElements) _animationList.DoLayoutList();

            EditorGUILayout.Space();

            // --- Debug Log ---
             _showDebugLog = EditorGUILayout.Foldout(_showDebugLog, "Action Log", true, EditorStyles.foldoutHeader);
             if (_showDebugLog)
             {
                 EditorGUI.indentLevel++;
                 EditorGUI.BeginDisabledGroup(true);
                 int start = Mathf.Max(0, _actionLog.Count - MaxLogEntries);
                 for (int i = start; i < _actionLog.Count; i++)
                 {
                     EditorGUILayout.LabelField(_actionLog[i]);
                 }
                 EditorGUI.EndDisabledGroup();
                  if (GUILayout.Button("Clear Log", GUILayout.Width(80)))
                  {
                      _actionLog.Clear();
                  }
                 EditorGUI.indentLevel--;
             }


            // Apply remaining changes from the entire inspector at the end
            if (EditorGUI.EndChangeCheck())
            {
                LogAction("Inspector Value Changed (Non-Source)");
                serializedObject.ApplyModifiedProperties();
                // Re-resolve runtime state only if source config didn't already trigger it this frame
                if (!sourceConfigChangedThisFrame)
                {
                    _applicator.ResolveActiveArtSet();
                    UpdateResolvedSetForEditor(); // Update display
                    EnsureItemTypeCache(true); // Force cache refresh on any change
                }
            }
        }

        // --- ReorderableList Draw Callbacks ---

        private void DrawImageElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty elementProp = _imageList.serializedProperty.GetArrayElementAtIndex(index);
            Rect lineRect = new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight);

            // Target Image Field
            SerializedProperty targetProp = elementProp.FindPropertyRelative("targetImage");
            EditorGUI.PropertyField(lineRect, targetProp, new GUIContent("Target Image"));
            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Layout: Preview on Left, Controls on Right
            float previewWidth = 60f;
            float spacing = 5f;
            Rect previewArea = new Rect(lineRect.x, lineRect.y, previewWidth, 60f); // Allocate space for preview + color bar
            Rect controlsArea = new Rect(lineRect.x + previewWidth + spacing, lineRect.y, rect.width - previewWidth - spacing, rect.height - (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing));

            // --- Preview ---
            DrawImagePreview(previewArea, elementProp);

            // --- Controls ---
            Rect controlLineRect = new Rect(controlsArea.x, controlsArea.y, controlsArea.width, EditorGUIUtility.singleLineHeight);

            // PicItem Dropdown - Use picItemIndex
            DrawItemTypeDropdown(controlLineRect, elementProp, "picItemIndex", "overrideSpriteFlag", false, GetPicItemTypeNames(), "Pic Item", index);
            controlLineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Show Pic Override Field
            if (elementProp.FindPropertyRelative("overrideSpriteFlag").boolValue)
            {
                EditorGUI.PropertyField(controlLineRect, elementProp.FindPropertyRelative("overrideSprite"), new GUIContent("Override Sprite"));
                controlLineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            // ColourItem Dropdown - Use colourItemIndex
            DrawItemTypeDropdown(controlLineRect, elementProp, "colourItemIndex", "overrideColourFlag", true, GetColourItemTypeNames(), "Tint Colour", index, "usePicDefaultColourFlag", UsePicDefaultLabel);
            controlLineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Show Colour Override Field
            if (elementProp.FindPropertyRelative("overrideColourFlag").boolValue)
            {
                EditorGUI.PropertyField(controlLineRect, elementProp.FindPropertyRelative("overrideColour"), new GUIContent("Override Colour"));
                controlLineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private void DrawLegacyTextElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty elementProp = _legacyTextList.serializedProperty.GetArrayElementAtIndex(index);
            Rect lineRect = new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight);

            // Target Text Field
            SerializedProperty targetProp = elementProp.FindPropertyRelative("targetText");
            EditorGUI.PropertyField(lineRect, targetProp, new GUIContent("Target Text"));
            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // FontItem Dropdown - Use fontItemIndex
            DrawItemTypeDropdown(lineRect, elementProp, "fontItemIndex", "overrideFontFlag", false, GetFontItemTypeNames(), "Font Item", index);
            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Show Font Override Field
            if (elementProp.FindPropertyRelative("overrideFontFlag").boolValue)
            {
                EditorGUI.PropertyField(lineRect, elementProp.FindPropertyRelative("overrideFont"), new GUIContent("Override Font"));
                lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            // ColourItem Dropdown - Use colourItemIndex
            DrawItemTypeDropdown(lineRect, elementProp, "colourItemIndex", "overrideColourFlag", true, GetColourItemTypeNames(), "Text Colour", index, "useFontDefaultColourFlag", UseFontDefaultLabel);
            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Show Colour Override Field
            if (elementProp.FindPropertyRelative("overrideColourFlag").boolValue)
            {
                EditorGUI.PropertyField(lineRect, elementProp.FindPropertyRelative("overrideColour"), new GUIContent("Override Colour"));
                lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private void DrawTMPTextElement(Rect rect, int index, bool isActive, bool isFocused)
        {
             SerializedProperty elementProp = _tmpTextList.serializedProperty.GetArrayElementAtIndex(index);
            Rect lineRect = new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight);

            // Target Text Field
            SerializedProperty targetProp = elementProp.FindPropertyRelative("targetText");
            EditorGUI.PropertyField(lineRect, targetProp, new GUIContent("Target Text (TMP)"));
            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // FontItem Dropdown - Use fontItemIndex
            DrawItemTypeDropdown(lineRect, elementProp, "fontItemIndex", "overrideFontFlag", false, GetFontItemTypeNames(), "Font Item", index);
            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Show Font Override Field
            if (elementProp.FindPropertyRelative("overrideFontFlag").boolValue)
            {
                EditorGUI.PropertyField(lineRect, elementProp.FindPropertyRelative("overrideTmpFont"), new GUIContent("Override Font (TMP)"));
                lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            // ColourItem Dropdown - Use colourItemIndex
            DrawItemTypeDropdown(lineRect, elementProp, "colourItemIndex", "overrideColourFlag", true, GetColourItemTypeNames(), "Text Colour", index, "useFontDefaultColourFlag", UseFontDefaultLabel);
            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Show Colour Override Field
            if (elementProp.FindPropertyRelative("overrideColourFlag").boolValue)
            {
                EditorGUI.PropertyField(lineRect, elementProp.FindPropertyRelative("overrideColour"), new GUIContent("Override Colour"));
                lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private void DrawAnimationElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty elementProp = _animationList.serializedProperty.GetArrayElementAtIndex(index);
            Rect lineRect = new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight);

            // Target Transform Field
            SerializedProperty targetProp = elementProp.FindPropertyRelative("targetTransform");
            EditorGUI.PropertyField(lineRect, targetProp, new GUIContent("Target Transform"));
            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // AnimationItem Dropdown - Use animationItemIndex
            DrawItemTypeDropdown(lineRect, elementProp, "animationItemIndex", "overrideAnimationFlag", false, GetAnimationItemTypeNames(), "Animation Item", index);
            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Show Animation Override Field or Readonly Info
            SerializedProperty overrideFlagProp = elementProp.FindPropertyRelative("overrideAnimationFlag");
            if (overrideFlagProp.boolValue)
            {
                EditorGUI.PropertyField(lineRect, elementProp.FindPropertyRelative("overrideAnimationItem"), new GUIContent("Override Item"));
                lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            else
            {
                 // Show resolved item info (read-only) - Resolve based on index
                 AnimationItem resolvedItem = ResolveAnimationItemForPreview(elementProp); // Use new helper
                 EditorGUI.BeginDisabledGroup(true);
                 EditorGUI.ObjectField(lineRect, "Resolved Item", resolvedItem, typeof(AnimationItem), false);
                 lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                 if(resolvedItem != null)
                 {
                     EditorGUI.FloatField(lineRect, "Duration", resolvedItem.Duration);
                     lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                     EditorGUI.Toggle(lineRect, "Use Fade", resolvedItem.UseFade);
                     lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                 }
                 EditorGUI.EndDisabledGroup();
            }
        }

        // --- Height Calculation ---
        private float GetElementHeight(SerializedProperty elementProp, System.Type elementType)
        {
            float standardHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            float totalHeight = standardHeight; // For target component field

            if (elementType == typeof(ImageArtElement))
            {
                totalHeight = standardHeight * 3; // Target, Pic Dropdown, Colour Dropdown
                if (elementProp.FindPropertyRelative("overrideSpriteFlag").boolValue) totalHeight += standardHeight;
                if (elementProp.FindPropertyRelative("overrideColourFlag").boolValue) totalHeight += standardHeight;
                totalHeight = Mathf.Max(totalHeight, 60f + standardHeight); // Ensure enough space for preview
            }
            else if (elementType == typeof(LegacyTextArtElement))
            {
                totalHeight = standardHeight * 3; // Target, Font Dropdown, Colour Dropdown
                if (elementProp.FindPropertyRelative("overrideFontFlag").boolValue) totalHeight += standardHeight;
                if (elementProp.FindPropertyRelative("overrideColourFlag").boolValue) totalHeight += standardHeight;
            }
            else if (elementType == typeof(TMPTextArtElement))
            {
                 totalHeight = standardHeight * 3; // Target, Font Dropdown, Colour Dropdown
                if (elementProp.FindPropertyRelative("overrideFontFlag").boolValue) totalHeight += standardHeight;
                if (elementProp.FindPropertyRelative("overrideColourFlag").boolValue) totalHeight += standardHeight;
            }
            else if (elementType == typeof(AnimationElement))
            {
                totalHeight = standardHeight * 2; // Target, Anim Dropdown
                if (elementProp.FindPropertyRelative("overrideAnimationFlag").boolValue)
                {
                     totalHeight += standardHeight; // Override field
                }
                else
                {
                    // Check if resolved item exists to estimate readonly height
                     AnimationItem resolvedItem = _applicator.ResolveAnimationItem(
                         _applicator._animationElements.Count > elementProp.GetIndex() ? _applicator._animationElements[elementProp.GetIndex()] : null
                     );
                     if (resolvedItem != null) totalHeight += standardHeight * 3; // Resolved Item, Duration, UseFade
                }
            }

            return totalHeight + 4f; // Add padding
        }

        // --- Helper Methods ---

        /// <summary>
        /// Updates the editor's internal state (_resolvedArtSetForEditor, _resolvedArtSetTypeForEditor)
        /// based on the current state of the runtime ArtSetApplicator.
        /// </summary>
        private void UpdateResolvedSetForEditor()
        {
            // Read the currently resolved set from the runtime component
            ArtSet currentRuntimeResolvedSet = _applicator.ResolvedArtSet; // Use the public property

            // Update editor display variables if they differ from runtime
            if (_resolvedArtSetForEditor != currentRuntimeResolvedSet)
            {
                _resolvedArtSetForEditor = currentRuntimeResolvedSet;
                _resolvedArtSetTypeForEditor = _resolvedArtSetForEditor?.SetType; // Get type from the resolved set
                EnsureItemTypeCache(true); // Refresh item cache based on the new type
                // Log is now handled when sourceConfigChangedThisFrame triggers resolution
                // LogAction($"Editor Display Updated: Resolved Set='{_resolvedArtSetForEditor?.name ?? "None"}'");
            }
            // Also handle case where set is same reference but type might have changed internally (less likely now)
            else if (_resolvedArtSetForEditor != null && _resolvedArtSetTypeForEditor != _resolvedArtSetForEditor.SetType)
            {
                 _resolvedArtSetTypeForEditor = _resolvedArtSetForEditor.SetType;
                 EnsureItemTypeCache(true);
                 LogAction($"Editor Display Updated: Resolved Set Type='{_resolvedArtSetTypeForEditor?.name ?? "None"}'");
            }
             // Ensure type is null if set is null
            else if (_resolvedArtSetForEditor == null && _resolvedArtSetTypeForEditor != null)
            {
                 _resolvedArtSetTypeForEditor = null;
                 EnsureItemTypeCache(true);
                 LogAction("Editor Display Updated: Resolved Set Type Cleared");
            }
        }


        /// <summary>
        /// Updates the state (_artSetSlotNames, _selectedArtSetIndex) for the ArtSet dropdown
        /// based on the provided ArtStyle.
        /// </summary>
        private void UpdateArtSetDropdownState(ArtStyle styleSource, bool forceUpdate = false) // Renamed, changed param
        {
            int currentFilterIndex = _artSetIndexFilterProp.intValue; // Read current index value

            // Check if the source style has changed
            bool contextChanged = forceUpdate || styleSource != _lastStyleUsedForDropdown;

            if (contextChanged)
            {
                _lastStyleUsedForDropdown = styleSource; // Update tracked style
                if (styleSource != null && styleSource.ArtSets != null)
                {
                    // Populate names directly from the ArtSets in the style
                    _artSetSlotNames = styleSource.ArtSets
                                        .Select((set, index) => set != null ? $"{index}: {set.name}" : $"{index}: <NULL SET>")
                                        .ToArray();
                    LogAction($"Dropdown Updated ({_artSetSlotNames.Length} sets from style '{styleSource.name}')");
                    
                    _applicator.ResolveActiveArtSet();
                    
                }
                else
                {
                    // No style or style has no sets
                    _artSetSlotNames = new string[0];
                    LogAction("Dropdown Cleared (No Style or No Sets in Style)");
                }
            }

            // Validate and set the selected index for the dropdown display
            if (_artSetSlotNames.Length > 0 && currentFilterIndex >= 0 && currentFilterIndex < _artSetSlotNames.Length)
            {
                _selectedArtSetIndex = currentFilterIndex;
            }
            else
            {
                // Index is out of bounds or no slots available, display as unselected
                _selectedArtSetIndex = -1;
                // Optionally reset the property if the index is invalid due to context change?
                // if (contextChanged && currentFilterIndex != -1 && _artSetSlotNames.Length > 0)
                // {
                //     _artSetIndexFilterProp.intValue = 0; // Reset to 0 if context changed and old index invalid?
                //     _selectedArtSetIndex = 0;
                //     LogAction("Dropdown selection reset to 0 due to context change / invalid index");
                // }
                 if (contextChanged && currentFilterIndex != -1) // If context changed and index *was* set
                 {
                      // Don't automatically reset to 0, just reflect invalid state
                      LogAction($"Dropdown selection index {currentFilterIndex} is now invalid for new context.");
                 }
            }
        }


        // Generic method to draw the ItemType dropdown - Modified for Index Property
        private void DrawItemTypeDropdown(Rect rect, SerializedProperty elementProp, string itemIndexPropName, string overrideFlagPropName, bool hasDefaultOption, string[] itemTypeNames, string label, int elementIndex, string defaultFlagPropName = null, string defaultLabel = null)
        {
            SerializedProperty itemIndexProp = elementProp.FindPropertyRelative(itemIndexPropName);
            SerializedProperty overrideFlagProp = elementProp.FindPropertyRelative(overrideFlagPropName);
            SerializedProperty defaultFlagProp = !string.IsNullOrEmpty(defaultFlagPropName) ? elementProp.FindPropertyRelative(defaultFlagPropName) : null;

            // --- Reorder Options ---
            List<string> options = new List<string>();
            options.AddRange(itemTypeNames); // Add actual item names first (Indices 0, 1, 2...)

            // Calculate indices for special items based on the number of actual items
            int noneIndexInPopup = itemTypeNames.Length; // Index of NoneLabel in the popup list
            int defaultOptionIndexInPopup = hasDefaultOption ? noneIndexInPopup + 1 : -1; // Index of DefaultLabel in the popup list
            int overrideIndexInPopup = noneIndexInPopup + (hasDefaultOption ? 2 : 1); // Index of OverrideLabel in the popup list

            options.Add(NoneLabel); // Add None/Default
            if (hasDefaultOption && defaultFlagProp != null && !string.IsNullOrEmpty(defaultLabel))
            {
                options.Add(defaultLabel); // Add optional default (e.g., Use Pic Default)
            }
            options.Add(OverrideLabel); // Add Override last

            // --- Determine Current Index for Popup Display ---
            int currentPopupIndex = noneIndexInPopup; // Default to showing "None/Default"

            if (overrideFlagProp.boolValue)
            {
                currentPopupIndex = overrideIndexInPopup; // Override selected
            }
            else if (hasDefaultOption && defaultFlagProp != null && defaultFlagProp.boolValue)
            {
                currentPopupIndex = defaultOptionIndexInPopup; // Default option selected
            }
            else
            {
                int storedIndex = itemIndexProp.intValue;
                if (storedIndex >= 0 && storedIndex < itemTypeNames.Length)
                {
                    // Stored index corresponds to one of the actual item types
                    currentPopupIndex = storedIndex;
                }
                else // storedIndex is -1 or out of bounds for itemTypeNames
                {
                    currentPopupIndex = noneIndexInPopup; // Show as None/Default
                    // Ensure property is actually -1 if it was out of bounds but not -1
                    if (storedIndex != -1)
                    {
                         // itemIndexProp.intValue = -1; // Optionally force sync property if invalid
                         // LogAction($"Corrected invalid index {storedIndex} to -1 for {label} element {elementIndex}");
                    }
                }
            }


            // --- Draw Popup and Handle Changes ---
            EditorGUI.BeginChangeCheck();
            int newPopupIndex = EditorGUI.Popup(rect, label, currentPopupIndex, options.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                LogAction($"Changed {label} for element {elementIndex}");

                // Reset flags initially
                overrideFlagProp.boolValue = false;
                if (defaultFlagProp != null) defaultFlagProp.boolValue = false;

                if (newPopupIndex < itemTypeNames.Length) // An actual ItemType selected (Indices 0, 1, 2...)
                {
                    itemIndexProp.intValue = newPopupIndex; // Store the selected type index (0, 1, 2...)
                }
                else if (newPopupIndex == noneIndexInPopup) // None/Default selected
                {
                    itemIndexProp.intValue = -1; // Store -1 for None/Default
                }
                else if (hasDefaultOption && newPopupIndex == defaultOptionIndexInPopup) // Default option selected
                {
                    if (defaultFlagProp != null) defaultFlagProp.boolValue = true;
                    itemIndexProp.intValue = -1; // Store -1 when using default flag
                }
                else if (newPopupIndex == overrideIndexInPopup) // Override selected
                {
                    overrideFlagProp.boolValue = true;
                    itemIndexProp.intValue = -1; // Store -1 when using override
                }
            }
        }

        // --- ItemType Name/ID Helpers (Requires Resolved ArtSetType) ---

        private string[] GetPicItemTypeNames() => GetItemTypeNames(_resolvedArtSetTypeForEditor?.PicItemTypes);
        private string[] GetColourItemTypeNames() => GetItemTypeNames(_resolvedArtSetTypeForEditor?.ColourItemTypes);
        private string[] GetFontItemTypeNames() => GetItemTypeNames(_resolvedArtSetTypeForEditor?.FontItemTypes);
        private string[] GetAnimationItemTypeNames() => GetItemTypeNames(_resolvedArtSetTypeForEditor?.AnimationItemTypes);

        private string[] GetItemTypeNames<T>(List<T> itemTypes) where T : ItemType
        {
            if (itemTypes == null) return new string[0];
            // Return all names, including the one at index 0 (if it exists)
            // The "Default" concept is handled by the NoneLabel in the dropdown.
            return itemTypes.Select(t => t?.Name ?? "<INVALID TYPE>").ToArray(); // Removed Skip(1)
        }

        // Cache for all types to avoid repeated LINQ evaluation
        private IEnumerable<ItemType> _cachedAllItemTypes = null;
        private ArtSetType _cachedArtSetTypeForHelpers = null;

        // Added forceRefresh parameter
        private void EnsureItemTypeCache(bool forceRefresh = false)
        {
             if (forceRefresh || _resolvedArtSetTypeForEditor != _cachedArtSetTypeForHelpers || _cachedAllItemTypes == null)
             {
                 // LogAction("Refreshing ItemType Cache"); // Optional: for debugging cache refreshes
                 _cachedArtSetTypeForHelpers = _resolvedArtSetTypeForEditor;
                 if (_cachedArtSetTypeForHelpers == null)
                 {
                     _cachedAllItemTypes = Enumerable.Empty<ItemType>();
                 }
                 else
                 {
                     // Ensure lists are not null before trying to cast/concat
                     var picTypes = _resolvedArtSetTypeForEditor.PicItemTypes?.Cast<ItemType>() ?? Enumerable.Empty<ItemType>();
                     var colourTypes = _resolvedArtSetTypeForEditor.ColourItemTypes?.Cast<ItemType>() ?? Enumerable.Empty<ItemType>();
                     var fontTypes = _resolvedArtSetTypeForEditor.FontItemTypes?.Cast<ItemType>() ?? Enumerable.Empty<ItemType>();
                     var animTypes = _resolvedArtSetTypeForEditor.AnimationItemTypes?.Cast<ItemType>() ?? Enumerable.Empty<ItemType>();

                     _cachedAllItemTypes = picTypes.Concat(colourTypes).Concat(fontTypes).Concat(animTypes).ToList(); // Execute LINQ query and cache the list
                 }
             }
        }


        // --- Preview Drawing ---
        // Modified to accept SerializedProperty and resolve based on editor state
        private void DrawImagePreview(Rect area, SerializedProperty elementProp)
        {
            // --- DEBUG: Log the resolved set being used for this preview ---
            // Debug.Log($"[Preview] Drawing preview for element {elementProp.GetIndex()}. Resolved ArtSet: '{_resolvedArtSetForEditor?.name ?? "None"}'");
            // -------------------------------------------------------------

            // Resolve Sprite and Color based on current element settings in the editor
            Sprite previewSprite = ResolveSpriteForPreview(elementProp);
            Color previewColor = ResolveColourForPreview(elementProp);

            // Draw Sprite Preview (similar to ArtSetItemEditorUtils)
            Rect spriteRect = new Rect(area.x, area.y, area.width, area.width); // Square preview
            Rect colorRect = new Rect(area.x, area.y + area.width + 2, area.width, EditorGUIUtility.singleLineHeight * 0.5f); // Small color bar

            EditorGUI.DrawRect(spriteRect, new Color(0.5f, 0.5f, 0.5f, 0.1f)); // Background for sprite area

            if (previewSprite != null && previewSprite.texture != null)
            {
                Texture tex = previewSprite.texture;
                Rect texCoords = previewSprite.textureRect;
                texCoords.x /= tex.width;
                texCoords.y /= tex.height;
                texCoords.width /= tex.width;
                texCoords.height /= tex.height;

                float spriteW = previewSprite.rect.width;
                float spriteH = previewSprite.rect.height;
                float aspect = spriteW / spriteH;
                Rect spriteDrawRect = spriteRect;

                if (aspect >= 1) // Wider or square
                {
                    spriteDrawRect.height = spriteRect.width / aspect;
                    spriteDrawRect.y += (spriteRect.height - spriteDrawRect.height) * 0.5f;
                }
                else // Taller
                {
                    spriteDrawRect.width = spriteRect.height * aspect;
                    spriteDrawRect.x += (spriteRect.width - spriteDrawRect.width) * 0.5f;
                }
                var prevTint = GUI.color; GUI.color = Color.white; // Use white tint for preview
                GUI.DrawTextureWithTexCoords(spriteDrawRect, tex, texCoords, true);
                GUI.color = prevTint;
            }
            else
            {
                // Draw placeholder if no sprite
                var c = GUI.color; GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.5f);
                GUI.Box(spriteRect, GUIContent.none, EditorStyles.helpBox); GUI.color = c;
            }

            // Draw Color Bar
            EditorGUI.DrawRect(colorRect, previewColor);
            EditorGUI.DrawRect(colorRect, new Color(0,0,0,0.3f)); // Border
        }

        // --- UPDATED: Editor-side resolution helpers for preview (using Index) ---

        private Sprite ResolveSpriteForPreview(SerializedProperty elementProp)
        {
            if (elementProp.FindPropertyRelative("overrideSpriteFlag").boolValue)
            {
                Sprite overrideSprite = elementProp.FindPropertyRelative("overrideSprite").objectReferenceValue as Sprite;
                // Debug.Log($"[Preview] Using Override Sprite: '{overrideSprite?.name ?? "None"}'");
                return overrideSprite;
            }

            int picIndex = elementProp.FindPropertyRelative("picItemIndex").intValue;
            PicItem resolvedPicItem = null;

            if (_resolvedArtSetForEditor?.PicSet?.Items != null)
            {
                var picItems = _resolvedArtSetForEditor.PicSet.Items;
                int targetListIndex = -1;

                if (picIndex == -1) // Corresponds to None/Default
                {
                    targetListIndex = 0; // Use Item[0] from the PicSet list
                }
                else if (picIndex >= 0) // Corresponds to Type Index 0, 1, 2...
                {
                    targetListIndex = picIndex + 1; // Use Item[1], Item[2], Item[3]... from PicSet list
                }

                if (targetListIndex >= 0 && targetListIndex < picItems.Count)
                {
                    resolvedPicItem = picItems[targetListIndex];
                    // Debug.Log($"[Preview] Attempting PicItem from Set Index {targetListIndex} (Type Index {picIndex}): '{resolvedPicItem?.name ?? "None"}'");
                }
                else
                {
                     // Debug.LogWarning($"[Preview] PicItem index {targetListIndex} (Type Index {picIndex}) out of bounds for ArtSet '{_resolvedArtSetForEditor.name}'.");
                     // Fallback to Item[0] if specific index is invalid but Item[0] exists
                     if (picItems.Count > 0) resolvedPicItem = picItems[0];
                }
            }

            if (resolvedPicItem != null)
            {
                // Debug.Log($"[Preview] Using Sprite from resolved PicItem: '{resolvedPicItem.Sprite?.name ?? "None"}'");
                return resolvedPicItem.Sprite;
            }

            // Fallback to applicator's default PicItem
            PicItem defaultPicItem = _defaultPicItemProp.objectReferenceValue as PicItem;
            if (defaultPicItem != null)
            {
                 // Debug.Log($"[Preview] Using Sprite from Default Applicator PicItem: '{defaultPicItem.Sprite?.name ?? "None"}'");
                 return defaultPicItem.Sprite; // Extract Sprite from the default item
            }

            // Debug.LogWarning("[Preview] No Sprite found (Override, Set Item, or Default).");
            return null; // Return null if no sprite found
        }

        private Color ResolveColourForPreview(SerializedProperty elementProp)
        {
            if (elementProp.FindPropertyRelative("overrideColourFlag").boolValue)
            {
                Color overrideColor = elementProp.FindPropertyRelative("overrideColour").colorValue;
                // Debug.Log($"[Preview] Using Override Colour: {overrideColor}");
                return overrideColor;
            }

            if (elementProp.FindPropertyRelative("usePicDefaultColourFlag").boolValue)
            {
                // Resolve PicItem first using its index to get its default color
                int picIndex = elementProp.FindPropertyRelative("picItemIndex").intValue;
                PicItem resolvedPicItemForColour = null;
                 if (_resolvedArtSetForEditor?.PicSet?.Items != null)
                 {
                     var picItems = _resolvedArtSetForEditor.PicSet.Items;
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

                if (resolvedPicItemForColour != null)
                {
                    // Debug.Log($"[Preview] Using Default Colour from PicItem '{resolvedPicItemForColour.name}': {resolvedPicItemForColour.DefaultColour}");
                    return resolvedPicItemForColour.DefaultColour; // Use the PicItem's DefaultColour field
                }
                // Fallback if PicItem not found - use applicator default color
                ColourItem defaultColourItem = _defaultColourItemProp.objectReferenceValue as ColourItem;
                Color fallbackColor = defaultColourItem != null ? defaultColourItem.Colour : Color.white;
                // Debug.Log($"[Preview] PicItem for default colour not found. Using Applicator Default Colour: {fallbackColor}");
                return fallbackColor;
            }

            // Try resolving specific ColourItem using index
            int colourIndex = elementProp.FindPropertyRelative("colourItemIndex").intValue;
            ColourItem resolvedColourItem = null;

             if (_resolvedArtSetForEditor?.ColourSet?.Items != null)
            {
                var colourItems = _resolvedArtSetForEditor.ColourSet.Items;
                int targetListIndex = -1;

                if (colourIndex == -1) // Corresponds to None/Default
                {
                    targetListIndex = 0; // Use Item[0] from the ColourSet list
                }
                else if (colourIndex >= 0) // Corresponds to Type Index 0, 1, 2...
                {
                    targetListIndex = colourIndex + 1; // Use Item[1], Item[2], Item[3]... from ColourSet list
                }

                if (targetListIndex >= 0 && targetListIndex < colourItems.Count)
                {
                    resolvedColourItem = colourItems[targetListIndex];
                    // Debug.Log($"[Preview] Attempting ColourItem from Set Index {targetListIndex} (Type Index {colourIndex}): '{resolvedColourItem?.name ?? "None"}'");
                }
                 else
                {
                     // Debug.LogWarning($"[Preview] ColourItem index {targetListIndex} (Type Index {colourIndex}) out of bounds for ArtSet '{_resolvedArtSetForEditor.name}'.");
                     // Fallback to Item[0] if specific index is invalid but Item[0] exists
                     if (colourItems.Count > 0) resolvedColourItem = colourItems[0];
                }
            }


            if (resolvedColourItem != null)
            {
                // Debug.Log($"[Preview] Using Colour from resolved ColourItem: {resolvedColourItem.Colour}");
                return resolvedColourItem.Colour;
            }

            // Fallback to applicator's default color
            ColourItem appDefaultColourItem = _defaultColourItemProp.objectReferenceValue as ColourItem;
            Color finalFallbackColor = appDefaultColourItem != null ? appDefaultColourItem.Colour : Color.white;
            // Debug.Log($"[Preview] Specific ColourItem not found. Using Applicator Default Colour: {finalFallbackColor}");
            return finalFallbackColor;
        }

        // NEW: Editor-side resolution helper for AnimationItem preview
        private AnimationItem ResolveAnimationItemForPreview(SerializedProperty elementProp)
        {
             if (elementProp.FindPropertyRelative("overrideAnimationFlag").boolValue)
             {
                 // Use override item, but fallback to default if override is null
                 AnimationItem overrideItem = elementProp.FindPropertyRelative("overrideAnimationItem").objectReferenceValue as AnimationItem;
                 return overrideItem ?? (_defaultAnimationItemProp.objectReferenceValue as AnimationItem);
             }

             int animIndex = elementProp.FindPropertyRelative("animationItemIndex").intValue;
             AnimationItem resolvedAnimItem = null;

             if (_resolvedArtSetForEditor?.AnimationSet?.Items != null)
             {
                 var animItems = _resolvedArtSetForEditor.AnimationSet.Items;
                 int targetListIndex = (animIndex == -1) ? 0 : animIndex + 1;

                 if (targetListIndex >= 0 && targetListIndex < animItems.Count)
                 {
                     resolvedAnimItem = animItems[targetListIndex];
                 }
                 else if (animItems.Count > 0) // Fallback to Item[0] if index invalid
                 {
                      resolvedAnimItem = animItems[0];
                 }
             }

             // Use resolved item or fallback to applicator default
             return resolvedAnimItem ?? (_defaultAnimationItemProp.objectReferenceValue as AnimationItem);
        }

        // REMOVED FindPicItemInSet
        // REMOVED FindColourItemInSet


        // --- Logging ---
        private static void LogAction(string action)
        {
             string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
             _actionLog.Add($"[{timestamp}] {action}");
             while (_actionLog.Count > MaxLogEntries)
             {
                 _actionLog.RemoveAt(0);
             }
        }
    }

    // --- New Static Class for Extension Methods ---
    public static class EditorExtensions
    {
        /// <summary>
        /// Gets the array index from a SerializedProperty path (e.g., "myList.Array.data[3]").
        /// </summary>
        public static int GetIndex(this SerializedProperty property)
        {
            string path = property.propertyPath;
            int startIndex = path.LastIndexOf('[');
            int endIndex = path.LastIndexOf(']');
            if (startIndex != -1 && endIndex != -1 && endIndex > startIndex + 1)
            {
                if (int.TryParse(path.Substring(startIndex + 1, endIndex - startIndex - 1), out int index))
                {
                    return index;
                }
            }
            Debug.LogWarning($"Could not parse index from property path: {path}");
            return -1; // Indicate error or not an array element
        }
    }
}
