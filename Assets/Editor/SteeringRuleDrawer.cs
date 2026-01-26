using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using SwarmLab;

namespace SwarmLab.Editor
{
    [CustomPropertyDrawer(typeof(SteeringRule), true)]
    public class SteeringRuleDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // If the element is null, we only need one line for the dropdown
            if (property.managedReferenceValue == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }
            
            // Otherwise draw the normal property height
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.managedReferenceValue == null)
            {
                // Draw the label
                Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
                EditorGUI.LabelField(labelRect, label);

                // Draw the "Select Type" button
                Rect buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);
                if (GUI.Button(buttonRect, "Add Rule... (Select Type)", EditorStyles.popup))
                {
                    ShowTypeSelector(property);
                }
            }
            else
            {
                // Optionally append the type name to the label for clarity
                string typeName = property.managedReferenceValue.GetType().Name;
                label.text = $"{label.text} ({typeName})";
                
                // Draw the property
                EditorGUI.PropertyField(position, property, label, true);
            }

            EditorGUI.EndProperty();
        }

        private void ShowTypeSelector(SerializedProperty property)
        {
            var menu = new GenericMenu();
            
            // Find all non-abstract classes that inherit from SteeringRule
            var types = TypeCache.GetTypesDerivedFrom<SteeringRule>()
                .Where(t => !t.IsAbstract && !t.IsGenericType)
                .OrderBy(t => t.Name);

            if (!types.Any())
            {
                menu.AddDisabledItem(new GUIContent("No classes inherit from SteeringRule"));
            }

            foreach (var type in types)
            {
                // Use the type name as the menu path
                menu.AddItem(new GUIContent(type.Name), false, () =>
                {
                    // Create instance and assign it
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
