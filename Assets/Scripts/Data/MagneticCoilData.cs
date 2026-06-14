using UnityEngine;

namespace PlasmaSimulation.Data
{
    [System.Serializable]
    public struct MagneticCoilData
    {
        public Vector2 Position;
        public float Radius;
        public float Current;
        public int Turns;

        public MagneticCoilData(Vector2 position, float radius, float current, int turns = 1)
        {
            Position = position;
            Radius = radius;
            Current = current;
            Turns = turns;
        }
    }
}
