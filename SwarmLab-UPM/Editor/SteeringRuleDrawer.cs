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
        private const float ROW_HEIGHT = 20f;
        private const float HEADER_HEIGHT = 22f;
        private const float PADDING = 6f; 
        private const float FIELD_SPACING = 2f; // Space between standard fields

        // --- 1. Calculate Dynamic Height ---
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.managedReferenceValue == null) 
                return EditorGUIUtility.singleLineHeight;

            // Start with Header height
            float height = EditorGUIUtility.singleLineHeight + PADDING;

            // A. Calculate height of Standard Fields (minDistance, maxForce, etc.)
            SerializedProperty iterator = property.Copy();
            SerializedProperty endProp = iterator.GetEndProperty();

            // Enter the children of the class
            if (iterator.NextVisible(true)) 
            {
                do
                {
                    if (SerializedProperty.EqualContents(iterator, endProp)) break;

                    // Skip the 'speciesWeights' list because we draw that manually later
                    if (iterator.name == "speciesWeights") continue;

                    // Add the height of this specific field (e.g. float, Vector3, etc.)
                    height += EditorGUI.GetPropertyHeight(iterator, true) + FIELD_SPACING;
                }
                while (iterator.NextVisible(false));
            }

            // B. Calculate height of the Species Table
            height += PADDING; // Space before table
            var speciesList = GetSpeciesFromConfig(property.serializedObject);
            
            if (speciesList.Count > 0)
                height += HEADER_HEIGHT + (speciesList.Count * ROW_HEIGHT);
            else
                height += ROW_HEIGHT;

            return height;
        }

        // --- 2. Draw the Inspector ---
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            // Handle Null Case (New Rule Button)
            if (property.managedReferenceValue == null)
            {
                EditorGUI.LabelField(currentRect, label);
                Rect buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);
                if (GUI.Button(buttonRect, "Add Rule...")) ShowTypeSelector(property);
                EditorGUI.EndProperty();
                return;
            }

            // Draw Rule Title
            string typeName = property.managedReferenceValue.GetType().Name;
            EditorGUI.LabelField(currentRect, $"{label.text} ({typeName})", EditorStyles.boldLabel);
            currentRect.y += EditorGUIUtility.singleLineHeight + PADDING;

            // --- A. DRAW STANDARD FIELDS ---
            // Iterate through properties again to Draw them
            SerializedProperty iterator = property.Copy();
            SerializedProperty endProp = iterator.GetEndProperty();

            if (iterator.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iterator, endProp)) break;
                    
                    // Skip the table list
                    if (iterator.name == "speciesWeights") continue;

                    float h = EditorGUI.GetPropertyHeight(iterator, true);
                    currentRect.height = h;

                    // Draw the field (e.g. minDistance)
                    EditorGUI.PropertyField(currentRect, iterator, true);
                    
                    currentRect.y += h + FIELD_SPACING;
                }
                while (iterator.NextVisible(false));
            }

            // --- B. DRAW SPECIES TABLE ---
            currentRect.y += PADDING; 
            
            SteeringRule rule = property.managedReferenceValue as SteeringRule;
            var availableSpecies = GetSpeciesFromConfig(property.serializedObject);
            
            if (availableSpecies.Count > 0)
            {
                // Draw Header
                Rect headerRect = new Rect(position.x, currentRect.y, position.width, HEADER_HEIGHT);
                DrawTableHeader(headerRect);
                currentRect.y += HEADER_HEIGHT;

                // Draw Rows
                EditorGUI.BeginChangeCheck();
                
                foreach (var speciesDef in availableSpecies)
                {
                    if (speciesDef == null) continue;

                    Rect rowRect = new Rect(position.x, currentRect.y, position.width, ROW_HEIGHT);
                    
                    float currentWeight = GetCurrentWeight(rule, speciesDef);
                    float newWeight = DrawSpeciesRow(rowRect, speciesDef.name, currentWeight);

                    if (!Mathf.Approximately(currentWeight, newWeight))
                    {
                        UpdateRuleWeight(rule, speciesDef, newWeight);
                    }

                    currentRect.y += ROW_HEIGHT;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    rule.OnValidate(); 
                    property.serializedObject.Update(); 
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                }
            }
            else
            {
                Rect infoRect = new Rect(position.x, currentRect.y, position.width, ROW_HEIGHT);
                EditorGUI.HelpBox(infoRect, "No Species defined in SwarmConfig.", MessageType.Info);
            }

            EditorGUI.EndProperty();
        }

        // --- Helpers ---

        private void DrawTableHeader(Rect rect)
        {
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

            if (currentWeight == 0) GUI.enabled = false;
            EditorGUI.LabelField(labelRect, name);
            GUI.enabled = true;

            return EditorGUI.FloatField(valueRect, currentWeight);
        }

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
                        if (!results.Contains(def)) results.Add(def);
                    }
                }
            }
            return results;
        }

        private float GetCurrentWeight(SteeringRule rule, SpeciesDefinition species)
        {
            var entry = rule.speciesWeights.FirstOrDefault(sw => sw.species == species);
            return entry.species != null ? entry.weight : 0f;
        }

        private void UpdateRuleWeight(SteeringRule rule, SpeciesDefinition species, float newWeight)
        {
            rule.speciesWeights.RemoveAll(sw => sw.species == species);
            if (Mathf.Abs(newWeight) > 0.001f)
            {
                rule.speciesWeights.Add(new SpeciesWeight { species = species, weight = newWeight });
            }
        }
        
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