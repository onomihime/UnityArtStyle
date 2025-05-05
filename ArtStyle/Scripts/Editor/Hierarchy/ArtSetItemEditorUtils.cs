// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using UnityEditor;
using System; // Required for Type
using System.IO; // Required for Path
using Modules.ArtStyle; // Required for Item/Set types

namespace Modules.ArtStyle.Editors
{
    /// <summary>
    /// Utility methods specifically for drawing ArtSet list items (PicItem, ColourItem, etc.) in editors.
    /// </summary>
    public static class ArtSetItemEditorUtils
    {
        private static readonly Color InlineInstanceLabelColor = new Color(1.0f, 0.9f, 0.4f); // Yellowish
        private static readonly Color EmptySlotColor = new Color(0.6f, 0.6f, 0.6f, 0.5f); // Greyish placeholder

        // --- PicItem Field --- (Moved from ArtSetEditorUtils and renamed)
        public static void DrawPicItemListItemField(
            UnityEngine.Object target, SerializedProperty soProp, string displayLabel, Type objectType,
            ref int creatingInlineIndex, ref string newInstanceName, string inlineCreationTitle, string defaultInstanceName,
            bool createAsAsset, int currentIndex, ref Sprite newSpriteRef, ref Color newColourRef)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;
            bool showInlineCreator = !createAsAsset && creatingInlineIndex == currentIndex;

            if (showInlineCreator) { /* ... PicItem Inline Creator UI (Name, Sprite, Color) ... */
                EditorGUILayout.LabelField(inlineCreationTitle, EditorStyles.boldLabel);
                newInstanceName = EditorGUILayout.TextField("Instance Name", newInstanceName);
                newSpriteRef = (Sprite)EditorGUILayout.ObjectField("Sprite", newSpriteRef, typeof(Sprite), false);
                newColourRef = EditorGUILayout.ColorField("Default Colour", newColourRef);
                EditorGUILayout.BeginHorizontal(); try {
                    if (GUILayout.Button("Confirm")) {
                        var newInstance = ScriptableObject.CreateInstance(objectType); newInstance.name = newInstanceName;
                        if (newInstance is PicItem newPicItem) {
                            SerializedObject newInstanceSO = new SerializedObject(newPicItem);
                            newInstanceSO.FindProperty("_sprite").objectReferenceValue = newSpriteRef;
                            newInstanceSO.FindProperty("_defaultColour").colorValue = newColourRef;
                            newInstanceSO.ApplyModifiedPropertiesWithoutUndo();
                        }
                        if (EditorUtility.IsPersistent(target)) AssetDatabase.AddObjectToAsset(newInstance, target);
                        soProp.objectReferenceValue = newInstance;
                        if (EditorUtility.IsPersistent(target)) { EditorUtility.SetDirty(target); AssetDatabase.SaveAssets(); }
                        Undo.RecordObject(target, $"Create Inline {displayLabel}"); soProp.objectReferenceValue = newInstance;
                        creatingInlineIndex = -1; GUI.FocusControl(null); newSpriteRef = null; newColourRef = Color.white;
                    } if (GUILayout.Button("Cancel")) { creatingInlineIndex = -1; GUI.FocusControl(null); newSpriteRef = null; newColourRef = Color.white; }
                } finally { EditorGUILayout.EndHorizontal(); }
            } else { /* ... PicItem Normal Display (Preview, Field, Buttons, Inline/Extract Logic) ... */
                EditorGUILayout.BeginHorizontal(); try {
                    EditorGUILayout.BeginVertical(GUILayout.Width(40f)); { /* ... PicItem Preview (Sprite + Color Bar) ... */
                        Rect previewRect = GUILayoutUtility.GetRect(40f, 40f, GUILayout.ExpandWidth(false)); Rect colorRect = GUILayoutUtility.GetRect(40f, 8f, GUILayout.ExpandWidth(false));
                        UnityEngine.Object currentRef = soProp.objectReferenceValue;
                        if (currentRef is PicItem picItem && picItem.Sprite != null && picItem.Sprite.texture != null) {
                            Texture tex = picItem.Sprite.texture; Rect texCoords = picItem.Sprite.textureRect; texCoords.x /= tex.width; texCoords.y /= tex.height; texCoords.width /= tex.width; texCoords.height /= tex.height;
                            float spriteW = picItem.Sprite.rect.width; float spriteH = picItem.Sprite.rect.height; float aspect = spriteW / spriteH; Rect spriteDrawRect = previewRect;
                            if (aspect >= 1) { spriteDrawRect.height = previewRect.width / aspect; spriteDrawRect.y += (previewRect.height - spriteDrawRect.height) * 0.5f; } else { spriteDrawRect.width = previewRect.height * aspect; spriteDrawRect.x += (previewRect.width - spriteDrawRect.width) * 0.5f; }
                            var prevTint = GUI.color; GUI.color = Color.white; GUI.DrawTextureWithTexCoords(spriteDrawRect, tex, texCoords, true); GUI.color = prevTint;
                        } else if (currentRef != null) { Texture2D previewTexture = AssetPreview.GetMiniThumbnail(currentRef); if (previewTexture != null) { GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit); } else { var c = GUI.color; GUI.color = EmptySlotColor; GUI.Box(previewRect, GUIContent.none, EditorStyles.helpBox); GUI.color = c; } } else { var c = GUI.color; GUI.color = EmptySlotColor; GUI.Box(previewRect, GUIContent.none, EditorStyles.helpBox); GUI.color = c; }
                        if (currentRef is PicItem picItemColor) { EditorGUI.DrawRect(colorRect, picItemColor.DefaultColour); } else { var c = GUI.color; GUI.color = EmptySlotColor; GUI.Box(colorRect, GUIContent.none, EditorStyles.helpBox); GUI.color = c; }
                        GUILayoutUtility.GetRect(40f, 2f, GUILayout.ExpandWidth(false));
                    } EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(); try {
                        EditorGUILayout.LabelField(displayLabel, EditorStyles.boldLabel);
                        UnityEngine.Object currentRef = soProp.objectReferenceValue;
                        if (currentRef == null && !showInlineCreator) { if (currentIndex == 0) { EditorGUILayout.HelpBox("Default slot must be filled!", MessageType.Error); } else { EditorGUILayout.HelpBox("Empty, will use Default.", MessageType.None); } }
                        EditorGUILayout.BeginHorizontal(); try {
                            EditorGUI.BeginChangeCheck(); EditorGUILayout.PropertyField(soProp, GUIContent.none, true); bool changed = EditorGUI.EndChangeCheck(); if (changed && !createAsAsset) { creatingInlineIndex = -1; }
                            EditorGUILayout.BeginVertical(GUILayout.Width(120)); try {
                                bool isAssigned = currentRef != null; bool clearClicked = false;
                                if (isAssigned) {
                                    bool isInline = AssetDatabase.IsSubAsset(currentRef);
                                    if (!createAsAsset && isInline) { if (GUILayout.Button("Extract Asset")) { /* ... Extract Logic ... */ } }
                                    if (GUILayout.Button("Clear")) { clearClicked = true; /* ... Clear Logic ... */ }
                                    if (!clearClicked) { if (!createAsAsset && isInline) { /* ... Inline Label ... */ } else if (!EditorUtility.IsPersistent(currentRef) && !isInline) { /* ... Not Asset Ref Label ... */ } }
                                } else { if (createAsAsset) { /* ... Create Asset Button ... */ } else { if (GUILayout.Button("Create Inline")) { creatingInlineIndex = currentIndex; newInstanceName = defaultInstanceName; newSpriteRef = null; newColourRef = Color.white; } } }
                            } finally { EditorGUILayout.EndVertical(); }
                        } finally { EditorGUILayout.EndHorizontal(); }
                    } finally { EditorGUILayout.EndVertical(); }
                } finally { EditorGUILayout.EndHorizontal(); }
            }
            EditorGUI.indentLevel--; EditorGUILayout.EndVertical(); EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
        }

        // --- ColourItem Field ---
        public static void DrawColourItemListItemField(
             UnityEngine.Object target, SerializedProperty soProp, string displayLabel, Type objectType,
             ref int creatingInlineIndex, ref string newInstanceName, string inlineCreationTitle, string defaultInstanceName,
             bool createAsAsset, int currentIndex, ref Color newColourRef) // No Sprite ref
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;
            bool showInlineCreator = !createAsAsset && creatingInlineIndex == currentIndex;

            if (showInlineCreator) { /* ... ColourItem Inline Creator UI (Name, Color) ... */
                EditorGUILayout.LabelField(inlineCreationTitle, EditorStyles.boldLabel);
                newInstanceName = EditorGUILayout.TextField("Instance Name", newInstanceName);
                newColourRef = EditorGUILayout.ColorField("Colour", newColourRef); // Only Color field
                EditorGUILayout.BeginHorizontal(); try {
                    if (GUILayout.Button("Confirm")) {
                        var newInstance = ScriptableObject.CreateInstance(objectType); newInstance.name = newInstanceName;
                        if (newInstance is ColourItem newColourItem) { // Check type
                            SerializedObject newInstanceSO = new SerializedObject(newColourItem);
                            newInstanceSO.FindProperty("_colour").colorValue = newColourRef; // Set only color
                            newInstanceSO.ApplyModifiedPropertiesWithoutUndo();
                        }
                        if (EditorUtility.IsPersistent(target)) AssetDatabase.AddObjectToAsset(newInstance, target);
                        soProp.objectReferenceValue = newInstance;
                        if (EditorUtility.IsPersistent(target)) { EditorUtility.SetDirty(target); AssetDatabase.SaveAssets(); }
                        Undo.RecordObject(target, $"Create Inline {displayLabel}"); soProp.objectReferenceValue = newInstance;
                        creatingInlineIndex = -1; GUI.FocusControl(null); newColourRef = Color.white; // Reset only color
                    } if (GUILayout.Button("Cancel")) { creatingInlineIndex = -1; GUI.FocusControl(null); newColourRef = Color.white; } // Reset only color
                } finally { EditorGUILayout.EndHorizontal(); }
            } else { /* ... ColourItem Normal Display (Preview, Field, Buttons, Inline/Extract Logic) ... */
                EditorGUILayout.BeginHorizontal(); try {
                    EditorGUILayout.BeginVertical(GUILayout.Width(40f)); { /* ... ColourItem Preview (Color Rect only) ... */
                        Rect previewRect = GUILayoutUtility.GetRect(40f, 40f + 8f + 2f, GUILayout.ExpandWidth(false)); // Combined height for color + spacing
                        UnityEngine.Object currentRef = soProp.objectReferenceValue;
                        Color displayColor = EmptySlotColor;
                        if (currentRef is ColourItem colourItem) { displayColor = colourItem.Colour; }
                        EditorGUI.DrawRect(previewRect, displayColor); // Draw color in the whole area
                    } EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(); try {
                        EditorGUILayout.LabelField(displayLabel, EditorStyles.boldLabel);
                        UnityEngine.Object currentRef = soProp.objectReferenceValue;
                        if (currentRef == null && !showInlineCreator) { if (currentIndex == 0) { EditorGUILayout.HelpBox("Default slot must be filled!", MessageType.Error); } else { EditorGUILayout.HelpBox("Empty, will use Default.", MessageType.None); } }
                        EditorGUILayout.BeginHorizontal(); try {
                            EditorGUI.BeginChangeCheck(); EditorGUILayout.PropertyField(soProp, GUIContent.none, true); bool changed = EditorGUI.EndChangeCheck(); if (changed && !createAsAsset) { creatingInlineIndex = -1; }
                            EditorGUILayout.BeginVertical(GUILayout.Width(120)); try {
                                bool isAssigned = currentRef != null; bool clearClicked = false;
                                if (isAssigned) {
                                    bool isInline = AssetDatabase.IsSubAsset(currentRef);
                                    if (!createAsAsset && isInline) { if (GUILayout.Button("Extract Asset")) { /* ... Extract Logic ... */ } }
                                    if (GUILayout.Button("Clear")) { clearClicked = true; /* ... Clear Logic ... */ }
                                    if (!clearClicked) { if (!createAsAsset && isInline) { /* ... Inline Label ... */ } else if (!EditorUtility.IsPersistent(currentRef) && !isInline) { /* ... Not Asset Ref Label ... */ } }
                                } else { if (createAsAsset) { /* ... Create Asset Button ... */ } else { if (GUILayout.Button("Create Inline")) { creatingInlineIndex = currentIndex; newInstanceName = defaultInstanceName; newColourRef = Color.white; } } } // Reset only color
                            } finally { EditorGUILayout.EndVertical(); }
                        } finally { EditorGUILayout.EndHorizontal(); }
                    } finally { EditorGUILayout.EndVertical(); }
                } finally { EditorGUILayout.EndHorizontal(); }
            }
            EditorGUI.indentLevel--; EditorGUILayout.EndVertical(); EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
        }

        // --- FontItem Field ---
        public static void DrawFontItemListItemField(
            UnityEngine.Object target, SerializedProperty soProp, string displayLabel, Type objectType,
            ref int creatingInlineIndex, ref string newInstanceNameRef, // Changed to ref int
            bool createAsAsset, int currentIndex)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;

            UnityEngine.Object currentRef = soProp.objectReferenceValue;
            // Show error/warning message first, check against index
            if (currentRef == null && creatingInlineIndex != currentIndex) // Check index
            {
                if (currentIndex == 0) { EditorGUILayout.HelpBox("Default slot must be filled!", MessageType.Error); }
                else { EditorGUILayout.HelpBox("Empty, will use Default.", MessageType.None); }
            }

            // --- Adapt call to DrawScriptableObjectField ---
            bool isCurrentlyCreating = (creatingInlineIndex == currentIndex);
            bool wasCreating = isCurrentlyCreating; // Store state before call

            ArtSetEditorUtils.DrawScriptableObjectField(
                target, soProp, displayLabel, objectType,
                ref isCurrentlyCreating, ref newInstanceNameRef, // Pass local bool by ref
                $"Create Inline '{displayLabel}'", $"New {displayLabel}",
                createAsAsset);

            // If the state changed from true to false, reset the main index tracker
            if (wasCreating && !isCurrentlyCreating)
            {
                creatingInlineIndex = -1;
            }
            // If we just initiated creation, set the index
            else if (!wasCreating && isCurrentlyCreating)
            {
                 creatingInlineIndex = currentIndex;
                 // Optionally reset name here if needed: newInstanceNameRef = $"New {displayLabel}";
            }
            // --- End Adaptation ---


            // If an item is assigned, draw its properties directly
            currentRef = soProp.objectReferenceValue;
            if (currentRef != null && currentRef is FontItem fontItem)
            {
                EditorGUI.indentLevel++; // Indent the item's properties
                SerializedObject itemSO = new SerializedObject(fontItem);
                itemSO.Update();
                EditorGUILayout.PropertyField(itemSO.FindProperty("_font"));
                EditorGUILayout.PropertyField(itemSO.FindProperty("_tmpFont"));
                EditorGUILayout.PropertyField(itemSO.FindProperty("_defaultColour"));
                itemSO.ApplyModifiedProperties();
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
        }

        // --- AnimationItem Field ---
         public static void DrawAnimationItemListItemField(
            UnityEngine.Object target, SerializedProperty soProp, string displayLabel, Type objectType,
            ref int creatingInlineIndex, ref string newInstanceNameRef, // Changed to ref int
            bool createAsAsset, int currentIndex)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;

            UnityEngine.Object currentRef = soProp.objectReferenceValue;
            // Show error/warning message first, check against index
            if (currentRef == null && creatingInlineIndex != currentIndex) // Check index
            {
                if (currentIndex == 0) { EditorGUILayout.HelpBox("Default slot must be filled!", MessageType.Error); }
                else { EditorGUILayout.HelpBox("Empty, will use Default.", MessageType.None); }
            }

            // --- Adapt call to DrawScriptableObjectField ---
            bool isCurrentlyCreating = (creatingInlineIndex == currentIndex);
            bool wasCreating = isCurrentlyCreating; // Store state before call

            ArtSetEditorUtils.DrawScriptableObjectField(
                target, soProp, displayLabel, objectType,
                ref isCurrentlyCreating, ref newInstanceNameRef, // Pass local bool by ref
                $"Create Inline '{displayLabel}'", $"New {displayLabel}",
                createAsAsset);

            // If the state changed from true to false, reset the main index tracker
            if (wasCreating && !isCurrentlyCreating)
            {
                creatingInlineIndex = -1;
            }
             // If we just initiated creation, set the index
            else if (!wasCreating && isCurrentlyCreating)
            {
                 creatingInlineIndex = currentIndex;
                 // Optionally reset name here if needed: newInstanceNameRef = $"New {displayLabel}";
            }
            // --- End Adaptation ---


            // If an item is assigned, draw its properties directly
            currentRef = soProp.objectReferenceValue;
            if (currentRef != null && currentRef is AnimationItem animItem)
            {
                EditorGUI.indentLevel++; // Indent the item's properties
                SerializedObject itemSO = new SerializedObject(animItem);
                itemSO.Update();
                EditorGUILayout.PropertyField(itemSO.FindProperty("_duration"));
                EditorGUILayout.PropertyField(itemSO.FindProperty("_useFade"));
                if (itemSO.FindProperty("_useFade").boolValue)
                {
                    EditorGUILayout.PropertyField(itemSO.FindProperty("_fadeStartOpacity"));
                    EditorGUILayout.PropertyField(itemSO.FindProperty("_curve"));
                }
                itemSO.ApplyModifiedProperties();
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
        }
    }
}
