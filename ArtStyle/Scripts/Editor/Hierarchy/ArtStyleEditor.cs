// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using UnityEditor;
using UnityEditorInternal; // Required for ReorderableList
using Modules.ArtStyle;

namespace Modules.ArtStyle.Editors
{
    [CustomEditor(typeof(ArtStyle))]
    public class ArtStyleEditor : Editor
    {
        private SerializedProperty _idProp;
        private SerializedProperty _nameProp;
        private SerializedProperty _artSetsProp;
        private ReorderableList _artSetList;

        private void OnEnable()
        {
            _idProp = serializedObject.FindProperty("_id");
            _nameProp = serializedObject.FindProperty("_name");
            _artSetsProp = serializedObject.FindProperty("_artSets");

            SetupArtSetList();
        }

        private void SetupArtSetList()
        {
            _artSetList = new ReorderableList(serializedObject, _artSetsProp, true, true, true, true);

            _artSetList.drawHeaderCallback = (Rect rect) =>
            {
                // Later, this header might reflect the mapping to ArtSetting slots
                EditorGUI.LabelField(rect, "Art Sets (Asset References)");
            };

            _artSetList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = _artSetList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;

                // Display the ArtSet asset reference field
                EditorGUI.PropertyField(rect, element, GUIContent.none);

                // Future: Could display the ArtSetType of the referenced ArtSet here
            };

            // Future: Add logic for handling mapping based on ArtSetting slots
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Display ID as read-only (Standardized)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_idProp, new GUIContent("ID"));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(_nameProp);

            EditorGUILayout.Space();

            // Draw the reorderable list for ArtSet assets
            _artSetList.DoLayoutList();

            // Future: Add UI for managing mapping to ArtSetting slots and handling extras.

            serializedObject.ApplyModifiedProperties();
        }
    }
}
