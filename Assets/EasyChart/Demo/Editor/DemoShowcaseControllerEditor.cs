using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EasyChart.Demo;

namespace EasyChart.Editor
{
    [CustomEditor(typeof(DemoShowcaseController))]
    public class DemoShowcaseControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty _uiDocumentProp;
        private SerializedProperty _demoProfilesProp;
        private SerializedProperty _activeGroupProp;
        private SerializedProperty _nextGroupKeyProp;
        private SerializedProperty _prevGroupKeyProp;
        private SerializedProperty _nextbuttonProp;
        private SerializedProperty _lastbuttonProp;
        private SerializedProperty _slotBackgroundColorProp;
        private SerializedProperty _slotBorderColorProp;

        private Vector2 _scrollPosition;
        private Dictionary<int, bool> _groupFoldouts = new Dictionary<int, bool>();

        private void OnEnable()
        {
            _uiDocumentProp = serializedObject.FindProperty("_uiDocument");
            _demoProfilesProp = serializedObject.FindProperty("_demoProfiles");
            _activeGroupProp = serializedObject.FindProperty("_activeGroup");
            _nextGroupKeyProp = serializedObject.FindProperty("_nextGroupKey");
            _prevGroupKeyProp = serializedObject.FindProperty("_prevGroupKey");
            _nextbuttonProp = serializedObject.FindProperty("_nextbutton");
            _lastbuttonProp = serializedObject.FindProperty("_lastbutton");
            _slotBackgroundColorProp = serializedObject.FindProperty("_slotBackgroundColor");
            _slotBorderColorProp = serializedObject.FindProperty("_slotBorderColor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_uiDocumentProp);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Navigation Keys", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_activeGroupProp);
            EditorGUILayout.PropertyField(_nextGroupKeyProp);
            EditorGUILayout.PropertyField(_prevGroupKeyProp);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("UGUI Navigation Buttons", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_nextbuttonProp);
            EditorGUILayout.PropertyField(_lastbuttonProp);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_slotBackgroundColorProp, new GUIContent("Slot Background"));
            EditorGUILayout.PropertyField(_slotBorderColorProp, new GUIContent("Slot Border"));

            EditorGUILayout.Space(15);

            DrawDemoGrid();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDemoGrid()
        {
            int totalCount = _demoProfilesProp.arraySize;
            int groupCount = Mathf.CeilToInt(totalCount / 16f);
            if (groupCount == 0) groupCount = 1;

            EditorGUILayout.LabelField($"Demo Profiles ({totalCount} total, {groupCount} groups)", EditorStyles.boldLabel);

            // Load from folder button
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Load from Folder (Recursive)", GUILayout.Height(25)))
            {
                LoadFromFolder();
            }
            
            if (GUILayout.Button("Add from Selection", GUILayout.Height(25)))
            {
                AddFromSelection();
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Clear All"))
            {
                if (EditorUtility.DisplayDialog("Clear All Demos", "Are you sure you want to clear all demo slots?", "Yes", "No"))
                {
                    ClearArray(_demoProfilesProp);
                }
            }
            
            if (GUILayout.Button("Remove Empty Slots"))
            {
                RemoveEmptySlots();
            }
            
            if (GUILayout.Button("+ Add 16 Slots"))
            {
                int newSize = _demoProfilesProp.arraySize + 16;
                _demoProfilesProp.arraySize = newSize;
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Scrollable group list
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(400));

            for (int g = 0; g < groupCount; g++)
            {
                int startIndex = g * 16;
                int endIndex = Mathf.Min(startIndex + 16, totalCount);
                
                if (!_groupFoldouts.ContainsKey(g))
                {
                    _groupFoldouts[g] = g == 0;
                }

                // Group box
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                string groupLabel = $"Group {g + 1} (#{startIndex + 1} - #{endIndex})";
                _groupFoldouts[g] = EditorGUILayout.Foldout(_groupFoldouts[g], groupLabel, true, EditorStyles.foldoutHeader);
                
                if (_groupFoldouts[g])
                {
                    EditorGUILayout.Space(3);
                    DrawGroupSlots(startIndex, endIndex);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
        }

        private void LoadFromFolder()
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Folder with ChartProfiles", "Assets", "");
            if (string.IsNullOrEmpty(folderPath)) return;

            // Convert to relative path
            if (folderPath.StartsWith(Application.dataPath))
            {
                folderPath = "Assets" + folderPath.Substring(Application.dataPath.Length);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please select a folder inside the Assets directory.", "OK");
                return;
            }

            // Find all ChartProfile assets recursively
            var guids = AssetDatabase.FindAssets("t:ChartProfile", new[] { folderPath });
            
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("No Profiles Found", $"No ChartProfile assets found in:\n{folderPath}", "OK");
                return;
            }

            var profiles = new List<ChartProfile>();
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<ChartProfile>(assetPath);
                if (profile != null)
                {
                    profiles.Add(profile);
                }
            }

            // Sort by name
            profiles = profiles.OrderBy(p => p.name).ToList();

            // Ask user whether to replace or append
            int choice = EditorUtility.DisplayDialogComplex(
                "Load Profiles",
                $"Found {profiles.Count} ChartProfile(s).\nHow would you like to add them?",
                "Replace All",
                "Cancel",
                "Append"
            );

            if (choice == 1) return; // Cancel

            if (choice == 0) // Replace
            {
                ClearArray(_demoProfilesProp);
            }

            int startIndex = _demoProfilesProp.arraySize;
            _demoProfilesProp.arraySize = startIndex + profiles.Count;

            for (int i = 0; i < profiles.Count; i++)
            {
                _demoProfilesProp.GetArrayElementAtIndex(startIndex + i).objectReferenceValue = profiles[i];
            }

            Debug.Log($"[DemoShowcase] Loaded {profiles.Count} ChartProfiles from {folderPath}");
        }

        private void AddFromSelection()
        {
            var selected = Selection.objects;
            int addedCount = 0;

            foreach (var obj in selected)
            {
                if (obj is ChartProfile profile)
                {
                    // Check if already exists
                    bool exists = false;
                    for (int i = 0; i < _demoProfilesProp.arraySize; i++)
                    {
                        if (_demoProfilesProp.GetArrayElementAtIndex(i).objectReferenceValue == profile)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        int index = _demoProfilesProp.arraySize;
                        _demoProfilesProp.arraySize = index + 1;
                        _demoProfilesProp.GetArrayElementAtIndex(index).objectReferenceValue = profile;
                        addedCount++;
                    }
                }
            }

            if (addedCount > 0)
            {
                Debug.Log($"[DemoShowcase] Added {addedCount} ChartProfile(s) from selection");
            }
        }

        private void RemoveEmptySlots()
        {
            for (int i = _demoProfilesProp.arraySize - 1; i >= 0; i--)
            {
                if (_demoProfilesProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    DeleteArrayElement(_demoProfilesProp, i);
                }
            }
        }

        private void DeleteArrayElement(SerializedProperty arrayProp, int index)
        {
            if (index < 0 || index >= arrayProp.arraySize) return;
            
            // If element is object reference and not null, first set to null
            var element = arrayProp.GetArrayElementAtIndex(index);
            if (element.propertyType == SerializedPropertyType.ObjectReference && element.objectReferenceValue != null)
            {
                element.objectReferenceValue = null;
            }
            
            arrayProp.DeleteArrayElementAtIndex(index);
        }

        private void ClearArray(SerializedProperty arrayProp)
        {
            // Clear all object references first to avoid Unity's quirk
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var element = arrayProp.GetArrayElementAtIndex(i);
                if (element.propertyType == SerializedPropertyType.ObjectReference)
                {
                    element.objectReferenceValue = null;
                }
            }
            arrayProp.arraySize = 0;
        }

        private void DrawGroupSlots(int startIndex, int endIndex)
        {
            // Draw slots as a simple list (more reliable than grid in narrow inspector)
            for (int i = startIndex; i < endIndex; i++)
            {
                if (i >= _demoProfilesProp.arraySize) break;
                
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.LabelField($"#{i + 1:D2}", GUILayout.Width(35));
                
                var prop = _demoProfilesProp.GetArrayElementAtIndex(i);
                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUILayout.ObjectField(
                    prop.objectReferenceValue,
                    typeof(ChartProfile),
                    false
                ) as ChartProfile;
                
                if (EditorGUI.EndChangeCheck())
                {
                    prop.objectReferenceValue = newValue;
                }

                // Move up/down buttons
                EditorGUI.BeginDisabledGroup(i == 0);
                if (GUILayout.Button("↑", GUILayout.Width(22)))
                {
                    SwapElements(i, i - 1);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(i >= _demoProfilesProp.arraySize - 1);
                if (GUILayout.Button("↓", GUILayout.Width(22)))
                {
                    SwapElements(i, i + 1);
                }
                EditorGUI.EndDisabledGroup();

                // Delete button
                if (GUILayout.Button("×", GUILayout.Width(22)))
                {
                    DeleteArrayElement(_demoProfilesProp, i);
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }

        private void SwapElements(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= _demoProfilesProp.arraySize) return;
            if (indexB < 0 || indexB >= _demoProfilesProp.arraySize) return;

            var tempA = _demoProfilesProp.GetArrayElementAtIndex(indexA).objectReferenceValue;
            var tempB = _demoProfilesProp.GetArrayElementAtIndex(indexB).objectReferenceValue;
            
            _demoProfilesProp.GetArrayElementAtIndex(indexA).objectReferenceValue = tempB;
            _demoProfilesProp.GetArrayElementAtIndex(indexB).objectReferenceValue = tempA;
        }
    }
}
