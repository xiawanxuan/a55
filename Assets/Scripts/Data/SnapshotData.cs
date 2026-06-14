using System.Collections.Generic;
using UnityEngine;

namespace PlasmaSimulation.Data
{
    [System.Serializable]
    public class SimulationSnapshot
    {
        public string Timestamp;
        public float SimulationTime;
        public int ActiveParticleCount;
        public List<ParticleData> Particles;
        public List<ElectrodeData> Electrodes;
        public float[,] PotentialField;
        public Vector2[,] ElectricField;
        public float TimeStep;
        public Vector2 SimulationSize;

        public SimulationSnapshot()
        {
            Particles = new List<ParticleData>();
            Electrodes = new List<ElectrodeData>();
        }
    }

    [System.Serializable]
    public class SnapshotFileWrapper
    {
        public SimulationSnapshot Snapshot;

        public SnapshotFileWrapper(SimulationSnapshot snapshot)
        {
            Snapshot = snapshot;
        }
    }
}
