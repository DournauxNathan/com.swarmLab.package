using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;

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

        private void OnEnable()
        {
            _speciesConfigsProp = serializedObject.FindProperty("speciesConfigs");
            FindAllSpeciesAssets();

            _speciesList = new ReorderableList(serializedObject, _speciesConfigsProp, true, true, true, true);

            _speciesList.drawHeaderCallback = (Rect rect) =>
            {
                // Show count vs max available (e.g. "Population Configuration (2/5)")
                string header = $"Population Configuration ({_speciesList.count}/{_allSpeciesAssets.Count})";
                EditorGUI.LabelField(rect, header);
            };

            // --- 1. Prevent adding more rows than available assets ---
            _speciesList.onCanAddCallback = (ReorderableList list) =>
            {
                return list.count < _allSpeciesAssets.Count;
            };

            // --- 2. Custom Add Logic (Avoids copying previous element) ---
            _speciesList.onAddCallback = (ReorderableList list) =>
            {
                // Add a new empty element
                int index = list.serializedProperty.arraySize;
                list.serializedProperty.arraySize++;
                list.index = index; // Select the new item

                var newElement = list.serializedProperty.GetArrayElementAtIndex(index);

                // RESET VALUES (Overwrite the copy behavior)
                // 1. Clear Species Reference
                newElement.FindPropertyRelative("speciesDefinition").objectReferenceValue = null;
                
                // 2. Reset numerical values
                newElement.FindPropertyRelative("count").intValue = _defaultPopulationCount;
                newElement.FindPropertyRelative("spawnRadius").floatValue = _defaultSpawnRadius;

                // 3. Clear the inner Rules list (Very important!)
                var rulesProp = newElement.FindPropertyRelative("steeringRules");
                if (rulesProp.isArray)
                {
                    rulesProp.ClearArray(); 
                }
            };

            // Dynamic height calculation
            _speciesList.elementHeightCallback = (int index) =>
            {
                var element = _speciesConfigsProp.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element, true) + 25f;
            };

            // Drawing the Element
            _speciesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = _speciesConfigsProp.GetArrayElementAtIndex(index);
                var speciesDefProp = element.FindPropertyRelative("speciesDefinition");

                rect.y += 4; 
                rect.height -= 4;

                Rect buttonRect = new Rect(rect.x, rect.y, rect.width, 18);
                Rect propertiesRect = new Rect(rect.x, rect.y + 22, rect.width, rect.height - 22);

                // Smart Selector Button
                SpeciesDefinition currentSpecies = speciesDefProp.objectReferenceValue as SpeciesDefinition;
                string btnLabel = currentSpecies != null ? currentSpecies.name : "Select Species...";

                // Change button color to red if null (Visual prompt to select something)
                var prevColor = GUI.backgroundColor;
                if (currentSpecies == null) GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);

                if (GUI.Button(buttonRect, new GUIContent(btnLabel), EditorStyles.popup))
                {
                    ShowSpeciesMenu(speciesDefProp, currentSpecies);
                }
                GUI.backgroundColor = prevColor;

                DrawPropertiesExcluding(propertiesRect, element, "speciesDefinition");
            };
        }

        // ... (Keep your existing ShowSpeciesMenu, FindAllSpeciesAssets, OnInspectorGUI, etc.) ...
        // ... (They do not need to change) ...

        // Just ensuring you have the helper method for reference:
        private void DrawPropertiesExcluding(Rect rect, SerializedProperty rootProp, string excludeName)
        {
            SerializedProperty prop = rootProp.Copy();
            SerializedProperty endProp = rootProp.GetEndProperty();
            if (prop.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(prop, endProp)) break;
                    if (prop.name == excludeName) continue;
                    float h = EditorGUI.GetPropertyHeight(prop, true);
                    Rect r = new Rect(rect.x, rect.y, rect.width, h);
                    EditorGUI.PropertyField(r, prop, true);
                    rect.y += h + 2;
                }
                while (prop.NextVisible(false));
            }
        }
        
        private void ShowSpeciesMenu(SerializedProperty property, SpeciesDefinition currentSelection)
        {
             // ... (Keep your existing menu logic) ...
             // Just remember to keep the logic where we disable items that are already in the list!
             // Copy-paste your previous ShowSpeciesMenu here.
             GenericMenu menu = new GenericMenu();
             HashSet<SpeciesDefinition> usedSpecies = new HashSet<SpeciesDefinition>();
             for (int i = 0; i < _speciesConfigsProp.arraySize; i++)
             {
                 var el = _speciesConfigsProp.GetArrayElementAtIndex(i);
                 var def = el.FindPropertyRelative("speciesDefinition").objectReferenceValue as SpeciesDefinition;
                 if (def != null) usedSpecies.Add(def);
             }

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
             EditorGUILayout.Space();
             EditorGUILayout.LabelField("Swarm Settings", EditorStyles.boldLabel);
             _speciesList.DoLayoutList();
             serializedObject.ApplyModifiedProperties();
        }
    }
}