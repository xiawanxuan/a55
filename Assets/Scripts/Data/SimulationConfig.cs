using UnityEngine;

namespace PlasmaSimulation.Data
{
    [CreateAssetMenu(fileName = "SimulationConfig", menuName = "PlasmaSimulation/Simulation Config", order = 0)]
    public class SimulationConfig : ScriptableObject
    {
        [Header("Simulation Area")]
        public Vector2 SimulationSize = new Vector2(20f, 12f);
        public int FieldGridResolutionX = 200;
        public int FieldGridResolutionY = 120;

        [Header("Time Settings")]
        public float TimeStep = 1e-5f;
        public float MinTimeStep = 1e-7f;
        public float MaxTimeStep = 1e-3f;

        [Header("Particle Pool")]
        public int MaxParticles = 100000;
        public int InitialElectrons = 5000;
        public int InitialIons = 5000;
        public int InitialNeutrals = 20000;

        [Header("Physical Constants")]
        public float CoulombConstant = 8.988e9f;
        public float VacuumPermittivity = 8.854e-12f;
        public float VacuumPermeability = 1.25663706e-6f;
        public float BoltzmannConstant = 1.381e-23f;
        public float ElectronCharge = -1.602e-19f;

        [Header("Collision & Ionization")]
        public float CollisionRadius = 0.05f;
        public float IonizationEnergyThreshold = 2.179e-18f;
        public float IonizationProbability = 0.3f;
        public float CollisionDamping = 0.98f;

        [Header("Electrode Settings")]
        public float ElectrodeDefaultCharge = 1e-6f;
        public float ElectrodeDefaultRadius = 0.5f;
        public float ElectrodeBrushSize = 0.3f;

        [Header("Magnetic Coil Settings")]
        public float CoilDefaultCurrent = 100f;
        public float CoilDefaultRadius = 1.5f;
        public int CoilDefaultTurns = 10;
        public float CoilBrushSize = 0.5f;
        public float LorentzForceScale = 1e15f;
        [Range(0f, 1f)]
        public float MagneticFieldOpacity = 0.4f;
        public Color CoilPositiveColor = new Color(0.8f, 0.2f, 0.8f, 1f);
        public Color CoilNegativeColor = new Color(0.2f, 0.8f, 0.8f, 1f);
        public bool EnableMagneticField = true;

        [Header("Rendering")]
        public Color ElectronColor = new Color(0.2f, 0.5f, 1f, 1f);
        public Color IonColor = new Color(1f, 0.3f, 0.2f, 1f);
        public Color NeutralColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        public float ParticleSize = 0.05f;
        [Range(0f, 1f)]
        public float HeatmapOpacity = 0.5f;
        public float HeatmapScale = 1e7f;

        [Header("Performance")]
        public int CollisionDetectionBatchSize = 1000;
        public int FieldSolveIterations = 50;
        public bool UseSpatialHash = true;
    }
}
