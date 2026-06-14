using UnityEngine;

namespace PlasmaSimulation.Data
{
    [System.Serializable]
    public struct ElectrodeData
    {
        public Vector2 Position;
        public float Charge;
        public float Radius;

        public ElectrodeData(Vector2 position, float charge, float radius)
        {
            Position = position;
            Charge = charge;
            Radius = radius;
        }
    }
}
