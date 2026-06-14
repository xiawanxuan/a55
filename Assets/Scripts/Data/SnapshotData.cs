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
        public List<MagneticCoilData> MagneticCoils;
        public float[,] PotentialField;
        public Vector2[,] ElectricField;
        public float[,] MagneticFieldZ;
        public float TimeStep;
        public Vector2 SimulationSize;
        public bool MagneticFieldEnabled;

        public SimulationSnapshot()
        {
            Particles = new List<ParticleData>();
            Electrodes = new List<ElectrodeData>();
            MagneticCoils = new List<MagneticCoilData>();
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
