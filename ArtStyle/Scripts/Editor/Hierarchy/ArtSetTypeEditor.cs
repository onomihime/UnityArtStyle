// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using UnityEditor;
using UnityEditorInternal; // Required for ReorderableList
using Modules.ArtStyle;
using System.Collections.Generic; // Required for List<>
using System; // Required for Guid

namespace Modules.ArtStyle.Editors
{
    [CustomEditor(typeof(ArtSetType))]
    public class ArtSetTypeEditor : Editor
    {
        private SerializedProperty _idProp;
        private SerializedProperty _nameProp;
        private SerializedProperty _picItemTypesProp;
        private SerializedProperty _colourItemTypesProp;
        private SerializedProperty _fontItemTypesProp;
        private SerializedProperty _animationItemTypesProp;

        private ReorderableList _picList;
        private ReorderableList _colourList;
        private ReorderableList _fontList;
        private ReorderableList _animList;

        private ArtSetType _targetScript;

        private void OnEnable()
        {
            _targetScript = (ArtSetType)target;

            _idProp = serializedObject.FindProperty("_id");
            _nameProp = serializedObject.FindProperty("_name");
            _picItemTypesProp = serializedObject.FindProperty("_picItemTypes");
            _colourItemTypesProp = serializedObject.FindProperty("_colourItemTypes");
            _fontItemTypesProp = serializedObject.FindProperty("_fontItemTypes");
            _animationItemTypesProp = serializedObject.FindProperty("_animationItemTypes");

            _picList = CreateItemList(_picItemTypesProp, "Picture Item Types", typeof(PicItemType));
            _colourList = CreateItemList(_colourItemTypesProp, "Colour Item Types", typeof(ColourItemType));
            _fontList = CreateItemList(_fontItemTypesProp, "Font Item Types", typeof(FontItemType));
            _animList = CreateItemList(_animationItemTypesProp, "Animation Item Types", typeof(AnimationItemType));
        }

        private ReorderableList CreateItemList(SerializedProperty property, string header, Type itemType)
        {
            var list = new ReorderableList(serializedObject, property, true, true, true, true);

            list.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, header);

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                float halfWidth = rect.width / 2 - 5;
                Rect nameRect = new Rect(rect.x, rect.y, halfWidth, EditorGUIUtility.singleLineHeight);
                Rect idRect = new Rect(rect.x + halfWidth + 10, rect.y, halfWidth, EditorGUIUtility.singleLineHeight);

                // Ensure ID exists before drawing
                var idProp = element.FindPropertyRelative("_id");
                if (string.IsNullOrEmpty(idProp.stringValue))
                {
                     // This relies on the OnEnable logic in ArtSetType to have run,
                     // but we force a check/apply here just in case.
                     _targetScript.ValidateItemTypeIds(); // Ensure IDs are generated
                     serializedObject.Update(); // Re-fetch potentially updated data
                     idProp = element.FindPropertyRelative("_id"); // Get it again
                }


                // Use PropertyField for consistency, keep disabled text field for ItemType IDs
                EditorGUI.PropertyField(nameRect, element.FindPropertyRelative("_name"), GUIContent.none);
                EditorGUI.BeginDisabledGroup(true); // Make ItemType ID read-only
                EditorGUI.TextField(idRect, "ID", idProp.stringValue); // Keep TextField here for specific layout
                EditorGUI.EndDisabledGroup();
            };

            list.onAddCallback = (ReorderableList l) =>
            {
                var index = l.serializedProperty.arraySize;
                l.serializedProperty.arraySize++;
                l.index = index;
                var element = l.serializedProperty.GetArrayElementAtIndex(index);
                // Reset fields for the new element (especially ID)
                element.FindPropertyRelative("_id").stringValue = Guid.NewGuid().ToString(); // Generate ID immediately
                element.FindPropertyRelative("_name").stringValue = $"New {itemType.Name}";

                // No need to call EnsureId here as we set it directly.
                // Let the main script handle saving via ApplyModifiedProperties.
            };

            return list;
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
            EditorGUILayout.LabelField("Item Type Definitions", EditorStyles.boldLabel);

            _picList.DoLayoutList();
            _colourList.DoLayoutList();
            _fontList.DoLayoutList();
            _animList.DoLayoutList();

            // Apply changes and ensure IDs are validated/saved
            if (serializedObject.ApplyModifiedProperties())
            {
                 // If properties were changed (e.g., name edited), ensure IDs are still valid
                 // This might be redundant if OnEnable handles it, but safe to include.
                 _targetScript.ValidateItemTypeIds();
            }
        }
    }
}
