using UnityEngine;
using UnityEditor;
using UnityEditorInternal; // Required for ReorderableList
using Modules.ArtStyle;
using System.Linq; // Required for Linq Select

namespace Modules.ArtStyle.Editors
{
    [CustomEditor(typeof(ArtSetting))]
    public class ArtSettingEditor : Editor
    {
        // Removed _idProp declaration
        private SerializedProperty _artSetTypeSlotsProp;
        private SerializedProperty _artStylesProp;
        private SerializedProperty _activeArtStyleIndexProp;

        private ReorderableList _slotList;
        private ReorderableList _styleList;

        private ArtSetting _targetSetting;

        private void OnEnable()
        {
            _targetSetting = (ArtSetting)target;

            // Removed _idProp initialization
            _artSetTypeSlotsProp = serializedObject.FindProperty("_artSetTypeSlots");
            _artStylesProp = serializedObject.FindProperty("_artStyles");
            _activeArtStyleIndexProp = serializedObject.FindProperty("_activeArtStyleIndex");

            SetupSlotList();
            SetupStyleList();
        }

        private void SetupSlotList()
        {
            _slotList = new ReorderableList(serializedObject, _artSetTypeSlotsProp, true, true, true, true);
            _slotList.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Art Set Type Slots (Asset References)");
            _slotList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = _slotList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(rect, element, GUIContent.none);
            };
        }

        private void SetupStyleList()
        {
            _styleList = new ReorderableList(serializedObject, _artStylesProp, true, true, true, true);
            _styleList.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Managed Art Styles (Asset References)");
            _styleList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = _styleList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(rect, element, GUIContent.none);
            };
             _styleList.onChangedCallback = (ReorderableList list) => {
                // Ensure index is clamped after list changes
                ClampActiveIndex();
            };
        }

        private void ClampActiveIndex()
        {
             int count = _artStylesProp.arraySize;
             if (count == 0) {
                 _activeArtStyleIndexProp.intValue = -1;
             } else {
                 _activeArtStyleIndexProp.intValue = Mathf.Clamp(_activeArtStyleIndexProp.intValue, 0, count - 1);
             }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // --- Duplicate Instance Warning ---
            if (ArtSetting.Instance != null && ArtSetting.Instance != _targetSetting)
            {
                // Store original color
                Color originalColor = GUI.color;
                // Set color to red for the HelpBox text
                GUI.color = Color.red;
                EditorGUILayout.HelpBox("DUPLICATE ArtSetting INSTANCE DETECTED!\nOnly one ArtSetting should be loaded. Please resolve the conflict.", MessageType.Error);
                // Restore original color
                GUI.color = originalColor;
                EditorGUILayout.Space(); // Add some space after the warning
            }

            // Removed ID field display
            // EditorGUI.BeginDisabledGroup(true);
            // EditorGUILayout.PropertyField(_idProp, new GUIContent("ID"));
            // EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(); // Keep space for layout

            _slotList.DoLayoutList();
            EditorGUILayout.Space();
            _styleList.DoLayoutList();
            EditorGUILayout.Space();

            // --- Active Style Selector ---
            EditorGUI.BeginChangeCheck();
            int currentActiveIndex = _activeArtStyleIndexProp.intValue;
            int styleCount = _artStylesProp.arraySize;
            string[] styleNames;
            int[] styleIndices;

            if (styleCount > 0)
            {
                styleNames = new string[styleCount];
                styleIndices = new int[styleCount];
                for (int i = 0; i < styleCount; i++)
                {
                    var styleProp = _artStylesProp.GetArrayElementAtIndex(i);
                    var styleObj = styleProp.objectReferenceValue as ArtStyle;
                    styleNames[i] = styleObj != null ? $"{i}: {styleObj.Name}" : $"{i}: (Missing)";
                    styleIndices[i] = i;
                }
            }
            else
            {
                styleNames = new string[] { "(No Styles Available)" };
                styleIndices = new int[] { -1 };
                currentActiveIndex = -1; // Ensure index is -1 if list is empty
            }

            int selectedIndex = EditorGUILayout.IntPopup("Active Art Style", currentActiveIndex, styleNames, styleIndices);

            if (EditorGUI.EndChangeCheck())
            {
                if (selectedIndex != _activeArtStyleIndexProp.intValue)
                {
                    
                    _activeArtStyleIndexProp.intValue = selectedIndex;
                    // Trigger the event manually from the editor for immediate feedback if needed
                    

                    if (_targetSetting != null)
                    {
                        // Manually invoke using the current property value
                        ArtStyle currentStyle = _targetSetting.ActiveArtStyle;
                        Debug.Log($"Auto triggering OnActiveStyleChanged. Current Style: {(currentStyle != null ? currentStyle.name : "null")}");
                        // Invoke with current style as both old and new for manual trigger? Or null, current?
                        // Let's invoke with (null, currentStyle) to simulate a change *to* the current style.
                        _targetSetting.OnActiveStyleChanged.Invoke(null, currentStyle);
                        
                        // Trigger again after a 1-second delay
                       /* EditorApplication.delayCall += () =>
                        {
                            _targetSetting.OnActiveStyleChanged.Invoke(null, currentStyle);
                        };*/
                    }
                    
                    // Note: This requires the instance to be loaded.
                    if (Application.isPlaying && ArtSetting.Instance != null)
                    {
                         // Get the ArtStyle corresponding to the selected index
                         ArtStyle selectedStyle = null;
                         if (selectedIndex >= 0 && selectedIndex < _artStylesProp.arraySize)
                         {
                             selectedStyle = _artStylesProp.GetArrayElementAtIndex(selectedIndex).objectReferenceValue as ArtStyle;
                         }
                         // Set the ActiveArtStyle property, which handles the index and event trigger
                         ArtSetting.Instance.ActiveArtStyle = selectedStyle;
                    }
                }
            }

            // Manual Trigger Button (for testing outside play mode)
            if (GUILayout.Button("Trigger OnActiveStyleChanged (Manual)"))
            {
                 if (_targetSetting != null)
                 {
                     // Manually invoke using the current property value
                     ArtStyle currentStyle = _targetSetting.ActiveArtStyle;
                     Debug.Log($"Manually triggering OnActiveStyleChanged. Current Style: {(currentStyle != null ? currentStyle.name : "null")}");
                     // Invoke with current style as both old and new for manual trigger? Or null, current?
                     // Let's invoke with (null, currentStyle) to simulate a change *to* the current style.
                     _targetSetting.OnActiveStyleChanged?.Invoke(null, currentStyle);
                 }
            }
            
             //Show the above event in the inspector for other scripts to subscribe to
             EditorGUILayout.LabelField("OnActiveStyleChanged Event", EditorStyles.boldLabel);
             EditorGUILayout.PropertyField(serializedObject.FindProperty("OnActiveStyleChanged"), GUIContent.none);
 
            EditorGUILayout.Space(); // Add spacing before the new button
 
            // --- Apply All Button ---
            if (GUILayout.Button("Apply All Applicators in Scene"))
            {
                ApplyToAllApplicators();
            }
            // -----------------------
 
             if (serializedObject.ApplyModifiedProperties())
             {
                // If properties changed (e.g., list reordered), re-clamp index
                ClampActiveIndex();
                serializedObject.ApplyModifiedPropertiesWithoutUndo(); // Apply the clamped index change
             }
        }
 
        /// <summary>
        /// Finds all active ArtSetApplicator components in the scene and calls ApplyStyle() on them.
        /// </summary>
        private void ApplyToAllApplicators()
        {
            // Find active applicators in the scene
            ArtSetApplicator[] applicators = FindObjectsOfType<ArtSetApplicator>();
 
            if (applicators.Length == 0)
            {
                Debug.Log("[ArtSettingEditor] No active ArtSetApplicators found in the scene.");
                return;
            }
 
            Undo.SetCurrentGroupName("Apply Style to All Applicators");
            int group = Undo.GetCurrentGroup();
 
            int appliedCount = 0;
            foreach (ArtSetApplicator applicator in applicators)
            {
                // Record the object for undo before applying changes
                // Note: ApplyStyleEditor might modify the component or its targets
                Undo.RecordObject(applicator, "Apply Style via ArtSetting");
                // Also record target components if ApplyStyle modifies them directly (e.g., Image, Text)
                foreach(var imgElement in applicator._imageElements) if(imgElement.targetImage) Undo.RecordObject(imgElement.targetImage, "Apply Style via ArtSetting");
                foreach(var txtElement in applicator._legacyTextElements) if(txtElement.targetText) Undo.RecordObject(txtElement.targetText, "Apply Style via ArtSetting");
                foreach(var tmpElement in applicator._tmpTextElements) if(tmpElement.targetText) Undo.RecordObject(tmpElement.targetText, "Apply Style via ArtSetting");
                // Animation targets usually aren't directly modified by ApplyStyle, only PlayAnimation
 
                applicator.ApplyStyleEditor(); // Use the editor method which forces rebuild
                appliedCount++;
            }
 
            Undo.CollapseUndoOperations(group);
 
            Debug.Log($"[ArtSettingEditor] Applied style to {appliedCount} ArtSetApplicator(s) in the scene.");
            SceneView.RepaintAll(); // Repaint scene view to reflect changes
        }
    }
 }
