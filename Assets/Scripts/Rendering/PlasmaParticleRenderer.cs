using UnityEngine;
using PlasmaSimulation.Data;
using PlasmaSimulation.Simulation;

namespace PlasmaSimulation.Rendering
{
    public class PlasmaParticleRenderer : MonoBehaviour
    {
        private SimulationConfig _config;
        private PlasmaParticlePool _particlePool;
        private Material _particleMaterial;
        private ComputeBuffer _positionBuffer;
        private ComputeBuffer _colorBuffer;
        private ComputeBuffer _argsBuffer;
        private Mesh _quadMesh;
        private Vector4[] _particleColors;

        private Bounds _renderBounds;

        public void Initialize(SimulationConfig config, PlasmaParticlePool particlePool)
        {
            _config = config;
            _particlePool = particlePool;

            CreateParticleMaterial();
            CreateBuffers();
            CreateQuadMesh();

            _particleColors = new Vector4[config.MaxParticles];
            _renderBounds = new Bounds(Vector3.zero, new Vector3(config.SimulationSize.x, config.SimulationSize.y, 100f));
        }

        private void CreateParticleMaterial()
        {
            Shader shader = Shader.Find("Plasma/ParticleRender");
            if (shader == null)
            {
                Debug.LogWarning("Plasma/ParticleRender shader not found, using fallback.");
                shader = Shader.Find("Standard");
            }
            _particleMaterial = new Material(shader);
            _particleMaterial.SetFloat("_ParticleSize", _config.ParticleSize);
        }

        private void CreateBuffers()
        {
            int maxParticles = _config.MaxParticles;

            _positionBuffer = new ComputeBuffer(maxParticles, sizeof(float) * 3, ComputeBufferType.Default);
            _colorBuffer = new ComputeBuffer(maxParticles, sizeof(float) * 4, ComputeBufferType.Default);
            _argsBuffer = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);

            uint[] args = new uint[4] { 6, 0, 0, 0 };
            _argsBuffer.SetData(args);
        }

        private void CreateQuadMesh()
        {
            _quadMesh = new Mesh();
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f)
            };
            int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };
            Vector2[] uvs = new Vector2[4]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            _quadMesh.vertices = vertices;
            _quadMesh.triangles = triangles;
            _quadMesh.uv = uvs;
            _quadMesh.RecalculateBounds();
        }

        private void Update()
        {
            if (_particlePool == null || _config == null) return;
            UpdateParticleData();
            RenderParticles();
        }

        private void UpdateParticleData()
        {
            var particles = _particlePool.Particles;
            var activeIndices = _particlePool.ActiveIndices;
            int activeCount = _particlePool.ActiveCount;

            Vector3[] positions = new Vector3[activeCount];
            Vector4[] colors = new Vector4[activeCount];

            Color electronCol = _config.ElectronColor;
            Color ionCol = _config.IonColor;
            Color neutralCol = _config.NeutralColor;

            for (int idx = 0; idx < activeCount; idx++)
            {
                int i = activeIndices[idx];
                ParticleData p = particles[i];

                positions[idx] = new Vector3(p.Position.x, p.Position.y, 0f);

                switch (p.Type)
                {
                    case ParticleType.Electron:
                        colors[idx] = new Vector4(electronCol.r, electronCol.g, electronCol.b, electronCol.a);
                        break;
                    case ParticleType.Ion:
                        colors[idx] = new Vector4(ionCol.r, ionCol.g, ionCol.b, ionCol.a);
                        break;
                    case ParticleType.Neutral:
                    default:
                        colors[idx] = new Vector4(neutralCol.r, neutralCol.g, neutralCol.b, neutralCol.a);
                        break;
                }
            }

            if (activeCount > 0)
            {
                _positionBuffer.SetData(positions, 0, 0, activeCount);
                _colorBuffer.SetData(colors, 0, 0, activeCount);
            }

            uint[] args = new uint[4] { 6, (uint)activeCount, 0, 0 };
            _argsBuffer.SetData(args);
        }

        private void RenderParticles()
        {
            if (_particlePool.ActiveCount == 0) return;

            _particleMaterial.SetBuffer("_PositionBuffer", _positionBuffer);
            _particleMaterial.SetBuffer("_ColorBuffer", _colorBuffer);
            _particleMaterial.SetFloat("_ParticleSize", _config.ParticleSize);

            Graphics.DrawMeshInstancedIndirect(
                _quadMesh,
                0,
                _particleMaterial,
                _renderBounds,
                _argsBuffer,
                0,
                null,
                UnityEngine.Rendering.ShadowCastingMode.Off,
                false,
                0,
                null,
                UnityEngine.Rendering.LightProbeUsage.Off);
        }

        public void Cleanup()
        {
            if (_positionBuffer != null) { _positionBuffer.Release(); _positionBuffer = null; }
            if (_colorBuffer != null) { _colorBuffer.Release(); _colorBuffer = null; }
            if (_argsBuffer != null) { _argsBuffer.Release(); _argsBuffer = null; }
            if (_particleMaterial != null) { Destroy(_particleMaterial); _particleMaterial = null; }
            if (_quadMesh != null) { Destroy(_quadMesh); _quadMesh = null; }
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
