using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwarmLab.Editor
{
    [CustomPropertyDrawer(typeof(SteeringRule), true)]
    public class SteeringRuleDrawer : PropertyDrawer
    {
        private const float RowHeight = 20f;
        private const float HeaderHeight = 22f;
        private const float Padding = 5f;

        // 1. Calculate the dynamic height of the drawer
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.managedReferenceValue == null) 
                return EditorGUIUtility.singleLineHeight;

            // Start with height for the header/label
            float height = EditorGUIUtility.singleLineHeight + Padding;

            // Retrieve the list of species from the SwarmConfig
            var speciesList = GetSpeciesFromConfig(property.serializedObject);
            
            if (speciesList.Count > 0)
            {
                // Add height for the table header + one row per species
                height += HeaderHeight + (speciesList.Count * RowHeight);
            }
            else
            {
                // Fallback height for "No Species Found" message
                height += RowHeight;
            }

            return height;
        }

        // 2. Draw the Virtual Table
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // -- Draw the Label (e.g., "Alignment Rule") --
            Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
            if (property.managedReferenceValue == null)
            {
                EditorGUI.LabelField(labelRect, label);
                Rect buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);
                if (GUI.Button(buttonRect, "Add Rule...")) ShowTypeSelector(property);
                EditorGUI.EndProperty();
                return;
            }

            string typeName = property.managedReferenceValue.GetType().Name;
            EditorGUI.LabelField(labelRect, $"{label.text} ({typeName})", EditorStyles.boldLabel);

            // -- Prepare Data --
            SteeringRule rule = property.managedReferenceValue as SteeringRule;
            var availableSpecies = GetSpeciesFromConfig(property.serializedObject);
            
            // Move cursor down
            float currentY = position.y + EditorGUIUtility.singleLineHeight + Padding;

            // -- Draw Table Header --
            if (availableSpecies.Count > 0)
            {
                Rect headerRect = new Rect(position.x, currentY, position.width, HeaderHeight);
                DrawTableHeader(headerRect);
                currentY += HeaderHeight;

                // -- Draw Rows --
                EditorGUI.BeginChangeCheck();
                
                foreach (var speciesDef in availableSpecies)
                {
                    if (speciesDef == null) continue;

                    Rect rowRect = new Rect(position.x, currentY, position.width, RowHeight);
                    
                    // 1. Get current weight (0 if not found)
                    float currentWeight = GetCurrentWeight(rule, speciesDef);

                    // 2. Draw the row
                    float newWeight = DrawSpeciesRow(rowRect, speciesDef.name, currentWeight);

                    // 3. Update the rule if changed
                    if (!Mathf.Approximately(currentWeight, newWeight))
                    {
                        UpdateRuleWeight(rule, speciesDef, newWeight);
                    }

                    currentY += RowHeight;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    // Important: Write C# changes back to Unity serialization and mark dirty
                    rule.OnValidate(); // Rebuild internal cache
                    property.serializedObject.Update(); 
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                }
            }
            else
            {
                // Fallback if SwarmConfig is empty
                Rect infoRect = new Rect(position.x, currentY, position.width, RowHeight);
                EditorGUI.HelpBox(infoRect, "No Species defined in SwarmConfig.", MessageType.Info);
            }

            EditorGUI.EndProperty();
        }

        // --- Helpers ---

        private void DrawTableHeader(Rect rect)
        {
            // Simple background
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.2f));
            
            Rect col1 = new Rect(rect.x + 5, rect.y, rect.width * 0.6f, rect.height);
            Rect col2 = new Rect(rect.x + rect.width * 0.6f, rect.y, rect.width * 0.4f, rect.height);

            EditorGUI.LabelField(col1, "Target Species", EditorStyles.miniBoldLabel);
            EditorGUI.LabelField(col2, "Weight Impact", EditorStyles.miniBoldLabel);
        }

        private float DrawSpeciesRow(Rect rect, string name, float currentWeight)
        {
            Rect labelRect = new Rect(rect.x + 10, rect.y, rect.width * 0.6f - 10, rect.height);
            Rect valueRect = new Rect(rect.x + rect.width * 0.6f, rect.y + 2, rect.width * 0.35f, rect.height - 4);

            // Grey out label if weight is 0 (visual cue that it's inactive)
            if (currentWeight == 0) GUI.enabled = false;
            EditorGUI.LabelField(labelRect, name);
            GUI.enabled = true;

            return EditorGUI.FloatField(valueRect, currentWeight);
        }

        // Helper to find all SpeciesDefinition in the SwarmConfig
        private List<SpeciesDefinition> GetSpeciesFromConfig(SerializedObject so)
        {
            var results = new List<SpeciesDefinition>();
            SerializedProperty listProp = so.FindProperty("speciesConfigs");

            if (listProp != null && listProp.isArray)
            {
                for (int i = 0; i < listProp.arraySize; i++)
                {
                    var element = listProp.GetArrayElementAtIndex(i);
                    var defProp = element.FindPropertyRelative("speciesDefinition");
                    
                    if (defProp != null && defProp.objectReferenceValue is SpeciesDefinition def)
                    {
                        // Avoid duplicates in the list if user added same species twice in config
                        if (!results.Contains(def)) results.Add(def);
                    }
                }
            }
            return results;
        }

        private float GetCurrentWeight(SteeringRule rule, SpeciesDefinition species)
        {
            // Simple lookup in the raw list
            var entry = rule.speciesWeights.FirstOrDefault(sw => sw.species == species);
            return entry.species != null ? entry.weight : 0f;
        }

        private void UpdateRuleWeight(SteeringRule rule, SpeciesDefinition species, float newWeight)
        {
            // Remove existing entry (we will re-add if needed)
            rule.speciesWeights.RemoveAll(sw => sw.species == species);

            // If weight is non-zero, add it back
            // (Solution C logic: sparse data storage, dense UI)
            if (Mathf.Abs(newWeight) > 0.001f)
            {
                rule.speciesWeights.Add(new SpeciesWeight { species = species, weight = newWeight });
            }
        }
        
        // ... (Keep your ShowTypeSelector method here) ...
        private void ShowTypeSelector(SerializedProperty property)
        {
            var menu = new GenericMenu();
            var types = TypeCache.GetTypesDerivedFrom<SteeringRule>()
                .Where(t => !t.IsAbstract && !t.IsGenericType)
                .OrderBy(t => t.Name);

            foreach (var type in types)
            {
                menu.AddItem(new GUIContent(type.Name), false, () =>
                {
                    var instance = Activator.CreateInstance(type);
                    property.serializedObject.Update();
                    property.managedReferenceValue = instance;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }
    }
}