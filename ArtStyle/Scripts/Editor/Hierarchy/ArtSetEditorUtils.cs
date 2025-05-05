// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using UnityEditor;
using System; // Required for Type
using System.IO; // Required for Path

namespace Modules.ArtStyle.Editors
{
    /// <summary>
    /// Utility methods for ArtSet related editors.
    /// </summary>
    public static class ArtSetEditorUtils
    {
        private static readonly Color InlineInstanceLabelColor = new Color(1.0f, 0.9f, 0.4f); // Yellowish

        /// <summary>
        /// Draws a UI section for managing a single ScriptableObject property.
        /// Handles assigning assets, clearing, creating inline instances, or creating direct assets.
        /// (Reverted version using bool flag for inline state)
        /// </summary>
        /// <param name="target">The target ScriptableObject containing the list/array being edited.</param>
        /// <param name="soProp">The SerializedProperty for the ScriptableObject reference (element in the list).</param>
        /// <param name="displayLabel">The main display label for the section.</param>
        /// <param name="objectType">The System.Type of the ScriptableObject.</param>
        /// <param name="isCreatingInline">Ref bool indicating if inline creation is active.</param>
        /// <param name="newInstanceName">Ref string holding the name for the new inline instance.</param>
        /// <param name="inlineCreationTitle">The title displayed during inline creation.</param>
        /// <param name="defaultInstanceName">The default name suggested for a new inline instance.</param>
        /// <param name="createAsAsset">If true, the 'Create' button directly creates an asset file instead of an inline instance.</param>
        public static void DrawScriptableObjectField(
            UnityEngine.Object target,
            SerializedProperty soProp,
            string displayLabel,
            Type objectType,
            ref bool isCreatingInline,      // Back to bool
            ref string newInstanceName,
            string inlineCreationTitle,
            string defaultInstanceName,
            bool createAsAsset)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;

            if (!createAsAsset && isCreatingInline) // Use bool flag
            {
                // --- Inline Creation UI ---
                EditorGUILayout.LabelField(inlineCreationTitle, EditorStyles.boldLabel);
                newInstanceName = EditorGUILayout.TextField("Instance Name", newInstanceName);

                EditorGUILayout.BeginHorizontal();
                try
                {
                    if (GUILayout.Button("Confirm"))
                    {
                        var newInstance = ScriptableObject.CreateInstance(objectType);
                        newInstance.name = newInstanceName;
                        if (EditorUtility.IsPersistent(target)) AssetDatabase.AddObjectToAsset(newInstance, target);
                        soProp.objectReferenceValue = newInstance;
                        if (EditorUtility.IsPersistent(target)) { EditorUtility.SetDirty(target); AssetDatabase.SaveAssets(); }
                        Undo.RecordObject(target, $"Create Inline {displayLabel}");
                        soProp.objectReferenceValue = newInstance;
                        isCreatingInline = false; // Reset bool flag
                        GUI.FocusControl(null);
                    }
                    if (GUILayout.Button("Cancel"))
                    {
                        isCreatingInline = false; // Reset bool flag
                        GUI.FocusControl(null);
                    }
                }
                finally { EditorGUILayout.EndHorizontal(); }
            }
            else // --- Normal Display ---
            {
                EditorGUILayout.LabelField(displayLabel, EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal(); // Field + Buttons horizontally
                try
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(soProp, GUIContent.none, true);
                    bool changed = EditorGUI.EndChangeCheck();
                    if (changed && !createAsAsset) { isCreatingInline = false; } // Reset bool flag

                    EditorGUILayout.BeginVertical(GUILayout.Width(120)); // Buttons vertically
                    try
                    {
                        UnityEngine.Object currentRef = soProp.objectReferenceValue;
                        bool isAssigned = currentRef != null;
                        bool clearClicked = false;

                        if (isAssigned)
                        {
                            // --- Re-introduce Inline Detection ---
                            // Use AssetDatabase.IsSubAsset for a more direct check
                            bool isInline = AssetDatabase.IsSubAsset(currentRef);
                            // --- End Inline Detection ---

                            // Extract Asset Button (Only if Inline AND not in createAsAsset mode)
                            // Check isInline here
                            if (!createAsAsset && isInline)
                            {
                                if (GUILayout.Button("Extract Asset"))
                                {
                                    // ... (Existing Extract Logic, ensure it uses currentRef) ...
                                    string suggestedName = string.IsNullOrEmpty(currentRef.name) ? $"Extracted_{objectType.Name}" : currentRef.name;
                                    string suggestedPath = $"Assets/ArtStyle Data/Extracted/{suggestedName}.asset"; // Ensure this base path exists or adjust
                                    string chosenPath = EditorUtility.SaveFilePanelInProject(
                                        "Save Inline Object as Asset", suggestedName + ".asset", "asset",
                                        $"Choose a location to save the {objectType.Name} asset.", Path.GetDirectoryName(suggestedPath));
                                    if (!string.IsNullOrEmpty(chosenPath))
                                    {
                                        Undo.RecordObject(target, $"Extract {displayLabel} Asset");
                                        // Important: Clone the object before removing it from the asset,
                                        // otherwise CreateAsset might fail or save the wrong state.
                                        var objectToExtract = UnityEngine.Object.Instantiate(currentRef);
                                        objectToExtract.name = Path.GetFileNameWithoutExtension(chosenPath); // Apply new name

                                        AssetDatabase.RemoveObjectFromAsset(currentRef); // Remove original sub-asset
                                        AssetDatabase.CreateAsset(objectToExtract, chosenPath); // Create new asset from clone

                                        // Update the property to point to the new asset
                                        soProp.objectReferenceValue = objectToExtract;

                                        EditorUtility.SetDirty(target); // Mark container dirty (reference changed)
                                        AssetDatabase.SaveAssets();
                                        AssetDatabase.Refresh();
                                        EditorGUIUtility.PingObject(objectToExtract);
                                        // isInline = false; // State will update on next repaint
                                    }
                                }
                            }

                            // Clear Button
                            if (GUILayout.Button("Clear"))
                            {
                                clearClicked = true;
                                Undo.RecordObject(target, $"Clear {displayLabel}");
                                var objToDestroy = currentRef;
                                soProp.objectReferenceValue = null;
                                // Use the calculated isInline flag here
                                if (!createAsAsset && isInline && objToDestroy != null)
                                {
                                    Undo.DestroyObjectImmediate(objToDestroy); // Destroy the sub-asset
                                    EditorUtility.SetDirty(target);
                                    AssetDatabase.SaveAssets();
                                }
                            }

                            // Labels
                            if (!clearClicked)
                            {
                                // Use the calculated isInline flag here
                                if (!createAsAsset && isInline)
                                {
                                    var c = GUI.color; GUI.color = InlineInstanceLabelColor;
                                    GUILayout.Label("Inline Instance", EditorStyles.miniLabel); GUI.color = c;
                                }
                                // Check if it's assigned but not a persistent asset (could be scene object, invalid ref, etc.)
                                // Also check it's not inline, as inline is handled above.
                                else if (!EditorUtility.IsPersistent(currentRef) && !isInline)
                                {
                                    var c = GUI.color; GUI.color = Color.red;
                                    GUILayout.Label("Not Asset Ref?", EditorStyles.miniLabel); GUI.color = c;
                                }
                            }
                        }
                        else // Not assigned
                        {
                            if (createAsAsset)
                            {
                                // --- Create Asset Button ---
                                if (GUILayout.Button("Create Asset"))
                                {
                                    // ... (Create Asset Logic) ...
                                    string suggestedName = defaultInstanceName;
                                    string defaultDirectory = "Assets/ArtStyle Data/Created";
                                    if (!Directory.Exists(defaultDirectory)) Directory.CreateDirectory(defaultDirectory);
                                    string suggestedPath = $"{defaultDirectory}/{suggestedName}.asset";
                                    string chosenPath = EditorUtility.SaveFilePanelInProject(
                                        $"Create New {objectType.Name} Asset", suggestedName + ".asset", "asset",
                                        $"Choose a location to save the new {objectType.Name} asset.", Path.GetDirectoryName(suggestedPath));
                                    if (!string.IsNullOrEmpty(chosenPath))
                                    {
                                        var newInstance = ScriptableObject.CreateInstance(objectType);
                                        newInstance.name = Path.GetFileNameWithoutExtension(chosenPath);
                                        Undo.RecordObject(target, $"Create {displayLabel} Asset");
                                        AssetDatabase.CreateAsset(newInstance, chosenPath); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
                                        soProp.objectReferenceValue = newInstance;
                                        EditorGUIUtility.PingObject(newInstance); GUI.FocusControl(null);
                                    }
                                }
                            }
                            else // createAsAsset is false
                            {
                                // --- Create Inline Button ---
                                if (GUILayout.Button("Create Inline"))
                                {
                                    isCreatingInline = true; // Set bool flag
                                    newInstanceName = defaultInstanceName;
                                }
                            }
                        }
                    } finally { EditorGUILayout.EndVertical(); } // End Buttons vertically
                } finally { EditorGUILayout.EndHorizontal(); } // End Field + Buttons horizontally
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical(); // End HelpBox
            EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
        }
    }
}
