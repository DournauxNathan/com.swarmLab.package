using UnityEngine;
using UnityEditor;
using System.Linq;
using SwarmLab.Core;

namespace SwarmLab.Editor
{
    [CustomPropertyDrawer(typeof(SteeringRule), true)]
    public class SteeringRuleDrawer : PropertyDrawer
    {
        private const float ROW_HEIGHT = 20f;
        private const float HEADER_HEIGHT = 22f;
        private const float PADDING = 8f;
        private const float RULE_PADDING = 16f;

        // How much width the Species Name gets (30%)
        private const float NAME_WIDTH_PCT = 0.3f; 

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.managedReferenceValue == null) return EditorGUIUtility.singleLineHeight;

            float height = EditorGUIUtility.singleLineHeight + PADDING;

            // 1. Standard Fields (Global Rule Settings)
            var iterator = property.Copy();
            var endProp = iterator.GetEndProperty();
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iterator, endProp)) break;
                    if (iterator.name == "speciesParams") continue; // Skip the list
                    height += EditorGUI.GetPropertyHeight(iterator, true) + 2f;
                } while (iterator.NextVisible(false));
            }

            // 2. The Table Height
            SerializedProperty listProp = property.FindPropertyRelative("speciesParams");
            if (listProp != null && listProp.isArray)
            {
                int count = listProp.arraySize;
                // Header + Rows
                height += PADDING + HEADER_HEIGHT + (count * ROW_HEIGHT);
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight + RULE_PADDING);

            // Handle "Add Rule" Button
            if (property.managedReferenceValue == null)
            {
                EditorGUI.LabelField(currentRect, label);
                Rect btnRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);
                if (GUI.Button(btnRect, "Add Rule...")) ShowTypeSelector(property);
                EditorGUI.EndProperty();
                return;
            }

            // Draw Rule Title
            string typeName = property.managedReferenceValue.GetType().Name;
            EditorGUI.LabelField(currentRect, $"{typeName}", EditorStyles.boldLabel);
            currentRect.y += EditorGUIUtility.singleLineHeight + PADDING;

            // --- 1. Draw Global Fields (ruleWeight, etc) ---
            var iterator = property.Copy();
            var endProp = iterator.GetEndProperty();
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iterator, endProp)) break;
                    if (iterator.name == "speciesParams") continue;

                    float h = EditorGUI.GetPropertyHeight(iterator, true);
                    currentRect.height = h;
                    EditorGUI.PropertyField(currentRect, iterator, true);
                    currentRect.y += h + 2f;
                } while (iterator.NextVisible(false));
            }

            // --- 2. Draw Dynamic Table ---
            SerializedProperty listProp = property.FindPropertyRelative("speciesParams");
            if (listProp != null)
            {
                currentRect.y += PADDING;
                
                // A. Sync List (Ensure rows match the Config)
                var rule = property.managedReferenceValue as SteeringRule;
                var config = GetConfigFromObject(property.serializedObject);
                if (rule != null && config != null) 
                {
                    rule.SyncSpeciesList(config.speciesConfigs.ConvertAll(s => s.speciesDefinition));
                }

                // B. Draw Header
                Rect headerRect = new Rect(position.x, currentRect.y, position.width, HEADER_HEIGHT);
                DrawDynamicHeader(headerRect, listProp);
                currentRect.y += HEADER_HEIGHT;

                // C. Draw Rows
                for (int i = 0; i < listProp.arraySize; i++)
                {
                    SerializedProperty element = listProp.GetArrayElementAtIndex(i);
                    Rect rowRect = new Rect(position.x, currentRect.y, position.width, ROW_HEIGHT);
                    
                    DrawDynamicRow(rowRect, element);
                    currentRect.y += ROW_HEIGHT;
                }
            }

            EditorGUI.EndProperty();
        }

        // --- DYNAMIC COLUMNS LOGIC ---

        private void DrawDynamicRow(Rect rect, SerializedProperty element)
        {
            // 1. Calculate Layout
            float nameWidth = rect.width * NAME_WIDTH_PCT;
            float remainingWidth = rect.width * (1f - NAME_WIDTH_PCT);
            
            // Count parameters (subtract 1 for the 'species' field)
            int paramCount = CountVisibleChildren(element) ;
            if (paramCount < 1) paramCount = 1;

            // Calculate exact width per parameter
            float paramWidth = remainingWidth / paramCount;

            // 2. Draw Species Name
            SerializedProperty speciesProp = element.FindPropertyRelative("species");
            string labelName = (speciesProp.objectReferenceValue != null) ? speciesProp.objectReferenceValue.name : "Null";
            
            Rect nameRect = new Rect(rect.x, rect.y, nameWidth, rect.height);
            EditorGUI.LabelField(nameRect, labelName, EditorStyles.miniLabel);

            // 3. Draw Parameters
            float currentX = rect.x + nameWidth;
            var iter = element.Copy();
            var end = iter.GetEndProperty();
            
            if (iter.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iter, end)) break;
                    if (iter.name == "species") continue; // Skip the key

                    // Draw field in its calculated slot
                    Rect propRect = new Rect(currentX, rect.y + 1, paramWidth - 4, rect.height - 2);
                    EditorGUI.PropertyField(propRect, iter, GUIContent.none);
                    
                    currentX += paramWidth;
                } while (iter.NextVisible(false));
            }
        }

        private void DrawDynamicHeader(Rect rect, SerializedProperty listProp)
        {
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.2f));
            if (listProp.arraySize == 0) return;

            SerializedProperty element = listProp.GetArrayElementAtIndex(0);

            // 1. Layout
            float nameWidth = rect.width * NAME_WIDTH_PCT;
            float remainingWidth = rect.width * (1f - NAME_WIDTH_PCT);
            
            int paramCount = CountVisibleChildren(element);
            if (paramCount < 1) paramCount = 1;
            float paramWidth = remainingWidth / paramCount;

            // 2. Species Header
            EditorGUI.LabelField(new Rect(rect.x + 5, rect.y, nameWidth, rect.height), "Species", EditorStyles.miniBoldLabel);

            // 3. Parameter Headers
            float currentX = rect.x + nameWidth;
            var iter = element.Copy();
            var end = iter.GetEndProperty();

            if (iter.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iter, end)) break;
                    if (iter.name == "species") continue;

                    // Use variable name as header (e.g. "Weight", "Radius")
                    EditorGUI.LabelField(new Rect(currentX, rect.y, paramWidth, rect.height), iter.displayName, EditorStyles.miniBoldLabel);
                    currentX += paramWidth;
                } while (iter.NextVisible(false));
            }
        }

        private int CountVisibleChildren(SerializedProperty prop)
        {
            int count = 0;
            var iter = prop.Copy();
            var end = iter.GetEndProperty();
            if (iter.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iter, end)) break;
                    count++;
                } while (iter.NextVisible(false));
            }
            return count;
        }

        // --- HELPERS ---

        private SwarmConfig GetConfigFromObject(SerializedObject so)
        {
            // If we are editing the Config directly
            if (so.targetObject is SwarmConfig config) return config;
            
            // If we are editing the Manager (embedded inspector)
            if (so.targetObject is SwarmManager manager) return manager.Config;

            return null;
        }

        private void ShowTypeSelector(SerializedProperty property)
        {
            // (Same TypeSelector logic as before...)
            var menu = new GenericMenu();
            var types = TypeCache.GetTypesDerivedFrom<SteeringRule>()
                .Where(t => !t.IsAbstract && !t.IsGenericType)
                .OrderBy(t => t.Name);

            foreach (var type in types)
            {
                menu.AddItem(new GUIContent(type.Name), false, () =>
                {
                    var instance = System.Activator.CreateInstance(type);
                    property.serializedObject.Update();
                    property.managedReferenceValue = instance;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }
    }
}