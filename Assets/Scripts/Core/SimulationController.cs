using UnityEngine;
using PlasmaSimulation.Data;
using PlasmaSimulation.Simulation;
using PlasmaSimulation.Interaction;
using PlasmaSimulation.Persistence;
using PlasmaSimulation.Rendering;

namespace PlasmaSimulation.Core
{
    public class SimulationController : MonoBehaviour
    {
        [Header("References")]
        public SimulationConfig Config;
        public Camera MainCamera;

        private PlasmaParticlePool _particlePool;
        private ElectromagneticFieldSolver _fieldSolver;
        private ParticleCollisionIonization _collisionIonization;
        private ParticleLifecycleManager _lifecycleManager;
        private SimulationSnapshotManager _snapshotManager;
        private MouseElectricFieldPainter _painter;
        private PlasmaParticleRenderer _particleRenderer;
        private ElectricFieldHeatmap _fieldHeatmap;

        [Header("Runtime State")]
        public bool IsPaused = false;
        public bool SingleStepRequested = false;
        public float SimulationTime = 0f;
        public float CurrentTimeStep;

        private float _fpsUpdateInterval = 0.5f;
        private float _fpsTimer = 0f;
        private int _fpsFrameCount = 0;
        private float _currentFps = 0f;

        public PlasmaParticlePool ParticlePool => _particlePool;
        public ElectromagneticFieldSolver FieldSolver => _fieldSolver;
        public SimulationSnapshotManager SnapshotManager => _snapshotManager;
        public float CurrentFPS => _currentFps;

        private void Awake()
        {
            if (Config == null)
            {
                Config = Resources.Load<SimulationConfig>("Configs/SimulationConfig");
                if (Config == null)
                {
                    Debug.LogWarning("SimulationConfig not found in Resources/Configs, using default.");
                    Config = ScriptableObject.CreateInstance<SimulationConfig>();
                }
            }

            InitializeModules();
        }

        private void InitializeModules()
        {
            GameObject modulesGO = new GameObject("SimulationModules");
            modulesGO.transform.SetParent(transform);

            _particlePool = modulesGO.AddComponent<PlasmaParticlePool>();
            _fieldSolver = modulesGO.AddComponent<ElectromagneticFieldSolver>();
            _collisionIonization = modulesGO.AddComponent<ParticleCollisionIonization>();
            _lifecycleManager = modulesGO.AddComponent<ParticleLifecycleManager>();
            _snapshotManager = modulesGO.AddComponent<SimulationSnapshotManager>();
            _painter = modulesGO.AddComponent<MouseElectricFieldPainter>();

            _particlePool.Initialize(Config);
            _fieldSolver.Initialize(Config);
            _collisionIonization.Initialize(Config, _particlePool, _fieldSolver);
            _lifecycleManager.Initialize(Config, _particlePool, _fieldSolver);
            _snapshotManager.Initialize(Config, _particlePool, _fieldSolver);
            _painter.Initialize(Config, _fieldSolver);

            CurrentTimeStep = Config.TimeStep;

            SetupRendering(modulesGO);
            SetupCamera();

            _particlePool.SpawnInitialParticles();
            _fieldSolver.SolveField();

            Debug.Log("Plasma Simulation initialized.");
            PrintControlsHelp();
        }

        private void SetupRendering(GameObject parent)
        {
            GameObject renderingGO = new GameObject("Rendering");
            renderingGO.transform.SetParent(parent.transform);

            _particleRenderer = renderingGO.AddComponent<PlasmaParticleRenderer>();
            _particleRenderer.Initialize(Config, _particlePool);

            _fieldHeatmap = renderingGO.AddComponent<ElectricFieldHeatmap>();
            _fieldHeatmap.Initialize(Config, _fieldSolver, MainCamera);
        }

        private void SetupCamera()
        {
            if (MainCamera == null)
            {
                MainCamera = Camera.main;
                if (MainCamera == null)
                {
                    GameObject camGO = new GameObject("Main Camera");
                    MainCamera = camGO.AddComponent<Camera>();
                    camGO.tag = "MainCamera";
                }
            }

            MainCamera.orthographic = true;
            float aspect = (float)Screen.width / Screen.height;
            MainCamera.orthographicSize = Config.SimulationSize.y * 0.5f;
            MainCamera.transform.position = new Vector3(0f, 0f, -10f);
            MainCamera.backgroundColor = Color.black;
            MainCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private void Update()
        {
            HandleSimulationInput();
            UpdateFPSCounter();

            if (!IsPaused || SingleStepRequested)
            {
                RunSimulationStep();
                SingleStepRequested = false;
            }
        }

        private void HandleSimulationInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                IsPaused = !IsPaused;
                Debug.Log(IsPaused ? "Simulation PAUSED" : "Simulation RESUMED");
            }

            if (Input.GetKeyDown(KeyCode.N) && IsPaused)
            {
                SingleStepRequested = true;
                Debug.Log("Single step executed");
            }

            if (Input.GetKey(KeyCode.UpArrow))
            {
                CurrentTimeStep = Mathf.Min(Config.MaxTimeStep, CurrentTimeStep * 1.1f);
                Debug.Log($"Time step increased to: {CurrentTimeStep:E2}");
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                CurrentTimeStep = Mathf.Max(Config.MinTimeStep, CurrentTimeStep * 0.9f);
                Debug.Log($"Time step decreased to: {CurrentTimeStep:E2}");
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                CurrentTimeStep = Config.TimeStep;
                Debug.Log($"Time step reset to: {CurrentTimeStep:E2}");
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                string path = _snapshotManager.SaveCurrentState(SimulationTime);
                Debug.Log($"Snapshot saved: {path}");
            }
            if (Input.GetKeyDown(KeyCode.F9))
            {
                var snapshots = _snapshotManager.GetAvailableSnapshots();
                if (snapshots.Count > 0)
                {
                    var snap = _snapshotManager.LoadSnapshotFromFile(snapshots[0]);
                    _snapshotManager.ApplySnapshot(snap, ref SimulationTime);
                }
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                _fieldSolver.SolveField();
                Debug.Log("Force field recalculation");
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                Config.EnableMagneticField = !Config.EnableMagneticField;
                _fieldSolver.SolveField();
                Debug.Log($"Magnetic field: {(Config.EnableMagneticField ? "ON" : "OFF")}");
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        private void RunSimulationStep()
        {
            SimulationTime += CurrentTimeStep;
            _collisionIonization.StepSimulation(CurrentTimeStep);
            _lifecycleManager.UpdateLifecycles(CurrentTimeStep);
        }

        private void UpdateFPSCounter()
        {
            _fpsTimer += Time.unscaledDeltaTime;
            _fpsFrameCount++;
            if (_fpsTimer >= _fpsUpdateInterval)
            {
                _currentFps = _fpsFrameCount / _fpsTimer;
                _fpsTimer = 0f;
                _fpsFrameCount = 0;
            }
        }

        private void PrintControlsHelp()
        {
            Debug.Log("=== PLASMA SIMULATION CONTROLS ===");
            Debug.Log("Left Mouse: Paint electrode/coil (hold to draw)");
            Debug.Log("Right Mouse: Remove electrode/coil");
            Debug.Log("Key 1: Positive electrode | Key 2: Negative electrode");
            Debug.Log("Key 3: Coil CCW current | Key 4: Coil CW current");
            Debug.Log("Key C: Clear current mode objects | Shift+C: Clear all");
            Debug.Log("Key M: Toggle magnetic field on/off");
            Debug.Log("Space: Pause/Resume | Key N: Single step");
            Debug.Log("Up/Down: Adjust time step | Key R: Reset time step");
            Debug.Log("Key F: Force field recalc");
            Debug.Log("F5: Save snapshot | F9: Load latest snapshot");
            Debug.Log("Esc: Exit");
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
        }

        public void RequestSingleStep()
        {
            if (IsPaused) SingleStepRequested = true;
        }

        public void SetTimeStep(float step)
        {
            CurrentTimeStep = Mathf.Clamp(step, Config.MinTimeStep, Config.MaxTimeStep);
        }

        public void ResetSimulation()
        {
            SimulationTime = 0f;
            _particlePool.RecycleAll();
            _particlePool.SpawnInitialParticles();
            _fieldSolver.ClearElectrodes();
            _fieldSolver.ClearMagneticCoils();
            _fieldSolver.ClearFields();
            _fieldSolver.SolveField();
            CurrentTimeStep = Config.TimeStep;
            IsPaused = false;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 320, 260));
            GUILayout.BeginVertical("box");
            GUILayout.Label($"FPS: {_currentFps:F1}", GetLabelStyle());
            GUILayout.Label($"Time: {SimulationTime:F4}s", GetLabelStyle());
            GUILayout.Label($"Timestep: {CurrentTimeStep:E2}", GetLabelStyle());
            GUILayout.Label($"Particles: {_particlePool.ActiveCount}/{Config.MaxParticles}", GetLabelStyle());
            GUILayout.Label($"  Electrons: {_lifecycleManager.ElectronCount}", GetLabelStyle());
            GUILayout.Label($"  Ions: {_lifecycleManager.IonCount}", GetLabelStyle());
            GUILayout.Label($"  Neutrals: {_lifecycleManager.NeutralCount}", GetLabelStyle());
            GUILayout.Label($"Status: {(IsPaused ? "<color=yellow>PAUSED</color>" : "<color=green>RUNNING</color>")}", GetLabelStyle());

            if (_painter.CurrentPaintMode == Interaction.PaintMode.Electrode)
            {
                GUILayout.Label($"Mode: <color=cyan>Electrode</color> | {(_painter.CurrentChargeSign > 0 ? "<color=red>Positive</color>" : "<color=blue>Negative</color>")}", GetLabelStyle());
            }
            else
            {
                string dir = _painter.CurrentCoilCurrentSign > 0 ? "CCW (B out)" : "CW (B in)";
                GUILayout.Label($"Mode: <color=magenta>Magnetic Coil</color> | {dir}", GetLabelStyle());
            }

            int coilCount = _fieldSolver.MagneticSolver != null ? _fieldSolver.MagneticSolver.CoilCount : 0;
            int electrodeCount = _fieldSolver.Electrodes != null ? _fieldSolver.Electrodes.Count : 0;
            string magStatus = Config.EnableMagneticField ? "<color=green>ON</color>" : "<color=gray>OFF</color>";
            GUILayout.Label($"Magnetic Field: {magStatus} | Coils: {coilCount} | Electrodes: {electrodeCount}", GetLabelStyle());
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private GUIStyle GetLabelStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 12;
            style.richText = true;
            style.normal.textColor = Color.white;
            return style;
        }

        private void OnApplicationQuit()
        {
            if (_particleRenderer != null) _particleRenderer.Cleanup();
            if (_fieldHeatmap != null) _fieldHeatmap.Cleanup();
        }
    }
}
