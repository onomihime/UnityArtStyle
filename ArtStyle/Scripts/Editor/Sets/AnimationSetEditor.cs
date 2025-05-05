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
    [CustomEditor(typeof(AnimationSet))]
    public class AnimationSetEditor : Editor
    {
        private SerializedProperty _idProp;
        private SerializedProperty _nameProp;
        private SerializedProperty _itemsProp;
        private ReorderableList _itemList;

        private void OnEnable()
        {
            _idProp = serializedObject.FindProperty("_id");
            // Try finding the custom "_name" property first
            _nameProp = serializedObject.FindProperty("_name");
            // If "_name" is not found, try finding the default "m_Name"
            if (_nameProp == null)
            {
                _nameProp = serializedObject.FindProperty("m_Name");
            }
            _itemsProp = serializedObject.FindProperty("_items");

            // Ensure _itemsProp is valid before setting up the list
            if (_itemsProp != null)
            {
                SetupItemList();
            }
            else
            {
                Debug.LogError("Could not find the '_items' property on the AnimationSet asset. ReorderableList cannot be initialized.");
                _itemList = null; // Ensure itemList is null if setup fails
            }
        }

        private void SetupItemList()
        {
            // Check if _itemsProp is valid before creating the list
            if (_itemsProp == null) return;

            _itemList = new ReorderableList(serializedObject, _itemsProp, true, true, true, true);

            _itemList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Animation Items (Asset References)");
            };

            _itemList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                // Add a null check for serializedProperty which can be null if the list is invalid
                if (_itemList.serializedProperty == null) return;

                var element = _itemList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(rect, element, GUIContent.none);
            };
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

            // Display the name property if found
            if (_nameProp != null)
            {
                EditorGUILayout.PropertyField(_nameProp, new GUIContent("Name"));
            }
            else
            {
                // If neither _name nor m_Name was found
                EditorGUILayout.HelpBox("Name property ('_name' or 'm_Name') not found.", MessageType.Warning);
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
                EditorGUILayout.HelpBox("Failed to initialize the Animation Items list. Ensure the '_items' property exists and is correctly serialized on the AnimationSet.", MessageType.Error);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
