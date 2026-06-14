using UnityEngine;

namespace PlasmaSimulation.Core
{
    public class SceneBootstrap : MonoBehaviour
    {
        private static bool _initialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoBootstrap()
        {
            if (_initialized) return;
            _initialized = true;

            GameObject root = new GameObject("[PlasmaSimulationRoot]");
            Object.DontDestroyOnLoad(root);

            SimulationController controller = root.AddComponent<SimulationController>();

            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                cam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }
            controller.MainCamera = cam;
        }

        private void Awake()
        {
            if (!_initialized)
            {
                _initialized = true;
                GameObject root = gameObject;

                if (root.GetComponent<SimulationController>() == null)
                {
                    SimulationController controller = root.AddComponent<SimulationController>();
                    Camera cam = Camera.main;
                    if (cam == null)
                    {
                        GameObject camGO = new GameObject("Main Camera");
                        camGO.tag = "MainCamera";
                        cam = camGO.AddComponent<Camera>();
                        camGO.AddComponent<AudioListener>();
                    }
                    controller.MainCamera = cam;
                }
            }
        }
    }
}
