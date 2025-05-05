// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using UnityEditor;
using Modules.ArtStyle;
using System; // Required for Type
using System.Collections.Generic; // Required for List
using System.Reflection; // Required for reflection
using System.Collections; // Required for IList

namespace Modules.ArtStyle.Editors
{
    [CustomEditor(typeof(ArtSet))]
    public class ArtSetEditor : Editor
    {
        private SerializedProperty _idProp;
        private SerializedProperty _nameProp;
        private SerializedProperty _setTypeProp;
        private SerializedProperty _picSetProp;
        private SerializedProperty _colourSetProp;
        private SerializedProperty _fontSetProp;
        private SerializedProperty _animationSetProp;

        // Foldout states
        private bool _showSetTypeSection = false;
        private bool _showSetsSection = false;
        private bool _showPicSetSection = true;
        private bool _showColourSetSection = true; // New
        private bool _showFontSetSection = true;   // New
        private bool _showAnimationSetSection = true; // New

        // State for inline ArtSetType creation UI (using bool, though unused when createAsAsset=true)
        private bool _creatingInlineSetType = false; // Back to bool
        private string _newSetTypeName = "New ArtSetType";

        // State for inline Set creation UI (using bool)
        private bool _creatingInlinePicSet = false; // Back to bool
        private string _newPicSetName = "New Inline PicSet";
        private bool _creatingInlineColourSet = false; // Back to bool
        private string _newColourSetName = "New Inline ColourSet";
        private bool _creatingInlineFontSet = false; // Back to bool
        private string _newFontSetName = "New Inline FontSet";
        private bool _creatingInlineAnimationSet = false; // Back to bool
        private string _newAnimationSetName = "New Inline AnimationSet";

        // State for inline PicItem creation UI (using int index)
        private int _creatingInlinePicItemIndex = -1;
        private string _newPicItemName = "New Inline PicItem";
        private Sprite _newPicItemSprite = null; // New state variable
        private Color _newPicItemColour = Color.white; // New state variable

        // State for inline ColourItem creation UI
        private int _creatingInlineColourItemIndex = -1; // New
        private string _newColourItemName = "New Inline ColourItem"; // New
        private Color _newColourItemColour = Color.white; // New

        // State for inline FontItem reference creation UI
        private int _creatingInlineFontItemRefIndex = -1; // Changed from bool to int
        private string _newFontItemRefName = "New Inline FontItem"; // New

        // State for inline AnimationItem reference creation UI
        private int _creatingInlineAnimationItemRefIndex = -1; // Changed from bool to int
        private string _newAnimationItemRefName = "New Inline AnimationItem"; // New


        // Cache for the inline ArtSetType editor
        private Editor _artSetTypeEditorInstance;

        private void OnEnable()
        {
            _idProp = serializedObject.FindProperty("_id");
            _nameProp = serializedObject.FindProperty("_name");
            _setTypeProp = serializedObject.FindProperty("_setType");
            _picSetProp = serializedObject.FindProperty("_picSet");
            _colourSetProp = serializedObject.FindProperty("_colourSet");
            _fontSetProp = serializedObject.FindProperty("_fontSet");
            _animationSetProp = serializedObject.FindProperty("_animationSet");

            // Ensure editor instance is cleaned up initially or on re-selection
            DestroyImmediate(_artSetTypeEditorInstance);

            // Check default items (can be expanded)
            CheckDefaultItem(_picSetProp, "PicSet");
            CheckDefaultItem(_colourSetProp, "ColourSet");
            CheckDefaultItem(_fontSetProp, "FontSet");
            CheckDefaultItem(_animationSetProp, "AnimationSet");
        }

        // Helper to check default item for any Set type using Reflection
        private void CheckDefaultItem(SerializedProperty setProp, string setTypeName)
        {
             if (setProp != null && setProp.objectReferenceValue != null)
            {
                UnityEngine.Object setAsset = setProp.objectReferenceValue;
                Type setType = setAsset.GetType();
                PropertyInfo itemsProperty = setType.GetProperty("Items", BindingFlags.Public | BindingFlags.Instance); // Get public instance property named "Items"

                if (itemsProperty == null || !itemsProperty.CanRead)
                {
                    Debug.LogWarning($"Could not find readable public 'Items' property on {setTypeName} '{setAsset.name}' for ArtSet '{((ArtSet)target).name}'.", target);
                    return;
                }

                object itemsValue = itemsProperty.GetValue(setAsset);

                if (itemsValue is IList itemsList) // Check if the value is an IList
                {
                     if (itemsList == null || itemsList.Count == 0 || itemsList[0] == null)
                     {
                          Debug.LogError($"ArtSet '{((ArtSet)target).name}': The assigned {setTypeName} '{setAsset.name}' is missing its Default/Fallback item (at index 0). This must be assigned.", target);
                     }
                }
                else
                {
                     Debug.LogWarning($"The 'Items' property on {setTypeName} '{setAsset.name}' does not return an IList for ArtSet '{((ArtSet)target).name}'.", target);
                }
                // Removed the try-catch for RuntimeBinderException
            }
        }


        private void OnDisable()
        {
            // Clean up the editor instance when the editor is disabled or destroyed
            if (_artSetTypeEditorInstance != null)
            {
                DestroyImmediate(_artSetTypeEditorInstance);
                _artSetTypeEditorInstance = null;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_idProp);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(_nameProp);

            EditorGUILayout.Space();
            #region Art Set Type editor
            // --- ArtSetType Section ---

            EditorGUILayout.LabelField("Art Set Type Definition", EditorStyles.boldLabel);



                        // Pass the ref int state variable
            ArtSetEditorUtils.DrawScriptableObjectField(
                target, _setTypeProp, "Set Type", typeof(ArtSetType),
                ref _creatingInlineSetType, ref _newSetTypeName, // Pass bool ref
                "Create Inline ArtSetType", "New ArtSetType", // Default name adjusted
                createAsAsset: true); // No currentIndex needed

            _showSetTypeSection = EditorGUILayout.Foldout(_showSetTypeSection, " Edit Art Set Type Definition", true, EditorStyles.foldoutHeader);


            if (_showSetTypeSection)
            {
                EditorGUI.indentLevel++; // Indent content within foldout

  

                // Draw the inline editor for the assigned ArtSetType
                UnityEngine.Object currentSetType = _setTypeProp.objectReferenceValue;
                if (currentSetType != null)
                {
                    // Check if editor needs recreation
                    if (_artSetTypeEditorInstance == null || _artSetTypeEditorInstance.target != currentSetType)
                    {
                        DestroyImmediate(_artSetTypeEditorInstance); // Clean up previous one if target changed
                        _artSetTypeEditorInstance = Editor.CreateEditor(currentSetType);
                    }

                    if (_artSetTypeEditorInstance != null)
                    {
                        EditorGUILayout.LabelField("Edit Assigned Set Type:", EditorStyles.boldLabel);
                        // Draw a box around the inline editor for visual separation
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUI.indentLevel++; // Indent content within the inline editor box
                        _artSetTypeEditorInstance.OnInspectorGUI();
                        EditorGUI.indentLevel--;
                        EditorGUILayout.EndVertical();
                    }
                }
                else
                {
                     // If the type is cleared, destroy the editor instance
                     if (_artSetTypeEditorInstance != null)
                     {
                         DestroyImmediate(_artSetTypeEditorInstance);
                         _artSetTypeEditorInstance = null;
                     }
                }
                EditorGUI.indentLevel--; // Restore indent level
            }

            #endregion

            #region Set Reference editor
            
            EditorGUILayout.Space();

            // --- Sets Section ---
            _showSetsSection = EditorGUILayout.Foldout(_showSetsSection, "Contained Sets", true, EditorStyles.foldoutHeader);
            if (_showSetsSection)
            {
                 EditorGUI.indentLevel++; // Indent content within foldout

                // Use the reverted DrawScriptableObjectField with bool state
                ArtSetEditorUtils.DrawScriptableObjectField(
                    target, _picSetProp, "Picture Set", typeof(PicSet),
                    ref _creatingInlinePicSet, ref _newPicSetName, // Pass bool ref
                    "Create Inline Picture Set", "New Inline PicSet",
                    createAsAsset: false); // No currentIndex needed

                ArtSetEditorUtils.DrawScriptableObjectField(
                    target, _colourSetProp, "Colour Set", typeof(ColourSet),
                    ref _creatingInlineColourSet, ref _newColourSetName, // Pass bool ref
                    "Create Inline Colour Set", "New Inline ColourSet",
                    createAsAsset: false); // No currentIndex needed

                ArtSetEditorUtils.DrawScriptableObjectField(
                    target, _fontSetProp, "Font Set", typeof(FontSet),
                    ref _creatingInlineFontSet, ref _newFontSetName, // Pass bool ref
                    "Create Inline Font Set", "New Inline FontSet",
                    createAsAsset: false); // No currentIndex needed

                ArtSetEditorUtils.DrawScriptableObjectField(
                    target, _animationSetProp, "Animation Set", typeof(AnimationSet),
                    ref _creatingInlineAnimationSet, ref _newAnimationSetName, // Pass bool ref
                    "Create Inline Animation Set", "New Inline AnimationSet",
                    createAsAsset: false); // No currentIndex needed

                 EditorGUI.indentLevel--; // Restore indent level
            }

            #endregion

            #region PicSet Item Editor
            EditorGUILayout.Space();
            _showPicSetSection = EditorGUILayout.Foldout(_showPicSetSection, "Picture Set Items", true, EditorStyles.foldoutHeader);
            if (_showPicSetSection)
            {
                EditorGUI.indentLevel++;
                UnityEngine.Object setRef = _picSetProp.objectReferenceValue;

                if (setRef == null) { EditorGUILayout.HelpBox($"Assign or create a PicSet in 'Contained Sets' to edit items.", MessageType.Info); }
                else if (setRef is PicSet set)
                {
                    SerializedObject setSO = new SerializedObject(set);
                    SerializedProperty itemsProp = setSO.FindProperty("_items");
                    ArtSetType artSetType = _setTypeProp.objectReferenceValue as ArtSetType;
                    List<PicItemType> itemTypes = artSetType?.PicItemTypes; // Get specific types
                    int typeSlotCount = itemTypes?.Count ?? 0;

                    setSO.Update();

                    // Default Slot
                    EditorGUILayout.LabelField("Default/Fallback Item", EditorStyles.boldLabel);
                    if (itemsProp.arraySize < 1) itemsProp.arraySize = 1;
                    SerializedProperty defaultItemProp = itemsProp.GetArrayElementAtIndex(0);
                    ArtSetItemEditorUtils.DrawPicItemListItemField(set, defaultItemProp, "Default Fallback", typeof(PicItem), ref _creatingInlinePicItemIndex, ref _newPicItemName, "Create Inline Default Item", "New Default Item", false, 0, ref _newPicItemSprite, ref _newPicItemColour);

                    EditorGUILayout.Space();

                    // Type Slots
                    if (artSetType == null) { EditorGUILayout.HelpBox("Assign an ArtSetType to define item slots.", MessageType.Warning); }
                    else
                    {
                        EditorGUILayout.LabelField("Item Slots (Defined by ArtSetType)", EditorStyles.boldLabel);
                        for (int i = 0; i < typeSlotCount; i++)
                        {
                            int slotIndex = i + 1;
                            if (itemsProp.arraySize <= slotIndex) itemsProp.arraySize = slotIndex + 1;
                            SerializedProperty itemProp = itemsProp.GetArrayElementAtIndex(slotIndex);
                            string slotLabel = itemTypes[i]?.Name ?? $"Slot {slotIndex} (Invalid Type?)";
                            ArtSetItemEditorUtils.DrawPicItemListItemField(set, itemProp, slotLabel, typeof(PicItem), ref _creatingInlinePicItemIndex, ref _newPicItemName, $"Create Inline '{slotLabel}' Item", $"New {slotLabel} Item", false, slotIndex, ref _newPicItemSprite, ref _newPicItemColour);
                        }
                    }

                    EditorGUILayout.Space();

                    // Extra Items
                    int firstExtraIndex = typeSlotCount + 1;
                    if (itemsProp.arraySize > firstExtraIndex)
                    {
                        EditorGUILayout.LabelField("Extra Items (Not defined in ArtSetType)", EditorStyles.boldLabel);
                        for (int i = firstExtraIndex; i < itemsProp.arraySize; i++)
                        {
                            SerializedProperty itemProp = itemsProp.GetArrayElementAtIndex(i);
                            string itemLabel = $"Extra Item [{i}]"; if (itemProp.objectReferenceValue != null) { itemLabel += $" ({itemProp.objectReferenceValue.name})"; }
                            ArtSetItemEditorUtils.DrawPicItemListItemField(set, itemProp, itemLabel, typeof(PicItem), ref _creatingInlinePicItemIndex, ref _newPicItemName, "Create Inline Extra Item", "New Extra Item", false, i, ref _newPicItemSprite, ref _newPicItemColour);
                        }
                    }
                    setSO.ApplyModifiedProperties();
                }
                EditorGUI.indentLevel--;
            }
            #endregion

            #region ColourSet Item Editor
            EditorGUILayout.Space();
            _showColourSetSection = EditorGUILayout.Foldout(_showColourSetSection, "Colour Set Items", true, EditorStyles.foldoutHeader);
            if (_showColourSetSection)
            {
                EditorGUI.indentLevel++;
                UnityEngine.Object setRef = _colourSetProp.objectReferenceValue; // Use correct prop

                if (setRef == null) { EditorGUILayout.HelpBox($"Assign or create a ColourSet in 'Contained Sets' to edit items.", MessageType.Info); }
                else if (setRef is ColourSet set) // Use correct type
                {
                    SerializedObject setSO = new SerializedObject(set);
                    SerializedProperty itemsProp = setSO.FindProperty("_items");
                    ArtSetType artSetType = _setTypeProp.objectReferenceValue as ArtSetType;
                    List<ColourItemType> itemTypes = artSetType?.ColourItemTypes; // Get specific types
                    int typeSlotCount = itemTypes?.Count ?? 0;

                    setSO.Update();

                    // Default Slot
                    EditorGUILayout.LabelField("Default/Fallback Item", EditorStyles.boldLabel);
                    if (itemsProp.arraySize < 1) itemsProp.arraySize = 1;
                    SerializedProperty defaultItemProp = itemsProp.GetArrayElementAtIndex(0);
                    ArtSetItemEditorUtils.DrawColourItemListItemField(set, defaultItemProp, "Default Fallback", typeof(ColourItem), ref _creatingInlineColourItemIndex, ref _newColourItemName, "Create Inline Default Item", "New Default Item", false, 0, ref _newColourItemColour); // Call correct function

                    EditorGUILayout.Space();

                    // Type Slots
                    if (artSetType == null) { EditorGUILayout.HelpBox("Assign an ArtSetType to define item slots.", MessageType.Warning); }
                    else
                    {
                        EditorGUILayout.LabelField("Item Slots (Defined by ArtSetType)", EditorStyles.boldLabel);
                        for (int i = 0; i < typeSlotCount; i++)
                        {
                            int slotIndex = i + 1;
                            if (itemsProp.arraySize <= slotIndex) itemsProp.arraySize = slotIndex + 1;
                            SerializedProperty itemProp = itemsProp.GetArrayElementAtIndex(slotIndex);
                            string slotLabel = itemTypes[i]?.Name ?? $"Slot {slotIndex} (Invalid Type?)";
                            ArtSetItemEditorUtils.DrawColourItemListItemField(set, itemProp, slotLabel, typeof(ColourItem), ref _creatingInlineColourItemIndex, ref _newColourItemName, $"Create Inline '{slotLabel}' Item", $"New {slotLabel} Item", false, slotIndex, ref _newColourItemColour); // Call correct function
                        }
                    }

                    EditorGUILayout.Space();

                    // Extra Items
                    int firstExtraIndex = typeSlotCount + 1;
                    if (itemsProp.arraySize > firstExtraIndex)
                    {
                        EditorGUILayout.LabelField("Extra Items (Not defined in ArtSetType)", EditorStyles.boldLabel);
                        for (int i = firstExtraIndex; i < itemsProp.arraySize; i++)
                        {
                            SerializedProperty itemProp = itemsProp.GetArrayElementAtIndex(i);
                            string itemLabel = $"Extra Item [{i}]"; if (itemProp.objectReferenceValue != null) { itemLabel += $" ({itemProp.objectReferenceValue.name})"; }
                            ArtSetItemEditorUtils.DrawColourItemListItemField(set, itemProp, itemLabel, typeof(ColourItem), ref _creatingInlineColourItemIndex, ref _newColourItemName, "Create Inline Extra Item", "New Extra Item", false, i, ref _newColourItemColour); // Call correct function
                        }
                    }
                    setSO.ApplyModifiedProperties();
                }
                EditorGUI.indentLevel--;
            }
            #endregion

            #region FontSet Item Editor
            EditorGUILayout.Space();
            _showFontSetSection = EditorGUILayout.Foldout(_showFontSetSection, "Font Set Items", true, EditorStyles.foldoutHeader);
            if (_showFontSetSection)
            {
                EditorGUI.indentLevel++;
                UnityEngine.Object setRef = _fontSetProp.objectReferenceValue;
                if (setRef == null) { EditorGUILayout.HelpBox($"Assign or create a FontSet in 'Contained Sets' to edit items.", MessageType.Info); }
                else if (setRef is FontSet set)
                {
                    SerializedObject setSO = new SerializedObject(set);
                    SerializedProperty itemsProp = setSO.FindProperty("_items");
                    ArtSetType artSetType = _setTypeProp.objectReferenceValue as ArtSetType;
                    List<FontItemType> itemTypes = artSetType?.FontItemTypes;
                    int typeSlotCount = itemTypes?.Count ?? 0;
                    setSO.Update();

                    // Default Slot
                    EditorGUILayout.LabelField("Default/Fallback Item", EditorStyles.boldLabel);
                    if (itemsProp.arraySize < 1) itemsProp.arraySize = 1;
                    SerializedProperty defaultItemProp = itemsProp.GetArrayElementAtIndex(0);
                    // Pass ref int index instead of bool
                    ArtSetItemEditorUtils.DrawFontItemListItemField(set, defaultItemProp, "Default Fallback", typeof(FontItem), ref _creatingInlineFontItemRefIndex, ref _newFontItemRefName, false, 0);

                    EditorGUILayout.Space();

                    // Type Slots
                    if (artSetType == null) { EditorGUILayout.HelpBox("Assign an ArtSetType to define item slots.", MessageType.Warning); }
                    else
                    {
                        EditorGUILayout.LabelField("Item Slots (Defined by ArtSetType)", EditorStyles.boldLabel);
                        for (int i = 0; i < typeSlotCount; i++)
                        {
                            int slotIndex = i + 1;
                            if (itemsProp.arraySize <= slotIndex) itemsProp.arraySize = slotIndex + 1;
                            SerializedProperty itemProp = itemsProp.GetArrayElementAtIndex(slotIndex);
                            string slotLabel = itemTypes[i]?.Name ?? $"Slot {slotIndex} (Invalid Type?)";
                            // Pass ref int index instead of bool
                            ArtSetItemEditorUtils.DrawFontItemListItemField(set, itemProp, slotLabel, typeof(FontItem), ref _creatingInlineFontItemRefIndex, ref _newFontItemRefName, false, slotIndex);
                        }
                    }

                    EditorGUILayout.Space();

                    // Extra Items
                    int firstExtraIndex = typeSlotCount + 1;
                    if (itemsProp.arraySize > firstExtraIndex)
                    {
                        EditorGUILayout.LabelField("Extra Items (Not defined in ArtSetType)", EditorStyles.boldLabel);
                        for (int i = firstExtraIndex; i < itemsProp.arraySize; i++)
                        {
                            SerializedProperty itemProp = itemsProp.GetArrayElementAtIndex(i);
                            string itemLabel = $"Extra Item [{i}]"; if (itemProp.objectReferenceValue != null) { itemLabel += $" ({itemProp.objectReferenceValue.name})"; }
                            // Pass ref int index instead of bool
                            ArtSetItemEditorUtils.DrawFontItemListItemField(set, itemProp, itemLabel, typeof(FontItem), ref _creatingInlineFontItemRefIndex, ref _newFontItemRefName, false, i);
                        }
                    }
                    setSO.ApplyModifiedProperties();
                }
                EditorGUI.indentLevel--;
            }
            #endregion

            #region AnimationSet Item Editor
             EditorGUILayout.Space();
            _showAnimationSetSection = EditorGUILayout.Foldout(_showAnimationSetSection, "Animation Set Items", true, EditorStyles.foldoutHeader);
            if (_showAnimationSetSection)
            {
                 EditorGUI.indentLevel++;
                UnityEngine.Object setRef = _animationSetProp.objectReferenceValue;
                if (setRef == null) { EditorGUILayout.HelpBox($"Assign or create an AnimationSet in 'Contained Sets' to edit items.", MessageType.Info); }
                else if (setRef is AnimationSet set)
                {
                    SerializedObject setSO = new SerializedObject(set);
                    SerializedProperty itemsProp = setSO.FindProperty("_items");
                    ArtSetType artSetType = _setTypeProp.objectReferenceValue as ArtSetType;
                    List<AnimationItemType> itemTypes = artSetType?.AnimationItemTypes;
                    int typeSlotCount = itemTypes?.Count ?? 0;
                    setSO.Update();

                    // Default Slot
                    EditorGUILayout.LabelField("Default/Fallback Item", EditorStyles.boldLabel);
                    if (itemsProp.arraySize < 1) itemsProp.arraySize = 1;
                    SerializedProperty defaultItemProp = itemsProp.GetArrayElementAtIndex(0);
                     // Pass ref int index instead of bool
                    ArtSetItemEditorUtils.DrawAnimationItemListItemField(set, defaultItemProp, "Default Fallback", typeof(AnimationItem), ref _creatingInlineAnimationItemRefIndex, ref _newAnimationItemRefName, false, 0);

                    EditorGUILayout.Space();

                    // Type Slots
                    if (artSetType == null) { EditorGUILayout.HelpBox("Assign an ArtSetType to define item slots.", MessageType.Warning); }
                    else
                    {
                        EditorGUILayout.LabelField("Item Slots (Defined by ArtSetType)", EditorStyles.boldLabel);
                        for (int i = 0; i < typeSlotCount; i++)
                        {
                            int slotIndex = i + 1;
                            if (itemsProp.arraySize <= slotIndex) itemsProp.arraySize = slotIndex + 1;
                            SerializedProperty itemProp = itemsProp.GetArrayElementAtIndex(slotIndex);
                            string slotLabel = itemTypes[i]?.Name ?? $"Slot {slotIndex} (Invalid Type?)";
                             // Pass ref int index instead of bool
                            ArtSetItemEditorUtils.DrawAnimationItemListItemField(set, itemProp, slotLabel, typeof(AnimationItem), ref _creatingInlineAnimationItemRefIndex, ref _newAnimationItemRefName, false, slotIndex);
                        }
                    }

                    EditorGUILayout.Space();

                    // Extra Items
                    int firstExtraIndex = typeSlotCount + 1;
                    if (itemsProp.arraySize > firstExtraIndex)
                    {
                        EditorGUILayout.LabelField("Extra Items (Not defined in ArtSetType)", EditorStyles.boldLabel);
                        for (int i = firstExtraIndex; i < itemsProp.arraySize; i++)
                        {
                            SerializedProperty itemProp = itemsProp.GetArrayElementAtIndex(i);
                            string itemLabel = $"Extra Item [{i}]"; if (itemProp.objectReferenceValue != null) { itemLabel += $" ({itemProp.objectReferenceValue.name})"; }
                             // Pass ref int index instead of bool
                            ArtSetItemEditorUtils.DrawAnimationItemListItemField(set, itemProp, itemLabel, typeof(AnimationItem), ref _creatingInlineAnimationItemRefIndex, ref _newAnimationItemRefName, false, i);
                        }
                    }
                    setSO.ApplyModifiedProperties();
                }
                 EditorGUI.indentLevel--;
            }
            #endregion


            serializedObject.ApplyModifiedProperties();
        }
    }
}
