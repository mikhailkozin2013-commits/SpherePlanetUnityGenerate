using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlanetTerrainGenerator))]
public class PlanetEditorBrush : Editor
{
    private bool isEditMode = false;
    private float brushRadius = 5f;
    private float brushStrength = 2f;
    private bool isDigMode = false; 

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlanetTerrainGenerator generator = (PlanetTerrainGenerator)target;

        EditorGUILayout.Space(15);
        GUILayout.Label("Инструменты Терраформинга (Окно Scene)", EditorStyles.boldLabel);

        string btnText = isEditMode ? "ВЫКЛЮЧИТЬ КИСТЬ" : "ВКЛЮЧИТЬ КИСТЬ РЕДАКТОРА";
        GUI.backgroundColor = isEditMode ? Color.green : Color.red;
        
        if (GUILayout.Button(btnText, GUILayout.Height(35)))
        {
            isEditMode = !isEditMode;
            if (!isEditMode) Tools.current = Tool.Move;
        }
        GUI.backgroundColor = Color.white;

        if (isEditMode)
        {
            EditorGUILayout.Space(5);
            brushRadius = EditorGUILayout.Slider("Радиус кисти", brushRadius, 1f, 20f);
            brushStrength = EditorGUILayout.Slider("Сила выдавливания", brushStrength, 0.1f, 10f);
            
            EditorGUILayout.Space(5);
            string modeText = isDigMode ? "Режим: КОПАТЬ ЯМЫ (ЛКМ)" : "Режим: СТРОИТЬ ГОРЫ (ЛКМ)";
            if (GUILayout.Button(modeText, GUILayout.Height(25)))
            {
                isDigMode = !isDigMode;
            }
            EditorGUILayout.HelpBox("Удерживайте ЛКМ на планете в окне Scene для изменения рельефа.\nШвы теперь сшиваются автоматически.", MessageType.Info);
        }
    }

    private void OnSceneGUI()
    {
        PlanetTerrainGenerator generator = (PlanetTerrainGenerator)target;
        
        if (!isEditMode) return;

        Tools.current = Tool.None;
        Event currentEvent = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            PlanetChunkData chunk = hit.collider.GetComponent<PlanetChunkData>();
            if (chunk != null)
            {
                if (Selection.activeGameObject != generator.gameObject)
                {
                    Selection.activeGameObject = generator.gameObject;
                }

                Handles.color = isDigMode ? Color.red : Color.cyan;
                Vector3 normal = (hit.point - generator.transform.position).normalized;
                
                Handles.DrawWireDisc(hit.point, normal, brushRadius);
                Handles.color = isDigMode ? new Color(1, 0, 0, 0.05f) : new Color(0, 1, 1, 0.05f);
                Handles.DrawSolidDisc(hit.point, normal, brushRadius);

                if (currentEvent.type == EventType.MouseMove && SceneView.currentDrawingSceneView != null)
                {
                    SceneView.currentDrawingSceneView.Repaint();
                }

                if ((currentEvent.type == EventType.MouseDrag || currentEvent.type == EventType.MouseDown) && currentEvent.button == 0)
                {
                    // Регистрируем изменения для Undo (Ctrl+Z) для всей планеты целиком
                    Undo.RecordObjects(generator.GetComponentsInChildren<MeshFilter>(), "Planet Global Terraform");

                    // ВЫЗЫВАЕМ СИНХРОННУЮ ДЕФОРМАЦИЮ ВСЕХ ЧАНКОВ
                    generator.GlobalDeformPlanet(hit.point, brushRadius, brushStrength * 0.1f, isDigMode);

                    currentEvent.Use();
                }
            }
        }

        if (currentEvent.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
    }
}
