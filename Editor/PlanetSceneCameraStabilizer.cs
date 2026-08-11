using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class PlanetSceneCameraStabilizer
{
    private static GameObject activePlanet;
    private static bool isStabilizerActive = false;
    private const string MenuPath = "Tools/Planet Editor Mode";

    static PlanetSceneCameraStabilizer()
    {
        // Подключаемся к циклу обновления окна Сцены
        SceneView.duringSceneGui += OnSceneGUI;
    }

    [MenuItem(MenuPath)]
    private static void ToggleMode()
    {
        isStabilizerActive = !isStabilizerActive;
        Menu.SetChecked(MenuPath, isStabilizerActive);

        if (isStabilizerActive)
        {
            FindPlanetInScene();
        }
    }

    private static void FindPlanetInScene()
    {
        var generator = Object.FindFirstObjectByType<PlanetTerrainGenerator>();
        if (generator != null)
        {
            activePlanet = generator.gameObject;
            Debug.Log($"<color=green>[PlanetCamera]</color> Стабилизатор включен. Слежение за: {activePlanet.name}");
        }
        else
        {
            Debug.LogWarning("[PlanetCamera] На сцене не найден объект со скриптом PlanetTerrainGenerator!");
            isStabilizerActive = false;
            Menu.SetChecked(MenuPath, false);
        }
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!isStabilizerActive) return;

        if (activePlanet == null)
        {
            FindPlanetInScene();
            if (activePlanet == null) return;
        }

        // 1. Находим точку на поверхности планеты строго под камерой редактора
        Vector3 camPosition = sceneView.camera.transform.position;
        Vector3 planetCenter = activePlanet.transform.position;

        // Направление от центра планеты к камере (это локальный вектор UP для этой зоны)
        Vector3 localUp = (camPosition - planetCenter).normalized;

        // Ищем физическую поверхность планеты под камерой с помощью Raycast
        Vector3 surfacePoint = planetCenter + localUp * 10f; // Дефолтная точка, если луч не попал
        Ray ray = new Ray(camPosition, -localUp);
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            surfacePoint = hit.point;
        }
        else
        {
            // Если камера улетела слишком высоко или смотрит мимо, считаем поверхность по базовому радиусу
            var generator = activePlanet.GetComponent<PlanetTerrainGenerator>();
            float baseRadius = generator != null ? generator.radius : 50f;
            surfacePoint = planetCenter + localUp * baseRadius;
        }

        // 2. Стабилизируем крен (Roll) камеры, сохраняя свободный обзор разработчика
        // Извлекаем текущие углы Pitch (вверх/вниз) и Yaw (влево/вправо), которые задает мышь пользователя
        Vector3 currentEuler = sceneView.rotation.eulerAngles;
        float pitch = currentEuler.x;
        float yaw = currentEuler.y;

        // Магия: Строим кватернион, где "ноги" выровнены по локальной поверхности, 
        // но углы обзора мыши (pitch/yaw) остаются полностью под контролем разработчика
        Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, localUp) * Quaternion.Euler(pitch, yaw, 0f);

        // Применяем вращение только если дельта существенна (чтобы избежать микро-тиков при полной остановке)
        if (Quaternion.Angle(sceneView.rotation, targetRotation) > 0.05f)
        {
            sceneView.rotation = targetRotation;
            
            // Настраиваем Pivot (точку фокуса камеры) на поверхность прямо под нами.
            // Это решает проблему вращения: теперь камера будет крутиться вокруг земли под собой, а не вокруг центра мира.
            sceneView.pivot = surfacePoint;
            
            // Принудительно просим Unity обновить кадр в окне Scene
            sceneView.Repaint();
        }
    }
}
