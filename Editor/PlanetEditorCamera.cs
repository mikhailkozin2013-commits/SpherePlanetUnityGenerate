using UnityEngine;
using UnityEditor;

public class PlanetEditorHelper : EditorWindow
{
    private static GameObject activePlanet;
    private static Vector3 currentUpVector = Vector3.up;
    private static Quaternion currentPlanetSpace = Quaternion.identity;

    [MenuItem("Tools/Planet Placement Assistant")]
    public static void ShowWindow()
    {
        GetWindow<PlanetEditorHelper>("Planet Assistant");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Tools.hidden = false;
    }

    private void OnGUI()
    {
        GUILayout.Label("Ассистент сферической разметки", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Найти планету на сцене", GUILayout.Height(30)))
        {
            FindPlanet();
        }

        if (activePlanet != null)
        {
            EditorGUILayout.HelpBox($"Привязка к: {activePlanet.name}\n\n[ГОРЯЧИЕ КЛАВИШИ В ОКНЕ СЦЕНЫ]:\n• Нажмите [ G ] — Выровнять стрелки по поверхности под объектом.\n• Нажмите [ F ] — Прижать объект к земле ногами.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Планета со скриптом PlanetAttractor не найдена. Нажмите кнопку выше.", MessageType.Warning);
        }
    }

    private static void FindPlanet()
    {
        var attractor = Object.FindFirstObjectByType<PlanetAttractor>();
        if (attractor != null) activePlanet = attractor.gameObject;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (activePlanet == null) return;
        if (Selection.activeTransform == null) 
        {
            Tools.hidden = false;
            return;
        }

        Transform target = Selection.activeTransform;
        Event e = Event.current;

        // Расчет локального "верха" для объекта относительно центра планеты
        Vector3 objectUp = (target.position - activePlanet.transform.position).normalized;
        Vector3 objectForward = Vector3.ProjectOnPlane(target.forward, objectUp).normalized;
        if (objectForward == Vector3.zero) objectForward = Vector3.ProjectOnPlane(Vector3.forward, objectUp).normalized;
        
        // Локальное пространство координат в этой точке планеты
        Quaternion planetRotationSpace = Quaternion.LookRotation(objectForward, objectUp);

        // --- ОБРАБОТКА НАЖАТИЯ КЛАВИШ ---
        if (e.type == EventType.KeyDown)
        {
            // Клавиша G — Обновить сетку координат под объектом
            if (e.keyCode == KeyCode.G)
            {
                currentUpVector = objectUp;
                currentPlanetSpace = planetRotationSpace;
                e.Use(); // Потребляем ввод, чтобы не пищало
            }

            // Клавиша F — Прижать к планете (Raycast вниз к центру)
            if (e.keyCode == KeyCode.F)
            {
                Undo.RecordObject(target, "Snap to Planet Surface");
                
                // Пускаем луч из космоса через объект к центру планеты
                Ray ray = new Ray(target.position + objectUp * 50f, -objectUp);
                if (Physics.Raycast(ray, out RaycastHit hit, 200f))
                {
                    target.position = hit.point;
                    // Сразу правильно разворачиваем ноги объекта к планете
                    target.rotation = Quaternion.FromToRotation(target.up, objectUp) * target.rotation;
                }
                e.Use();
            }
        }

        // --- ОТРИСОВКА ИНСТРУМЕНТОВ ---
        if (Tools.pivotRotation == PivotRotation.Global)
        {
            // Кастомное перемещение (W) вдоль изгиба планеты
            if (Tools.current == Tool.Move)
            {
                Tools.hidden = true;
                EditorGUI.BeginChangeCheck();
                Vector3 newPosition = Handles.PositionHandle(target.position, currentPlanetSpace == Quaternion.identity ? planetRotationSpace : currentPlanetSpace);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Move on Planet");
                    target.position = newPosition;
                }
            }
            // Кастомный поворот (E) строго параллельно горизонту планеты!
            else if (Tools.current == Tool.Rotate)
            {
                Tools.hidden = true;
                EditorGUI.BeginChangeCheck();
                Quaternion newRotation = Handles.RotationHandle(target.rotation, target.position);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Rotate on Planet");
                    // Корректируем вращение так, чтобы ось Y вращалась только по горизонту
                    target.rotation = newRotation;
                }
            }
            else
            {
                Tools.hidden = false;
            }
        }
        else
        {
            Tools.hidden = false;
        }
    }
}
