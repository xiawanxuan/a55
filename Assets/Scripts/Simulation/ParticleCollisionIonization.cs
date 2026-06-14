using System.Collections.Generic;
using UnityEngine;
using PlasmaSimulation.Data;

namespace PlasmaSimulation.Simulation
{
    public class ParticleCollisionIonization : MonoBehaviour
    {
        private SimulationConfig _config;
        private PlasmaParticlePool _particlePool;
        private ElectromagneticFieldSolver _fieldSolver;
        private Dictionary<long, List<int>> _spatialHash;
        private float _hashCellSize;

        public int IonizationCount { get; private set; }
        public int CollisionCount { get; private set; }

        public void Initialize(SimulationConfig config, PlasmaParticlePool particlePool, ElectromagneticFieldSolver fieldSolver)
        {
            _config = config;
            _particlePool = particlePool;
            _fieldSolver = fieldSolver;
            _hashCellSize = config.CollisionRadius * 2f;
            _spatialHash = new Dictionary<long, List<int>>();
        }

        public void StepSimulation(float deltaTime)
        {
            IonizationCount = 0;
            CollisionCount = 0;
            UpdateParticleDynamics(deltaTime);
            if (_config.UseSpatialHash)
            {
                BuildSpatialHash();
                DetectCollisionsSpatial();
            }
            else
            {
                DetectCollisionsBruteForce();
            }
        }

        private void UpdateParticleDynamics(float deltaTime)
        {
            var particles = _particlePool.Particles;
            var activeIndices = _particlePool.ActiveIndices;
            Vector2 halfSize = _config.SimulationSize * 0.5f;
            float maxSpeed = 1e7f;
            float boundaryEscapeMargin = _config.CollisionRadius * 2f;
            List<int> toRecycle = null;

            for (int idx = 0; idx < activeIndices.Count; idx++)
            {
                int i = activeIndices[idx];
                ParticleData p = particles[i];
                if (p.IsActive == 0) continue;

                if (p.Position.x < -halfSize.x - boundaryEscapeMargin ||
                    p.Position.x > halfSize.x + boundaryEscapeMargin ||
                    p.Position.y < -halfSize.y - boundaryEscapeMargin ||
                    p.Position.y > halfSize.y + boundaryEscapeMargin)
                {
                    if (toRecycle == null) toRecycle = new List<int>();
                    toRecycle.Add(i);
                    continue;
                }

                Vector2 eField = _fieldSolver.GetElectricFieldAt(p.Position);
                Vector2 force = p.Charge * eField;
                Vector2 acceleration = force / p.Mass;
                p.Velocity += acceleration * deltaTime;

                float speed = p.Velocity.magnitude;
                if (speed > maxSpeed)
                {
                    p.Velocity = p.Velocity.normalized * maxSpeed;
                }

                p.Position += p.Velocity * deltaTime;
                p.Lifetime += deltaTime;
                p.Energy = 0.5f * p.Mass * p.Velocity.sqrMagnitude;

                if (p.Position.x < -halfSize.x)
                {
                    p.Position.x = -halfSize.x;
                    p.Velocity.x = Mathf.Abs(p.Velocity.x) * _config.CollisionDamping;
                }
                else if (p.Position.x > halfSize.x)
                {
                    p.Position.x = halfSize.x;
                    p.Velocity.x = -Mathf.Abs(p.Velocity.x) * _config.CollisionDamping;
                }

                if (p.Position.y < -halfSize.y)
                {
                    p.Position.y = -halfSize.y;
                    p.Velocity.y = Mathf.Abs(p.Velocity.y) * _config.CollisionDamping;
                }
                else if (p.Position.y > halfSize.y)
                {
                    p.Position.y = halfSize.y;
                    p.Velocity.y = -Mathf.Abs(p.Velocity.y) * _config.CollisionDamping;
                }

                particles[i] = p;
            }

            if (toRecycle != null)
            {
                for (int r = 0; r < toRecycle.Count; r++)
                {
                    _particlePool.Recycle(toRecycle[r]);
                }
            }
        }

        private void BuildSpatialHash()
        {
            _spatialHash.Clear();
            var particles = _particlePool.Particles;
            var activeIndices = _particlePool.ActiveIndices;

            for (int idx = 0; idx < activeIndices.Count; idx++)
            {
                int i = activeIndices[idx];
                ParticleData p = particles[i];
                if (p.IsActive == 0) continue;

                long key = GetHashKey(p.Position);
                if (!_spatialHash.ContainsKey(key))
                {
                    _spatialHash[key] = new List<int>();
                }
                _spatialHash[key].Add(i);
            }
        }

        private long GetHashKey(Vector2 position)
        {
            int x = Mathf.FloorToInt(position.x / _hashCellSize);
            int y = Mathf.FloorToInt(position.y / _hashCellSize);
            return ((long)x << 32) | (uint)y;
        }

        private void DetectCollisionsSpatial()
        {
            var particles = _particlePool.Particles;
            var activeIndices = _particlePool.ActiveIndices;
            float collisionDistSq = _config.CollisionRadius * _config.CollisionRadius;
            HashSet<long> processedPairs = new HashSet<long>();

            for (int idx = 0; idx < activeIndices.Count; idx++)
            {
                int i = activeIndices[idx];
                ParticleData p = particles[i];
                if (p.IsActive == 0) continue;

                int cx = Mathf.FloorToInt(p.Position.x / _hashCellSize);
                int cy = Mathf.FloorToInt(p.Position.y / _hashCellSize);

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        long key = ((long)(cx + dx) << 32) | (uint)(cy + dy);
                        List<int> bucket;
                        if (_spatialHash.TryGetValue(key, out bucket))
                        {
                            for (int b = 0; b < bucket.Count; b++)
                            {
                                int j = bucket[b];
                                if (j <= i) continue;

                                long pairKey = ((long)i << 32) | (uint)j;
                                if (processedPairs.Contains(pairKey)) continue;
                                processedPairs.Add(pairKey);

                                ProcessCollision(i, j, collisionDistSq);
                            }
                        }
                    }
                }
            }
        }

        private void DetectCollisionsBruteForce()
        {
            var particles = _particlePool.Particles;
            var activeIndices = _particlePool.ActiveIndices;
            float collisionDistSq = _config.CollisionRadius * _config.CollisionRadius;

            for (int a = 0; a < activeIndices.Count; a++)
            {
                int i = activeIndices[a];
                ParticleData pi = particles[i];
                if (pi.IsActive == 0) continue;

                for (int b = a + 1; b < activeIndices.Count; b++)
                {
                    int j = activeIndices[b];
                    ProcessCollision(i, j, collisionDistSq);
                }
            }
        }

        private void ProcessCollision(int i, int j, float collisionDistSq)
        {
            var particles = _particlePool.Particles;
            ParticleData pi = particles[i];
            ParticleData pj = particles[j];
            if (pi.IsActive == 0 || pj.IsActive == 0) return;

            Vector2 delta = pj.Position - pi.Position;
            float distSq = delta.sqrMagnitude;

            if (distSq < collisionDistSq && distSq > 0f)
            {
                CollisionCount++;
                HandleParticleCollision(i, j, ref pi, ref pj, delta, distSq);
            }
        }

        private void HandleParticleCollision(int i, int j, ref ParticleData pi, ref ParticleData pj, Vector2 delta, float distSq)
        {
            var particles = _particlePool.Particles;

            if (pi.Type == ParticleType.Electron && pj.Type == ParticleType.Neutral)
            {
                TryIonizeElectronNeutral(i, j, ref pi, ref pj);
            }
            else if (pj.Type == ParticleType.Electron && pi.Type == ParticleType.Neutral)
            {
                TryIonizeElectronNeutral(j, i, ref pj, ref pi);
            }
            else
            {
                ResolveElasticCollision(ref pi, ref pj, delta, distSq);
            }

            particles[i] = pi;
            particles[j] = pj;
        }

        private void TryIonizeElectronNeutral(int electronIdx, int neutralIdx, ref ParticleData electron, ref ParticleData neutral)
        {
            Vector2 halfSize = _config.SimulationSize * 0.5f;
            float boundaryMargin = _config.CollisionRadius;
            if (electron.Position.x < -halfSize.x + boundaryMargin ||
                electron.Position.x > halfSize.x - boundaryMargin ||
                electron.Position.y < -halfSize.y + boundaryMargin ||
                electron.Position.y > halfSize.y - boundaryMargin ||
                neutral.Position.x < -halfSize.x + boundaryMargin ||
                neutral.Position.x > halfSize.x - boundaryMargin ||
                neutral.Position.y < -halfSize.y + boundaryMargin ||
                neutral.Position.y > halfSize.y - boundaryMargin)
            {
                return;
            }

            float relativeEnergy = 0.5f * electron.Mass * electron.Velocity.sqrMagnitude;

            if (relativeEnergy > _config.IonizationEnergyThreshold && Random.value < _config.IonizationProbability)
            {
                IonizationCount++;
                Vector2 ionPos = neutral.Position;
                Vector2 ionVel = neutral.Velocity * 0.5f;
                Vector2 newElectronPos = neutral.Position + Random.insideUnitCircle * _config.CollisionRadius * 0.5f;
                Vector2 newElectronVel = Random.insideUnitCircle * electron.Velocity.magnitude * 0.3f;

                _particlePool.Recycle(neutralIdx);
                _particlePool.SpawnIon(ionPos, ionVel);
                _particlePool.SpawnElectron(newElectronPos, newElectronVel);

                electron.Velocity *= 0.7f;
            }
            else
            {
                float dist = Mathf.Sqrt((neutral.Position - electron.Position).sqrMagnitude);
                Vector2 delta = neutral.Position - electron.Position;
                ResolveElasticCollision(ref electron, ref neutral, delta, dist * dist);
            }
        }

        private void ResolveElasticCollision(ref ParticleData a, ref ParticleData b, Vector2 delta, float distSq)
        {
            float dist = Mathf.Sqrt(distSq);
            Vector2 normal = delta / dist;
            Vector2 tangent = new Vector2(-normal.y, normal.x);

            float relVelAlongNormal = Vector2.Dot(b.Velocity - a.Velocity, normal);
            if (relVelAlongNormal > 0f) return;

            float m1 = a.Mass;
            float m2 = b.Mass;
            float totalMass = m1 + m2;

            float v1n = Vector2.Dot(a.Velocity, normal);
            float v1t = Vector2.Dot(a.Velocity, tangent);
            float v2n = Vector2.Dot(b.Velocity, normal);
            float v2t = Vector2.Dot(b.Velocity, tangent);

            float v1nAfter = (v1n * (m1 - m2) + 2f * m2 * v2n) / totalMass;
            float v2nAfter = (v2n * (m2 - m1) + 2f * m1 * v1n) / totalMass;

            a.Velocity = (normal * v1nAfter + tangent * v1t) * _config.CollisionDamping;
            b.Velocity = (normal * v2nAfter + tangent * v2t) * _config.CollisionDamping;

            float overlap = _config.CollisionRadius - dist;
            if (overlap > 0f)
            {
                Vector2 separation = normal * (overlap * 0.5f);
                a.Position -= separation;
                b.Position += separation;
            }
        }
    }
}
