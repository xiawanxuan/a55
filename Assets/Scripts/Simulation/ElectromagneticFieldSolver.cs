using System.Collections.Generic;
using UnityEngine;
using PlasmaSimulation.Data;

namespace PlasmaSimulation.Simulation
{
    public class ElectromagneticFieldSolver : MonoBehaviour
    {
        private SimulationConfig _config;
        private float[,] _potentialField;
        private float[,] _chargeDensity;
        private Vector2[,] _electricField;
        private List<ElectrodeData> _electrodes;
        private MagneticFieldSolver _magneticSolver;
        private float _cellSizeX;
        private float _cellSizeY;

        public float[,] PotentialField => _potentialField;
        public Vector2[,] ElectricField => _electricField;
        public List<ElectrodeData> Electrodes => _electrodes;
        public MagneticFieldSolver MagneticSolver => _magneticSolver;
        public int GridSizeX => _config != null ? _config.FieldGridResolutionX : 0;
        public int GridSizeY => _config != null ? _config.FieldGridResolutionY : 0;

        public void Initialize(SimulationConfig config)
        {
            _config = config;
            _potentialField = new float[config.FieldGridResolutionX, config.FieldGridResolutionY];
            _chargeDensity = new float[config.FieldGridResolutionX, config.FieldGridResolutionY];
            _electricField = new Vector2[config.FieldGridResolutionX, config.FieldGridResolutionY];
            _electrodes = new List<ElectrodeData>();
            _cellSizeX = config.SimulationSize.x / config.FieldGridResolutionX;
            _cellSizeY = config.SimulationSize.y / config.FieldGridResolutionY;
            _magneticSolver = gameObject.GetComponent<MagneticFieldSolver>();
            if (_magneticSolver == null)
            {
                _magneticSolver = gameObject.AddComponent<MagneticFieldSolver>();
            }
            _magneticSolver.Initialize(config);
            ClearFields();
        }

        public void ClearFields()
        {
            int nx = _config.FieldGridResolutionX;
            int ny = _config.FieldGridResolutionY;
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    _potentialField[i, j] = 0f;
                    _chargeDensity[i, j] = 0f;
                    _electricField[i, j] = Vector2.zero;
                }
            }
            if (_magneticSolver != null)
            {
                _magneticSolver.ClearField();
            }
        }

        public void AddElectrode(Vector2 worldPosition, float charge, float radius)
        {
            _electrodes.Add(new ElectrodeData(worldPosition, charge, radius));
        }

        public void RemoveElectrodeAt(Vector2 worldPosition, float tolerance = 0.5f)
        {
            for (int i = _electrodes.Count - 1; i >= 0; i--)
            {
                if (Vector2.Distance(_electrodes[i].Position, worldPosition) < tolerance)
                {
                    _electrodes.RemoveAt(i);
                }
            }
        }

        public void ClearElectrodes()
        {
            _electrodes.Clear();
        }

        public void SolveField()
        {
            ComputeChargeDensity();
            SolvePoissonJacobi();
            ComputeElectricField();
            if (_magneticSolver != null && _config.EnableMagneticField)
            {
                _magneticSolver.SolveField();
            }
        }

        private void ComputeChargeDensity()
        {
            int nx = _config.FieldGridResolutionX;
            int ny = _config.FieldGridResolutionY;
            Vector2 halfSize = _config.SimulationSize * 0.5f;

            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    _chargeDensity[i, j] = 0f;
                }
            }

            for (int e = 0; e < _electrodes.Count; e++)
            {
                ElectrodeData electrode = _electrodes[e];
                int minX = Mathf.Max(0, WorldToGridX(electrode.Position.x - electrode.Radius));
                int maxX = Mathf.Min(nx - 1, WorldToGridX(electrode.Position.x + electrode.Radius));
                int minY = Mathf.Max(0, WorldToGridY(electrode.Position.y - electrode.Radius));
                int maxY = Mathf.Min(ny - 1, WorldToGridY(electrode.Position.y + electrode.Radius));
                float rSq = electrode.Radius * electrode.Radius;
                float area = Mathf.PI * rSq;
                float chargePerCell = electrode.Charge / (area / (_cellSizeX * _cellSizeY));

                for (int i = minX; i <= maxX; i++)
                {
                    for (int j = minY; j <= maxY; j++)
                    {
                        Vector2 cellPos = GridToWorld(i, j);
                        float distSq = (cellPos - electrode.Position).sqrMagnitude;
                        if (distSq < rSq)
                        {
                            _chargeDensity[i, j] += chargePerCell;
                        }
                    }
                }
            }
        }

        private void SolvePoissonJacobi()
        {
            int nx = _config.FieldGridResolutionX;
            int ny = _config.FieldGridResolutionY;
            int iterations = _config.FieldSolveIterations;
            float[,] temp = new float[nx, ny];
            float eps = _config.VacuumPermittivity;
            float hx2 = _cellSizeX * _cellSizeX;
            float hy2 = _cellSizeY * _cellSizeY;
            float denom = 2f / hx2 + 2f / hy2;

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int i = 0; i < nx; i++)
                {
                    for (int j = 0; j < ny; j++)
                    {
                        temp[i, j] = _potentialField[i, j];
                    }
                }

                for (int i = 1; i < nx - 1; i++)
                {
                    for (int j = 1; j < ny - 1; j++)
                    {
                        float lapX = (temp[i + 1, j] + temp[i - 1, j]) / hx2;
                        float lapY = (temp[i, j + 1] + temp[i, j - 1]) / hy2;
                        float rho = _chargeDensity[i, j] / eps;
                        _potentialField[i, j] = (lapX + lapY + rho) / denom;
                    }
                }

                for (int i = 0; i < nx; i++)
                {
                    _potentialField[i, 0] = _potentialField[i, 1];
                    _potentialField[i, ny - 1] = _potentialField[i, ny - 2];
                }
                for (int j = 0; j < ny; j++)
                {
                    _potentialField[0, j] = _potentialField[1, j];
                    _potentialField[nx - 1, j] = _potentialField[nx - 2, j];
                }
            }
        }

        private void ComputeElectricField()
        {
            int nx = _config.FieldGridResolutionX;
            int ny = _config.FieldGridResolutionY;
            float inv2Dx = 1f / (2f * _cellSizeX);
            float inv2Dy = 1f / (2f * _cellSizeY);

            for (int i = 1; i < nx - 1; i++)
            {
                for (int j = 1; j < ny - 1; j++)
                {
                    float dVdx = (_potentialField[i + 1, j] - _potentialField[i - 1, j]) * inv2Dx;
                    float dVdy = (_potentialField[i, j + 1] - _potentialField[i, j - 1]) * inv2Dy;
                    _electricField[i, j] = new Vector2(-dVdx, -dVdy);
                }
            }

            for (int i = 0; i < nx; i++)
            {
                _electricField[i, 0] = _electricField[i, 1];
                _electricField[i, ny - 1] = _electricField[i, ny - 2];
            }
            for (int j = 0; j < ny; j++)
            {
                _electricField[0, j] = _electricField[1, j];
                _electricField[nx - 1, j] = _electricField[nx - 2, j];
            }
        }

        public Vector2 GetElectricFieldAt(Vector2 worldPosition)
        {
            int gx = WorldToGridX(worldPosition.x);
            int gy = WorldToGridY(worldPosition.y);
            gx = Mathf.Clamp(gx, 0, _config.FieldGridResolutionX - 1);
            gy = Mathf.Clamp(gy, 0, _config.FieldGridResolutionY - 1);

            float fx = (worldPosition.x - GridToWorldX(gx)) / _cellSizeX;
            float fy = (worldPosition.y - GridToWorldY(gy)) / _cellSizeY;
            fx = Mathf.Clamp01(fx);
            fy = Mathf.Clamp01(fy);

            int gx1 = Mathf.Min(gx + 1, _config.FieldGridResolutionX - 1);
            int gy1 = Mathf.Min(gy + 1, _config.FieldGridResolutionY - 1);

            Vector2 e00 = _electricField[gx, gy];
            Vector2 e10 = _electricField[gx1, gy];
            Vector2 e01 = _electricField[gx, gy1];
            Vector2 e11 = _electricField[gx1, gy1];

            Vector2 ex0 = Vector2.Lerp(e00, e10, fx);
            Vector2 ex1 = Vector2.Lerp(e01, e11, fx);
            return Vector2.Lerp(ex0, ex1, fy);
        }

        public float GetPotentialAt(Vector2 worldPosition)
        {
            int gx = WorldToGridX(worldPosition.x);
            int gy = WorldToGridY(worldPosition.y);
            gx = Mathf.Clamp(gx, 0, _config.FieldGridResolutionX - 1);
            gy = Mathf.Clamp(gy, 0, _config.FieldGridResolutionY - 1);

            float fx = (worldPosition.x - GridToWorldX(gx)) / _cellSizeX;
            float fy = (worldPosition.y - GridToWorldY(gy)) / _cellSizeY;
            fx = Mathf.Clamp01(fx);
            fy = Mathf.Clamp01(fy);

            int gx1 = Mathf.Min(gx + 1, _config.FieldGridResolutionX - 1);
            int gy1 = Mathf.Min(gy + 1, _config.FieldGridResolutionY - 1);

            float v00 = _potentialField[gx, gy];
            float v10 = _potentialField[gx1, gy];
            float v01 = _potentialField[gx, gy1];
            float v11 = _potentialField[gx1, gy1];

            float vx0 = Mathf.Lerp(v00, v10, fx);
            float vx1 = Mathf.Lerp(v01, v11, fx);
            return Mathf.Lerp(vx0, vx1, fy);
        }

        public int WorldToGridX(float worldX)
        {
            float halfWidth = _config.SimulationSize.x * 0.5f;
            return Mathf.FloorToInt((worldX + halfWidth) / _cellSizeX);
        }

        public int WorldToGridY(float worldY)
        {
            float halfHeight = _config.SimulationSize.y * 0.5f;
            return Mathf.FloorToInt((worldY + halfHeight) / _cellSizeY);
        }

        public float GridToWorldX(int gridX)
        {
            float halfWidth = _config.SimulationSize.x * 0.5f;
            return (gridX + 0.5f) * _cellSizeX - halfWidth;
        }

        public float GridToWorldY(int gridY)
        {
            float halfHeight = _config.SimulationSize.y * 0.5f;
            return (gridY + 0.5f) * _cellSizeY - halfHeight;
        }

        public Vector2 GridToWorld(int gridX, int gridY)
        {
            return new Vector2(GridToWorldX(gridX), GridToWorldY(gridY));
        }

        public void LoadState(float[,] potentialField, Vector2[,] electricField, List<ElectrodeData> electrodes)
        {
            if (potentialField != null) _potentialField = (float[,])potentialField.Clone();
            if (electricField != null) _electricField = (Vector2[,])electricField.Clone();
            if (electrodes != null) _electrodes = new List<ElectrodeData>(electrodes);
        }

        public void LoadState(float[,] potentialField, Vector2[,] electricField, List<ElectrodeData> electrodes, List<MagneticCoilData> coils)
        {
            LoadState(potentialField, electricField, electrodes);
            if (_magneticSolver != null)
            {
                _magneticSolver.LoadState(coils);
            }
        }

        public Vector3 GetLorentzForce(Vector2 position, Vector2 velocity, float charge)
        {
            if (_magneticSolver == null || !_config.EnableMagneticField) return Vector3.zero;
            return _magneticSolver.GetLorentzForce(position, velocity, charge);
        }

        public float GetMagneticFieldZAt(Vector2 worldPosition)
        {
            if (_magneticSolver == null || !_config.EnableMagneticField) return 0f;
            return _magneticSolver.GetMagneticFieldZAt(worldPosition);
        }

        public void AddMagneticCoil(Vector2 worldPosition, float radius, float current, int turns = 1)
        {
            if (_magneticSolver != null)
            {
                _magneticSolver.AddCoil(worldPosition, radius, current, turns);
            }
        }

        public void RemoveMagneticCoilAt(Vector2 worldPosition, float tolerance = 1.0f)
        {
            if (_magneticSolver != null)
            {
                _magneticSolver.RemoveCoilAt(worldPosition, tolerance);
            }
        }

        public void ClearMagneticCoils()
        {
            if (_magneticSolver != null)
            {
                _magneticSolver.ClearCoils();
            }
        }

        public List<MagneticCoilData> GetMagneticCoils()
        {
            return _magneticSolver != null ? _magneticSolver.Coils : null;
        }

        public float[,] GetMagneticFieldZ()
        {
            return _magneticSolver != null ? _magneticSolver.MagneticFieldZ : null;
        }
    }
}
