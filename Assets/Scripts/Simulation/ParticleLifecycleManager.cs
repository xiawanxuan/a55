using System.Collections.Generic;
using UnityEngine;
using PlasmaSimulation.Data;

namespace PlasmaSimulation.Simulation
{
    public class ParticleLifecycleManager : MonoBehaviour
    {
        private SimulationConfig _config;
        private PlasmaParticlePool _particlePool;
        private ElectromagneticFieldSolver _fieldSolver;
        private List<int> _toRecycle;
        public float MaxParticleLifetime = 30f;
        public float ParticleRecombinationProbability = 0.0001f;
        public int ElectronCount { get; private set; }
        public int IonCount { get; private set; }
        public int NeutralCount { get; private set; }

        public void Initialize(SimulationConfig config, PlasmaParticlePool particlePool, ElectromagneticFieldSolver fieldSolver)
        {
            _config = config;
            _particlePool = particlePool;
            _fieldSolver = fieldSolver;
            _toRecycle = new List<int>(config.MaxParticles);
        }

        public void UpdateLifecycles(float deltaTime)
        {
            _toRecycle.Clear();
            ElectronCount = 0;
            IonCount = 0;
            NeutralCount = 0;

            var particles = _particlePool.Particles;
            var activeIndices = _particlePool.ActiveIndices;

            for (int idx = 0; idx < activeIndices.Count; idx++)
            {
                int i = activeIndices[idx];
                ParticleData p = particles[i];
                if (p.IsActive == 0) continue;

                switch (p.Type)
                {
                    case ParticleType.Electron: ElectronCount++; break;
                    case ParticleType.Ion: IonCount++; break;
                    case ParticleType.Neutral: NeutralCount++; break;
                }

                if (p.Lifetime > MaxParticleLifetime)
                {
                    _toRecycle.Add(i);
                    continue;
                }

                CheckBoundaryEscape(ref p, i);
                particles[i] = p;
            }

            for (int i = 0; i < _toRecycle.Count; i++)
            {
                _particlePool.Recycle(_toRecycle[i]);
            }

            CheckRecombination();
        }

        private void CheckBoundaryEscape(ref ParticleData p, int index)
        {
            Vector2 halfSize = _config.SimulationSize * 0.5f;
            float margin = 0.5f;

            if (p.Position.x < -halfSize.x - margin ||
                p.Position.x > halfSize.x + margin ||
                p.Position.y < -halfSize.y - margin ||
                p.Position.y > halfSize.y + margin)
            {
                _toRecycle.Add(index);
            }
        }

        private void CheckRecombination()
        {
            if (_particlePool.ActiveCount < 10) return;

            var particles = _particlePool.Particles;
            var activeIndices = _particlePool.ActiveIndices;
            float recombDist = _config.CollisionRadius * 0.5f;
            float recombDistSq = recombDist * recombDist;
            List<int> electrons = new List<int>();
            List<int> ions = new List<int>();

            for (int idx = 0; idx < activeIndices.Count; idx++)
            {
                int i = activeIndices[idx];
                if (particles[i].IsActive == 0) continue;
                if (particles[i].Type == ParticleType.Electron) electrons.Add(i);
                else if (particles[i].Type == ParticleType.Ion) ions.Add(i);
            }

            int maxChecks = Mathf.Min(electrons.Count, ions.Count) / 10;
            for (int c = 0; c < maxChecks; c++)
            {
                if (Random.value > ParticleRecombinationProbability) continue;

                int ei = electrons[Random.Range(0, electrons.Count)];
                int ii = ions[Random.Range(0, ions.Count)];

                if (particles[ei].IsActive == 0 || particles[ii].IsActive == 0) continue;

                float distSq = (particles[ei].Position - particles[ii].Position).sqrMagnitude;
                if (distSq < recombDistSq)
                {
                    Vector2 neutralPos = (particles[ei].Position + particles[ii].Position) * 0.5f;
                    Vector2 neutralVel = (particles[ei].Velocity * particles[ei].Mass + particles[ii].Velocity * particles[ii].Mass) / (particles[ei].Mass + particles[ii].Mass);
                    _particlePool.Recycle(ei);
                    _particlePool.Recycle(ii);
                    _particlePool.SpawnNeutral(neutralPos, neutralVel);
                }
            }
        }

        public void ResetStatistics()
        {
            ElectronCount = 0;
            IonCount = 0;
            NeutralCount = 0;
        }
    }
}
