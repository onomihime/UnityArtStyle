// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using UnityEditor;
using Modules.ArtStyle;

namespace Modules.ArtStyle.Editors
{
    [CustomEditor(typeof(AnimationApplicator))]
    public class AnimationApplicatorEditor : Editor
    {
        private SerializedProperty _defaultAnimationItemProp;

        private void OnEnable()
        {
            // Cache the serialized property for the default item
            _defaultAnimationItemProp = serializedObject.FindProperty("_defaultAnimationItem");
        }

        public override void OnInspectorGUI()
        {
            // Draw the default fields like _playOnEnable and _defaultAnimationItem
            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties(); // Apply changes made by DrawDefaultInspector

            EditorGUILayout.Space();

            // Get the target component instance
            AnimationApplicator applicator = (AnimationApplicator)target;

            // --- Play Button ---
            EditorGUI.BeginDisabledGroup(!Application.isPlaying); // Disable if not in Play Mode
            if (GUILayout.Button("Play Animation (Runtime Only)"))
            {
                if (Application.isPlaying)
                {
                    // Get the default item reference from the serialized property
                    AnimationItem defaultItem = _defaultAnimationItemProp.objectReferenceValue as AnimationItem;

                    if (defaultItem != null)
                    {
                        // Call the Play method on the target instance using the default item
                        applicator.Play(defaultItem, defaultItem.Duration);
                    }
                    else
                    {
                        Debug.LogWarning("Cannot play animation from editor: Default Animation Item is not assigned.", applicator);
                    }
                }
            }
            EditorGUI.EndDisabledGroup();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Animation playback via button is only available in Play Mode.", MessageType.Info);
            }
        }
    }
}
