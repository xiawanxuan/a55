using System.Runtime.InteropServices;
using UnityEngine;

namespace PlasmaSimulation.Data
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ParticleData
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Charge;
        public float Mass;
        public ParticleType Type;
        public int IsActive;
        public float Lifetime;
        public float Energy;

        public static ParticleData CreateDefault()
        {
            return new ParticleData
            {
                Position = Vector2.zero,
                Velocity = Vector2.zero,
                Charge = 0f,
                Mass = 1f,
                Type = ParticleType.Neutral,
                IsActive = 0,
                Lifetime = 0f,
                Energy = 0f
            };
        }

        public static ParticleData CreateElectron(Vector2 position, Vector2 velocity)
        {
            return new ParticleData
            {
                Position = position,
                Velocity = velocity,
                Charge = -1.602e-19f,
                Mass = 9.109e-31f,
                Type = ParticleType.Electron,
                IsActive = 1,
                Lifetime = 0f,
                Energy = 0.5f * 9.109e-31f * velocity.sqrMagnitude
            };
        }

        public static ParticleData CreateIon(Vector2 position, Vector2 velocity)
        {
            return new ParticleData
            {
                Position = position,
                Velocity = velocity,
                Charge = 1.602e-19f,
                Mass = 1.673e-27f,
                Type = ParticleType.Ion,
                IsActive = 1,
                Lifetime = 0f,
                Energy = 0.5f * 1.673e-27f * velocity.sqrMagnitude
            };
        }

        public static ParticleData CreateNeutral(Vector2 position, Vector2 velocity)
        {
            return new ParticleData
            {
                Position = position,
                Velocity = velocity,
                Charge = 0f,
                Mass = 1.673e-27f,
                Type = ParticleType.Neutral,
                IsActive = 1,
                Lifetime = 0f,
                Energy = 0.5f * 1.673e-27f * velocity.sqrMagnitude
            };
        }
    }
}
