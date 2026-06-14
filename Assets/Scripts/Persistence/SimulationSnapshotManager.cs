using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PlasmaSimulation.Data;
using PlasmaSimulation.Simulation;

namespace PlasmaSimulation.Persistence
{
    public class SimulationSnapshotManager : MonoBehaviour
    {
        private SimulationConfig _config;
        private PlasmaParticlePool _particlePool;
        private ElectromagneticFieldSolver _fieldSolver;
        private string _snapshotDirectory;

        public void Initialize(SimulationConfig config, PlasmaParticlePool particlePool, ElectromagneticFieldSolver fieldSolver)
        {
            _config = config;
            _particlePool = particlePool;
            _fieldSolver = fieldSolver;
            _snapshotDirectory = Path.Combine(Application.persistentDataPath, "PlasmaSnapshots");
            if (!Directory.Exists(_snapshotDirectory))
            {
                Directory.CreateDirectory(_snapshotDirectory);
            }
        }

        public SimulationSnapshot CaptureSnapshot(float simulationTime)
        {
            SimulationSnapshot snapshot = new SimulationSnapshot
            {
                Timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"),
                SimulationTime = simulationTime,
                ActiveParticleCount = _particlePool.ActiveCount,
                TimeStep = _config.TimeStep,
                SimulationSize = _config.SimulationSize,
                Particles = _particlePool.GetActiveParticlesList(),
                Electrodes = new List<ElectrodeData>(_fieldSolver.Electrodes)
            };

            int nx = _fieldSolver.GridSizeX;
            int ny = _fieldSolver.GridSizeY;
            snapshot.PotentialField = new float[nx, ny];
            snapshot.ElectricField = new Vector2[nx, ny];

            System.Array.Copy(_fieldSolver.PotentialField, snapshot.PotentialField, nx * ny);
            System.Array.Copy(_fieldSolver.ElectricField, snapshot.ElectricField, nx * ny);

            return snapshot;
        }

        public string SaveSnapshotToFile(SimulationSnapshot snapshot)
        {
            string filePath = Path.Combine(_snapshotDirectory, $"snapshot_{snapshot.Timestamp}.json");
            SnapshotFileWrapper wrapper = new SnapshotFileWrapper(snapshot);
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"Snapshot saved to: {filePath}");
            return filePath;
        }

        public string SaveCurrentState(float simulationTime)
        {
            SimulationSnapshot snapshot = CaptureSnapshot(simulationTime);
            return SaveSnapshotToFile(snapshot);
        }

        public SimulationSnapshot LoadSnapshotFromFile(string fileName)
        {
            string filePath = Path.Combine(_snapshotDirectory, fileName);
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Snapshot file not found: {filePath}");
                return null;
            }
            string json = File.ReadAllText(filePath);
            SnapshotFileWrapper wrapper = JsonUtility.FromJson<SnapshotFileWrapper>(json);
            return wrapper.Snapshot;
        }

        public List<string> GetAvailableSnapshots()
        {
            if (!Directory.Exists(_snapshotDirectory)) return new List<string>();
            string[] files = Directory.GetFiles(_snapshotDirectory, "*.json");
            List<string> result = new List<string>(files.Length);
            for (int i = 0; i < files.Length; i++)
            {
                result.Add(Path.GetFileName(files[i]));
            }
            result.Sort();
            result.Reverse();
            return result;
        }

        public void ApplySnapshot(SimulationSnapshot snapshot, ref float simulationTime)
        {
            if (snapshot == null)
            {
                Debug.LogError("Cannot apply null snapshot");
                return;
            }

            simulationTime = snapshot.SimulationTime;
            _config.TimeStep = snapshot.TimeStep;

            _particlePool.LoadParticlesFromList(snapshot.Particles);
            _fieldSolver.LoadState(snapshot.PotentialField, snapshot.ElectricField, snapshot.Electrodes);

            Debug.Log($"Snapshot applied: {snapshot.Timestamp}, Particles: {snapshot.ActiveParticleCount}");
        }

        public bool DeleteSnapshot(string fileName)
        {
            string filePath = Path.Combine(_snapshotDirectory, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }

        public string SnapshotDirectory => _snapshotDirectory;
    }
}
