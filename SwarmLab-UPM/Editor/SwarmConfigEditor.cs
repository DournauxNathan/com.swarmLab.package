using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using SwarmLab.Core;

namespace SwarmLab.Editor
{
    [CustomEditor(typeof(SwarmConfig))]
    public class SwarmConfigEditor : UnityEditor.Editor
    {
        private readonly int _defaultPopulationCount = 10;
        private readonly float _defaultSpawnRadius = 1f;
        
        private ReorderableList _speciesList;
        private SerializedProperty _speciesConfigsProp;
        private List<SpeciesDefinition> _allSpeciesAssets;

        // Constants for consistent layout
        private const float TOP_PADDING = 4f;
        private const float BUTTON_HEIGHT = 20f; // Increased slightly for better click area
        private const float SPACING = 2f;

        private void OnEnable()
        {
            _speciesConfigsProp = serializedObject.FindProperty("speciesConfigs");
            FindAllSpeciesAssets();

            _speciesList = new ReorderableList(serializedObject, _speciesConfigsProp, true, true, true, true);

            _speciesList.drawHeaderCallback = (Rect rect) =>
            {
                int totalAssets = _allSpeciesAssets != null ? _allSpeciesAssets.Count : 0;
                EditorGUI.LabelField(rect, $"Population Configuration ({_speciesList.count}/{totalAssets})");
            };

            _speciesList.onCanAddCallback = (ReorderableList list) =>
            {
                if (_allSpeciesAssets == null || _allSpeciesAssets.Count == 0) FindAllSpeciesAssets();
                return list.count < _allSpeciesAssets.Count;
            };

            _speciesList.onAddCallback = (ReorderableList list) =>
            {
                int index = list.serializedProperty.arraySize;
                list.serializedProperty.arraySize++;
                list.index = index; 

                var newElement = list.serializedProperty.GetArrayElementAtIndex(index);
                newElement.FindPropertyRelative("speciesDefinition").objectReferenceValue = null;
                newElement.FindPropertyRelative("count").intValue = _defaultPopulationCount;
                newElement.FindPropertyRelative("spawnRadius").floatValue = _defaultSpawnRadius;

                var rulesProp = newElement.FindPropertyRelative("steeringRules");
                if (rulesProp.isArray) rulesProp.ClearArray(); 
            };

            // --- THE FIX: ROBUST HEIGHT CALCULATION ---
            _speciesList.elementHeightCallback = (int index) =>
            {
                if (index >= _speciesConfigsProp.arraySize) return 0f;
                var element = _speciesConfigsProp.GetArrayElementAtIndex(index);
                
                // Calculate height exactly matching the drawing logic
                return CalculateElementHeight(element);
            };

            _speciesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = _speciesConfigsProp.GetArrayElementAtIndex(index);
                var speciesDefProp = element.FindPropertyRelative("speciesDefinition");

                // Apply Top Padding
                rect.y += TOP_PADDING; 
                
                // 1. Draw the Button
                Rect buttonRect = new Rect(rect.x, rect.y, rect.width, BUTTON_HEIGHT);
                
                SpeciesDefinition currentSpecies = speciesDefProp.objectReferenceValue as SpeciesDefinition;
                string btnLabel = currentSpecies != null ? currentSpecies.name : "Select Species...";

                var prevColor = GUI.backgroundColor;
                if (currentSpecies == null) GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);

                if (GUI.Button(buttonRect, new GUIContent(btnLabel), EditorStyles.popup))
                {
                    FindAllSpeciesAssets(); 
                    ShowSpeciesMenu(speciesDefProp, currentSpecies);
                }
                GUI.backgroundColor = prevColor;

                // 2. Draw the Properties below the button
                // Move rect down by Button Height + Spacing
                float currentY = rect.y + BUTTON_HEIGHT + SPACING;
                
                // We pass the starting Y so the helper knows where to draw
                DrawPropertiesExcluding(element, "speciesDefinition", rect.x, currentY, rect.width);
            };
        }

        // --- NEW HELPER: Strict Height Calculation ---
        private float CalculateElementHeight(SerializedProperty rootProp)
        {
            float height = TOP_PADDING + BUTTON_HEIGHT + SPACING;

            SerializedProperty prop = rootProp.Copy();
            SerializedProperty endProp = rootProp.GetEndProperty();

            // Enter children
            if (prop.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(prop, endProp)) break;
                    if (prop.name == "speciesDefinition") continue; // We account for this via BUTTON_HEIGHT

                    // Add height of this property
                    height += EditorGUI.GetPropertyHeight(prop, true);
                    
                    // Add spacing between properties
                    height += SPACING;
                }
                while (prop.NextVisible(false));
            }

            // Add a little bottom padding
            height += 4f; 
            
            return height;
        }

        // --- NEW HELPER: Strict Drawing ---
        // Now accepts X, Y, Width explicitly to avoid drift
        private void DrawPropertiesExcluding(SerializedProperty rootProp, string excludeName, float x, float startY, float width)
        {
            SerializedProperty prop = rootProp.Copy();
            SerializedProperty endProp = rootProp.GetEndProperty();
            
            float currentY = startY;

            if (prop.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(prop, endProp)) break;
                    if (prop.name == excludeName) continue;

                    float h = EditorGUI.GetPropertyHeight(prop, true);
                    Rect r = new Rect(x, currentY, width, h);
                    
                    EditorGUI.PropertyField(r, prop, true);
                    
                    currentY += h + SPACING;
                }
                while (prop.NextVisible(false));
            }
        }

        // ... (Keep ShowSpeciesMenu, FindAllSpeciesAssets, and OnInspectorGUI the same) ...
        // They are safe.
        
        private void ShowSpeciesMenu(SerializedProperty property, SpeciesDefinition currentSelection)
        {
             // Copy-paste your existing Menu code here
             // ...
             GenericMenu menu = new GenericMenu();
             HashSet<SpeciesDefinition> usedSpecies = new HashSet<SpeciesDefinition>();
             for (int i = 0; i < _speciesConfigsProp.arraySize; i++)
             {
                 var el = _speciesConfigsProp.GetArrayElementAtIndex(i);
                 var def = el.FindPropertyRelative("speciesDefinition").objectReferenceValue as SpeciesDefinition;
                 if (def != null) usedSpecies.Add(def);
             }

             if (_allSpeciesAssets == null) FindAllSpeciesAssets();

             foreach (var species in _allSpeciesAssets)
             {
                 bool isUsed = usedSpecies.Contains(species);
                 bool isCurrent = species == currentSelection;

                 if (isUsed && !isCurrent)
                 {
                     menu.AddDisabledItem(new GUIContent(species.name + " (Already Added)"));
                 }
                 else
                 {
                     menu.AddItem(new GUIContent(species.name), isCurrent, () =>
                     {
                         property.serializedObject.Update();
                         property.objectReferenceValue = species;
                         property.serializedObject.ApplyModifiedProperties();
                     });
                 }
             }
             if (_allSpeciesAssets.Count == 0) menu.AddDisabledItem(new GUIContent("No Species Assets found"));
             menu.ShowAsContext();
        }

        private void FindAllSpeciesAssets()
        {
            _allSpeciesAssets = new List<SpeciesDefinition>();
            string[] guids = AssetDatabase.FindAssets("t:SpeciesDefinition");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SpeciesDefinition asset = AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(path);
                if (asset != null) _allSpeciesAssets.Add(asset);
            }
        }
        
        public override void OnInspectorGUI()
        {
             serializedObject.Update();
             if (_allSpeciesAssets == null || _allSpeciesAssets.Count == 0) FindAllSpeciesAssets();
             EditorGUILayout.Space();
             EditorGUILayout.LabelField("Swarm Settings", EditorStyles.boldLabel);
             if (_speciesList != null) _speciesList.DoLayoutList();
             serializedObject.ApplyModifiedProperties();
        }
    }
}