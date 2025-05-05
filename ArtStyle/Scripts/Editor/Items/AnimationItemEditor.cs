// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using UnityEditor;
using Modules.ArtStyle;

namespace Modules.ArtStyle.Editors
{
    [CustomEditor(typeof(AnimationItem))]
    public class AnimationItemEditor : Editor
    {
        private SerializedProperty _idProp;
        private SerializedProperty _nameProp;
        private SerializedProperty _durationProp;
        private SerializedProperty _useFadeProp;
        private SerializedProperty _fadeStartOpacityProp;
        private SerializedProperty _curveProp;

        private void OnEnable()
        {
            _idProp = serializedObject.FindProperty("_id");
            _nameProp = serializedObject.FindProperty("_name");
            _durationProp = serializedObject.FindProperty("_duration");
            _useFadeProp = serializedObject.FindProperty("_useFade");
            _fadeStartOpacityProp = serializedObject.FindProperty("_fadeStartOpacity");
            _curveProp = serializedObject.FindProperty("_curve");
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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Properties", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_durationProp);
            EditorGUILayout.PropertyField(_useFadeProp);
            if (_useFadeProp.boolValue) // Only show fade properties if enabled
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_fadeStartOpacityProp);
                EditorGUILayout.PropertyField(_curveProp);
                EditorGUI.indentLevel--;
            }


            serializedObject.ApplyModifiedProperties();
        }
    }
}
