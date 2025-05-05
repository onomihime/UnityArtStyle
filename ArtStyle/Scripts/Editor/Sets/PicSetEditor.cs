// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using Modules.ArtStyle;

namespace Modules.ArtStyle.Editors
{
    [CustomEditor(typeof(PicSet))]
    public class PicSetEditor : Editor
    {
        private SerializedProperty _idProp;
        // No _nameProp needed for this specific editor's GUI
        private SerializedProperty _itemsProp;
        private ReorderableList _itemList;
        private Dictionary<string, Editor> _inlineItemEditors = new Dictionary<string, Editor>(); // Cache for inline editors

        private void OnEnable()
        {
            _idProp = serializedObject.FindProperty("_id");
            _itemsProp = serializedObject.FindProperty("_items");

            // Ensure _itemsProp is valid before setting up the list
            if (_itemsProp != null)
            {
                SetupItemList();
            }
            else
            {
                Debug.LogError("Could not find the '_items' property on the PicSet asset. ReorderableList cannot be initialized.");
                _itemList = null; // Ensure itemList is null if setup fails
            }
        }

        private void OnDisable()
        {
            // Clean up cached editors when the editor is disabled or destroyed
            foreach (var editor in _inlineItemEditors.Values)
            {
                if (editor != null) DestroyImmediate(editor);
            }
            _inlineItemEditors.Clear();
        }

        private void SetupItemList()
        {
            // Check if _itemsProp is valid before creating the list
            if (_itemsProp == null) return;

            _itemList = new ReorderableList(serializedObject, _itemsProp, true, true, true, true);

            _itemList.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Picture Items (Assets or Inline)");

            _itemList.elementHeightCallback = (int index) =>
            {
                // Add null check for _itemsProp
                if (_itemsProp == null || index >= _itemsProp.arraySize) return EditorGUIUtility.singleLineHeight;

                var element = _itemsProp.GetArrayElementAtIndex(index);
                if (element.objectReferenceValue != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(element.objectReferenceValue)))
                {
                    // Inline item: Calculate height needed for its editor + button + spacing
                    float height = EditorGUIUtility.standardVerticalSpacing * 3; // Top/Bottom spacing + button spacing
                    height += EditorGUIUtility.singleLineHeight; // For "[Inline]" label and Extract button
                    string key = GetInlineEditorKey(element);
                    if (_inlineItemEditors.TryGetValue(key, out Editor itemEditor) && itemEditor != null)
                    {
                        // Approximated height - real height calculation is complex.
                        // A simpler approach might be needed if this is unreliable.
                        // For now, let's use a fixed estimate or calculate based on properties.
                        // This simple calculation assumes a few standard property fields.
                        if (itemEditor.serializedObject.targetObject != null) {
                             SerializedProperty prop = itemEditor.serializedObject.GetIterator();
                             bool enterChildren = true;
                             while (prop.NextVisible(enterChildren))
                             {
                                 if (prop.name != "m_Script") // Skip script field
                                 {
                                     height += EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
                                 }
                                 enterChildren = false; // Don't go deep into children unless necessary
                             }
                        } else {
                             height += EditorGUIUtility.singleLineHeight * 4; // Estimate if target is null briefly
                        }

                    } else {
                         height += EditorGUIUtility.singleLineHeight * 4; // Estimate if editor not ready
                    }
                    return height;
                }
                else
                {
                    // Asset reference: Standard height
                    return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
                }
            };

            _itemList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                // Add null check for _itemList and its property
                if (_itemList?.serializedProperty == null || index >= _itemList.serializedProperty.arraySize) return;

                var element = _itemList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += EditorGUIUtility.standardVerticalSpacing;
                rect.height = EditorGUIUtility.singleLineHeight;

                bool isInline = element.objectReferenceValue != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(element.objectReferenceValue));
                string key = GetInlineEditorKey(element); // Get key regardless of inline status for cleanup logic

                if (isInline)
                {
                    // --- Inline Item ---
                    Rect labelRect = new Rect(rect.x, rect.y, rect.width - 80, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(labelRect, $"Element {index} [Inline]");

                    // Extract Button
                    Rect buttonRect = new Rect(rect.x + rect.width - 75, rect.y, 75, EditorGUIUtility.singleLineHeight);
                    if (GUI.Button(buttonRect, "Extract"))
                    {
                        ExtractInlineItem(index);
                        // Exit GUI here because the list structure changed
                        GUIUtility.ExitGUI();
                    }

                    // Get or create editor for the inline item
                    if (!_inlineItemEditors.TryGetValue(key, out Editor itemEditor) || itemEditor == null || itemEditor.target != element.objectReferenceValue)
                    {
                        // Clean up old editor if target changed
                        if (itemEditor != null) DestroyImmediate(itemEditor);

                        if (element.objectReferenceValue != null) {
                            itemEditor = Editor.CreateEditor(element.objectReferenceValue);
                            _inlineItemEditors[key] = itemEditor;
                        } else {
                             _inlineItemEditors.Remove(key); // Remove if object ref is null
                        }
                    }


                    // Draw the inline item's editor
                    if (itemEditor != null && itemEditor.target != null)
                    {
                        Rect editorRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, rect.width, rect.height); // Adjust rect as needed
                        // Indent the inline editor slightly
                        EditorGUI.indentLevel++;
                        itemEditor.serializedObject.Update(); // Ensure latest data
                        EditorGUI.BeginChangeCheck();
                        // Draw the default inspector fields for the inline item
                        // Skip drawing the script field
                        SerializedProperty prop = itemEditor.serializedObject.GetIterator();
                        bool enterChildren = true;
                        float currentY = editorRect.y;
                        while (prop.NextVisible(enterChildren))
                        {
                            if (prop.name != "m_Script") // Skip script field
                            {
                                float propHeight = EditorGUI.GetPropertyHeight(prop, true);
                                EditorGUI.PropertyField(new Rect(editorRect.x, currentY, editorRect.width, propHeight), prop, true);
                                currentY += propHeight + EditorGUIUtility.standardVerticalSpacing;
                            }
                            enterChildren = false; // Only iterate top-level properties
                        }

                        if (EditorGUI.EndChangeCheck())
                        {
                            itemEditor.serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(target); // Mark parent Set dirty when inline item changes
                        }
                        EditorGUI.indentLevel--;
                    } else {
                         EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, rect.width, EditorGUIUtility.singleLineHeight), "Error: Could not create editor for inline item.");
                    }
                }
                else
                {
                    // --- Asset Reference ---
                    EditorGUI.PropertyField(rect, element, GUIContent.none);
                     // Clean up editor cache if element is no longer inline or is null
                     if (_inlineItemEditors.ContainsKey(key))
                     {
                         DestroyImmediate(_inlineItemEditors[key]);
                         _inlineItemEditors.Remove(key);
                     }
                }
            };

             _itemList.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) => {
                // Add null check for serializedProperty
                if (l?.serializedProperty == null) return;
                var menu = new GenericMenu();

                // Option 1: Add slot for asset reference
                menu.AddItem(new GUIContent("Add Asset Reference Slot"), false, () => {
                    ReorderableList.defaultBehaviours.DoAddButton(l); // Default behavior adds a null slot
                });

                // Option 2: Create and add inline item
                menu.AddItem(new GUIContent("Create New Inline Item"), false, () => {
                    var index = l.serializedProperty.arraySize;
                    l.serializedProperty.arraySize++;
                    l.index = index;
                    var element = l.serializedProperty.GetArrayElementAtIndex(index);

                    // Create an instance of PicItem (or the appropriate item type)
                    var newItem = ScriptableObject.CreateInstance<PicItem>();
                    newItem.name = "New Inline PicItem"; // Default name
                    // IMPORTANT: Do NOT use AssetDatabase.CreateAsset here.
                    // Assign the instance directly to the serialized property.
                    // Unity will serialize it *with* the parent Set asset.
                    element.objectReferenceValue = newItem;

                    // Ensure the parent asset knows about this inline object for serialization
                    // Although ApplyModifiedProperties should handle it, explicit AddObjectToAsset
                    // *might* be needed if serialization fails, but try without first.
                    // AssetDatabase.AddObjectToAsset(newItem, target); // Try WITHOUT this first. It makes it a sub-asset.

                    serializedObject.ApplyModifiedProperties(); // Apply the array size change and new object reference
                    EditorUtility.SetDirty(target); // Mark the Set asset dirty
                });

                menu.ShowAsContext();
            };

            _itemList.onRemoveCallback = (ReorderableList l) => {
                 // Add null check for serializedProperty
                 if (l?.serializedProperty == null || l.index >= l.serializedProperty.arraySize) return;

                 var element = l.serializedProperty.GetArrayElementAtIndex(l.index);
                 var objToRemove = element.objectReferenceValue;

                 // If it was an inline item, destroy its instance
                 if (objToRemove != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(objToRemove)))
                 {
                     string key = GetInlineEditorKey(element);
                      if (_inlineItemEditors.TryGetValue(key, out Editor editor))
                      {
                          DestroyImmediate(editor); // Destroy cached editor first
                          _inlineItemEditors.Remove(key);
                      }
                     // IMPORTANT: Destroy the actual inline ScriptableObject instance
                     Undo.DestroyObjectImmediate(objToRemove); // Use Undo for proper removal
                     // No need to call AssetDatabase.RemoveObjectFromAsset if it wasn't added explicitly
                 }
                 // else: it's an asset reference or null, just remove the list element

                 // Use default remove behavior AFTER handling inline instance destruction
                 ReorderableList.defaultBehaviours.DoRemoveButton(l);

                 serializedObject.ApplyModifiedProperties();
                 EditorUtility.SetDirty(target);
            };
        }

         // Helper to get a unique key for the inline editor cache
        private string GetInlineEditorKey(SerializedProperty element)
        {
            // Use instance ID if available, otherwise property path as fallback
            return element.objectReferenceInstanceIDValue != 0
                ? element.objectReferenceInstanceIDValue.ToString()
                : element.propertyPath;
        }


        private void ExtractInlineItem(int index)
        {
            var element = _itemsProp.GetArrayElementAtIndex(index);
            var inlineItem = element.objectReferenceValue as PicItem;

            if (inlineItem == null) return;

            string suggestedName = string.IsNullOrEmpty(inlineItem.Name) ? "ExtractedPicItem" : inlineItem.Name;
            string path = EditorUtility.SaveFilePanelInProject("Save Pic Item Asset", suggestedName, "asset", "Please enter a file name to save the Pic Item to");

            if (string.IsNullOrEmpty(path)) return; // User cancelled

            // Create a new asset from the inline instance data
            // IMPORTANT: Create a *clone* to avoid modifying the instance before it's replaced.
            // However, CreateAsset works by saving the *existing* object instance to the path.
            // So, we don't need to clone first.

            // Remove the inline object from the list *before* creating the asset
            // to prevent potential conflicts if the list holds the same instance.
            // Store the reference, set the list element to null temporarily.
            element.objectReferenceValue = null;
            serializedObject.ApplyModifiedPropertiesWithoutUndo(); // Apply null assignment immediately

            // Create the asset file
            AssetDatabase.CreateAsset(inlineItem, path); // This saves the 'inlineItem' instance as a new asset
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Now, find the newly created asset and assign it back to the list element
            var newAsset = AssetDatabase.LoadAssetAtPath<PicItem>(path);
            if (newAsset != null)
            {
                element.objectReferenceValue = newAsset;
                serializedObject.ApplyModifiedProperties(); // Apply the new asset reference
                EditorUtility.SetDirty(target); // Mark parent dirty

                 // Clean up the inline editor cache for the extracted item
                 string key = GetInlineEditorKey(element); // Key might change, re-evaluate based on instance ID if needed
                 // Use the original instance ID before it was saved to find the editor
                 key = inlineItem.GetInstanceID().ToString();
                 if (_inlineItemEditors.TryGetValue(key, out Editor editor))
                 {
                     DestroyImmediate(editor);
                     _inlineItemEditors.Remove(key);
                 }
            }
            else
            {
                Debug.LogError($"[PicSetEditor] Failed to load the newly created asset at path: {path}. Restoring inline item.");
                // Restore the inline item if asset creation failed
                element.objectReferenceValue = inlineItem;
                serializedObject.ApplyModifiedProperties();
            }
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Display ID as read-only (Standardized)
            if (_idProp != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(_idProp, new GUIContent("ID"));
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.HelpBox("'_id' property not found.", MessageType.Warning);
            }


            EditorGUILayout.Space();

            // Check if _itemList is initialized before drawing it
            if (_itemList == null && _itemsProp != null) // Try to setup again only if _itemsProp is valid
            {
                // Attempt to initialize it if it's null and _itemsProp exists
                SetupItemList();
            }

            // Check again after attempting initialization
            if (_itemList != null)
            {
                _itemList.DoLayoutList();
            }
            else
            {
                // If it's still null after trying to set it up, show an error.
                EditorGUILayout.HelpBox("Failed to initialize the Picture Items list. Ensure the '_items' property exists and is correctly serialized on the PicSet.", MessageType.Error);
            }


            serializedObject.ApplyModifiedProperties();

             // Clean up any cached editors whose targets have been destroyed (e.g., due to Undo/Redo)
             // This is a basic cleanup, more robust handling might be needed
             List<string> keysToRemove = new List<string>();
             foreach (var kvp in _inlineItemEditors)
             {
                 if (kvp.Value == null || kvp.Value.target == null)
                 {
                     keysToRemove.Add(kvp.Key);
                 }
             }
             foreach (string key in keysToRemove)
             {
                  if (_inlineItemEditors.TryGetValue(key, out Editor editor) && editor != null) {
                     DestroyImmediate(editor); // Destroy the editor object itself
                  }
                 _inlineItemEditors.Remove(key);
             }
        }
    }
}
