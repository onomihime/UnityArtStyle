// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using UnityEditor;
using Modules.ArtStyle;

namespace Modules.ArtStyle.Editors
{
    [CustomEditor(typeof(FontItem))]
    public class FontItemEditor : Editor
    {
        private SerializedProperty _idProp;
        private SerializedProperty _nameProp;
        private SerializedProperty _fontProp;
        private SerializedProperty _tmpFontProp;
        private SerializedProperty _defaultColourProp;

        private void OnEnable()
        {
            _idProp = serializedObject.FindProperty("_id");
            _nameProp = serializedObject.FindProperty("_name");
            _fontProp = serializedObject.FindProperty("_font");
            _tmpFontProp = serializedObject.FindProperty("_tmpFont");
            _defaultColourProp = serializedObject.FindProperty("_defaultColour");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Display ID as read-only (Standardized)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_idProp, new GUIContent("ID"));
            EditorGUI.EndDisabledGroup();

            // Draw other properties
            EditorGUILayout.PropertyField(_nameProp);
            EditorGUILayout.PropertyField(_fontProp);
            EditorGUILayout.PropertyField(_tmpFontProp);
            EditorGUILayout.PropertyField(_defaultColourProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
