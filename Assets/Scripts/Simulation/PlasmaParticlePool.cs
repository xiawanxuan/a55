using System.Collections.Generic;
using UnityEngine;
using PlasmaSimulation.Data;

namespace PlasmaSimulation.Simulation
{
    public class PlasmaParticlePool : MonoBehaviour
    {
        private SimulationConfig _config;
        private ParticleData[] _particles;
        private Queue<int> _freeIndices;
        private List<int> _activeIndices;
        private int _activeCount;

        public ParticleData[] Particles => _particles;
        public List<int> ActiveIndices => _activeIndices;
        public int ActiveCount => _activeCount;
        public int MaxCount => _config != null ? _config.MaxParticles : 0;

        public void Initialize(SimulationConfig config)
        {
            _config = config;
            _particles = new ParticleData[config.MaxParticles];
            _freeIndices = new Queue<int>(config.MaxParticles);
            _activeIndices = new List<int>(config.MaxParticles);
            _activeCount = 0;

            for (int i = 0; i < config.MaxParticles; i++)
            {
                _particles[i] = ParticleData.CreateDefault();
                _freeIndices.Enqueue(i);
            }
        }

        public int SpawnElectron(Vector2 position, Vector2 velocity)
        {
            if (_freeIndices.Count == 0) return -1;
            int index = _freeIndices.Dequeue();
            _particles[index] = ParticleData.CreateElectron(position, velocity);
            _activeIndices.Add(index);
            _activeCount++;
            return index;
        }

        public int SpawnIon(Vector2 position, Vector2 velocity)
        {
            if (_freeIndices.Count == 0) return -1;
            int index = _freeIndices.Dequeue();
            _particles[index] = ParticleData.CreateIon(position, velocity);
            _activeIndices.Add(index);
            _activeCount++;
            return index;
        }

        public int SpawnNeutral(Vector2 position, Vector2 velocity)
        {
            if (_freeIndices.Count == 0) return -1;
            int index = _freeIndices.Dequeue();
            _particles[index] = ParticleData.CreateNeutral(position, velocity);
            _activeIndices.Add(index);
            _activeCount++;
            return index;
        }

        public int SpawnParticle(ParticleType type, Vector2 position, Vector2 velocity)
        {
            switch (type)
            {
                case ParticleType.Electron: return SpawnElectron(position, velocity);
                case ParticleType.Ion: return SpawnIon(position, velocity);
                case ParticleType.Neutral: return SpawnNeutral(position, velocity);
                default: return -1;
            }
        }

        public void Recycle(int index)
        {
            if (index < 0 || index >= _particles.Length) return;
            if (_particles[index].IsActive == 0) return;

            _particles[index].IsActive = 0;
            _particles[index].Lifetime = 0f;
            _freeIndices.Enqueue(index);
            _activeIndices.Remove(index);
            _activeCount--;
        }

        public void RecycleAll()
        {
            for (int i = 0; i < _activeIndices.Count; i++)
            {
                int idx = _activeIndices[i];
                _particles[idx].IsActive = 0;
                _particles[idx].Lifetime = 0f;
                _freeIndices.Enqueue(idx);
            }
            _activeIndices.Clear();
            _activeCount = 0;
        }

        public void SetParticleData(int index, ParticleData data)
        {
            if (index < 0 || index >= _particles.Length) return;
            _particles[index] = data;
        }

        public ParticleData GetParticleData(int index)
        {
            if (index < 0 || index >= _particles.Length) return ParticleData.CreateDefault();
            return _particles[index];
        }

        public void SpawnInitialParticles()
        {
            Vector2 halfSize = _config.SimulationSize * 0.5f;
            float thermalVelocity = Mathf.Sqrt(2f * _config.BoltzmannConstant * 300f / 9.109e-31f);

            for (int i = 0; i < _config.InitialElectrons; i++)
            {
                Vector2 pos = new Vector2(
                    Random.Range(-halfSize.x, halfSize.x),
                    Random.Range(-halfSize.y, halfSize.y));
                Vector2 vel = Random.insideUnitCircle * thermalVelocity * 0.1f;
                SpawnElectron(pos, vel);
            }

            float ionThermalVelocity = Mathf.Sqrt(2f * _config.BoltzmannConstant * 300f / 1.673e-27f);
            for (int i = 0; i < _config.InitialIons; i++)
            {
                Vector2 pos = new Vector2(
                    Random.Range(-halfSize.x, halfSize.x),
                    Random.Range(-halfSize.y, halfSize.y));
                Vector2 vel = Random.insideUnitCircle * ionThermalVelocity * 0.1f;
                SpawnIon(pos, vel);
            }

            for (int i = 0; i < _config.InitialNeutrals; i++)
            {
                Vector2 pos = new Vector2(
                    Random.Range(-halfSize.x, halfSize.x),
                    Random.Range(-halfSize.y, halfSize.y));
                Vector2 vel = Random.insideUnitCircle * ionThermalVelocity * 0.05f;
                SpawnNeutral(pos, vel);
            }
        }

        public List<ParticleData> GetActiveParticlesList()
        {
            List<ParticleData> result = new List<ParticleData>(_activeCount);
            for (int i = 0; i < _activeIndices.Count; i++)
            {
                result.Add(_particles[_activeIndices[i]]);
            }
            return result;
        }

        public void LoadParticlesFromList(List<ParticleData> particleList)
        {
            RecycleAll();
            for (int i = 0; i < particleList.Count && _freeIndices.Count > 0; i++)
            {
                ParticleData p = particleList[i];
                if (p.IsActive == 0) continue;
                int index = _freeIndices.Dequeue();
                _particles[index] = p;
                _activeIndices.Add(index);
                _activeCount++;
            }
        }
    }
}
