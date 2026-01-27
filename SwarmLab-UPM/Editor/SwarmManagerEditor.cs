using UnityEngine;
using UnityEditor;

namespace SwarmLab.Editor
{
    [CustomEditor(typeof(SwarmManager))]
    public class SwarmManagerEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor _configEditor;

        public override void OnInspectorGUI()
        {
            // 1. Synchronize data
            serializedObject.Update();

            // 2. Draw 'drawSpawnZones'
            SerializedProperty drawGizmosProp = serializedObject.FindProperty("drawSpawnZones");
            EditorGUILayout.PropertyField(drawGizmosProp);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Simulation Settings", EditorStyles.boldLabel);
            
            SerializedProperty simModeProp = serializedObject.FindProperty("simulationMode");
            EditorGUILayout.PropertyField(simModeProp);

            // Conditional display for Planar mode
            if (simModeProp.enumValueIndex == (int)SwarmManager.SimulationMode.Planar)
            {
                SerializedProperty planarBoundProp = serializedObject.FindProperty("planarBoundary");
                SerializedProperty planarSizeProp = serializedObject.FindProperty("planarSize");

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(planarBoundProp);
                EditorGUILayout.PropertyField(planarSizeProp);
                
                // Helper button to create plane if missing
                if (planarBoundProp.objectReferenceValue == null)
                {
                    if (GUILayout.Button("Create Helper Plane"))
                    {
                        ((SwarmManager)target).CreatePlane();
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Swarm Logic", EditorStyles.boldLabel);

            // 3. Draw 'swarmConfig'
            SerializedProperty configProp = serializedObject.FindProperty("swarmConfig");
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(configProp);
            if (EditorGUI.EndChangeCheck())
            {
                // If config changed, clear the cached editor so it rebuilds next frame
                if (_configEditor != null) DestroyImmediate(_configEditor);
            }

            // 4. Apply changes to the Manager itself
            serializedObject.ApplyModifiedProperties();

            // 5. Draw the Embedded Inspector (The "Inner" Editor)
            if (configProp.objectReferenceValue != null)
            {
                DrawEmbeddedConfigInspector(configProp.objectReferenceValue);
                
                EditorGUILayout.Space(10);
                DrawGenerationButtons();
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a Swarm Config to begin.", MessageType.Info);
            }
        }

        private void OnDisable()
        {
            if (_configEditor != null) DestroyImmediate(_configEditor);
        }

        private void DrawEmbeddedConfigInspector(Object configObject)
        {
            if (configObject == null) return;

            EditorGUILayout.Space();
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            EditorGUILayout.Space();

            // Safety: Ensure we don't hold onto an editor with a destroyed target
            if (_configEditor != null && _configEditor.target == null)
            {
                DestroyImmediate(_configEditor);
                _configEditor = null;
            }

            CreateCachedEditor(configObject, null, ref _configEditor);
            
            // Double check creation succeeded
            if (_configEditor != null)
            {
                _configEditor.OnInspectorGUI();
            }
        }

        private void DrawGenerationButtons()
        {
            SwarmManager manager = (SwarmManager)target;

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Clear Swarm", GUILayout.Height(30)))
            {
                manager.ClearSwarm();
            }

            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            
            if (GUILayout.Button("Generate Swarm", GUILayout.Height(30)))
            {
                manager.GenerateSwarm();
            }
            
            GUI.backgroundColor = oldColor;
            EditorGUILayout.EndHorizontal();
        }

        private void OnSceneGUI()
        {
            SwarmManager manager = (SwarmManager)target;
            if (manager.Config == null) return;

            // --- CHECK FOR THE BOOL BEFORE DRAWING HANDLES ---
            // We need to read the bool from the serialized object or the manager directly
            // Accessing via manager is faster/easier here since we aren't modifying it
            SerializedProperty drawGizmosProp = serializedObject.FindProperty("drawSpawnZones");
            if (!drawGizmosProp.boolValue) return;

            // --- CHECK SIMULATION MODE ---
            // If we are in Planar mode, don't draw the Spherical Spawn Handles
            SerializedProperty simModeProp = serializedObject.FindProperty("simulationMode");
            if (simModeProp.enumValueIndex == (int)SwarmManager.SimulationMode.Planar) return;

            // Create a SerializedObject for the Config Asset
            SerializedObject configSO = new SerializedObject(manager.Config);
            configSO.Update();

            SerializedProperty listProp = configSO.FindProperty("speciesConfigs");

            for (int i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty element = listProp.GetArrayElementAtIndex(i);
                
                SerializedProperty offsetProp = element.FindPropertyRelative("spawnOffset");
                SerializedProperty radiusProp = element.FindPropertyRelative("spawnRadius");
                SerializedProperty defProp = element.FindPropertyRelative("speciesDefinition");

                string speciesName = defProp.objectReferenceValue != null ? defProp.objectReferenceValue.name : $"Species {i}";
                Color speciesColor = Color.HSVToRGB((speciesName.GetHashCode() * 0.13f) % 1f, 1f, 1f);

                Vector3 worldCenter = manager.transform.TransformPoint(offsetProp.vector3Value);

                // Draw Position Handle
                EditorGUI.BeginChangeCheck();
                Handles.color = speciesColor;
                Handles.Label(worldCenter + Vector3.up * (radiusProp.floatValue + 1f), speciesName, EditorStyles.boldLabel);
                Vector3 newWorldCenter = Handles.PositionHandle(worldCenter, manager.transform.rotation);

                if (EditorGUI.EndChangeCheck())
                {
                    offsetProp.vector3Value = manager.transform.InverseTransformPoint(newWorldCenter);
                }

                // Draw Radius Handle
                EditorGUI.BeginChangeCheck();
                float newRadius = Handles.RadiusHandle(manager.transform.rotation, worldCenter, radiusProp.floatValue);
                
                if (EditorGUI.EndChangeCheck())
                {
                    radiusProp.floatValue = Mathf.Max(0.1f, newRadius);
                }
            }

            if (configSO.hasModifiedProperties)
            {
                configSO.ApplyModifiedProperties();
            }
        }
    }
}