using System.Collections.Generic;
using UnityEngine;
using PlasmaSimulation.Data;

namespace PlasmaSimulation.Simulation
{
    public class MagneticFieldSolver : MonoBehaviour
    {
        private SimulationConfig _config;
        private List<MagneticCoilData> _coils;
        private float[,] _magneticFieldZ;

        public List<MagneticCoilData> Coils => _coils;
        public float[,] MagneticFieldZ => _magneticFieldZ;
        public int CoilCount => _coils != null ? _coils.Count : 0;

        public void Initialize(SimulationConfig config)
        {
            _config = config;
            _coils = new List<MagneticCoilData>();
            _magneticFieldZ = new float[config.FieldGridResolutionX, config.FieldGridResolutionY];
            ClearField();
        }

        public void ClearField()
        {
            int nx = _config.FieldGridResolutionX;
            int ny = _config.FieldGridResolutionY;
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    _magneticFieldZ[i, j] = 0f;
                }
            }
        }

        public void AddCoil(Vector2 worldPosition, float radius, float current, int turns = 1)
        {
            _coils.Add(new MagneticCoilData(worldPosition, radius, current, turns));
        }

        public void RemoveCoilAt(Vector2 worldPosition, float tolerance = 1.0f)
        {
            for (int i = _coils.Count - 1; i >= 0; i--)
            {
                if (Vector2.Distance(_coils[i].Position, worldPosition) < tolerance)
                {
                    _coils.RemoveAt(i);
                }
            }
        }

        public void ClearCoils()
        {
            _coils.Clear();
            ClearField();
        }

        public void SolveField()
        {
            if (!_config.EnableMagneticField || _coils.Count == 0)
            {
                ClearField();
                return;
            }

            int nx = _config.FieldGridResolutionX;
            int ny = _config.FieldGridResolutionY;
            float mu0 = _config.VacuumPermeability;

            float cellSizeX = _config.SimulationSize.x / nx;
            float cellSizeY = _config.SimulationSize.y / ny;
            float halfW = _config.SimulationSize.x * 0.5f;
            float halfH = _config.SimulationSize.y * 0.5f;

            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    float bx = (i + 0.5f) * cellSizeX - halfW;
                    float by = (j + 0.5f) * cellSizeY - halfH;
                    Vector2 cellPos = new Vector2(bx, by);

                    float totalBz = 0f;
                    for (int c = 0; c < _coils.Count; c++)
                    {
                        totalBz += CalculateBiotSavart(cellPos, _coils[c], mu0);
                    }

                    _magneticFieldZ[i, j] = totalBz;
                }
            }
        }

        private float CalculateBiotSavart(Vector2 point, MagneticCoilData coil, float mu0)
        {
            Vector2 delta = point - coil.Position;
            float r = delta.magnitude;
            float R = coil.Radius;

            if (r < 0.001f) r = 0.001f;

            float denom = Mathf.Pow(R * R + r * r, 1.5f);
            if (denom < 1e-20f) return 0f;

            float I = coil.Current * coil.Turns;
            float Bz = (mu0 * I * R * R) / (2f * denom);

            float radialDecay = Mathf.Exp(-r * 0.5f);
            Bz *= radialDecay;

            return Bz * _config.LorentzForceScale;
        }

        public float GetMagneticFieldZAt(Vector2 worldPosition)
        {
            if (!_config.EnableMagneticField || _coils.Count == 0) return 0f;

            int nx = _config.FieldGridResolutionX;
            int ny = _config.FieldGridResolutionY;
            float cellSizeX = _config.SimulationSize.x / nx;
            float cellSizeY = _config.SimulationSize.y / ny;
            float halfW = _config.SimulationSize.x * 0.5f;
            float halfH = _config.SimulationSize.y * 0.5f;

            float fx = (worldPosition.x + halfW) / cellSizeX - 0.5f;
            float fy = (worldPosition.y + halfH) / cellSizeY - 0.5f;

            int gx = Mathf.FloorToInt(fx);
            int gy = Mathf.FloorToInt(fy);
            gx = Mathf.Clamp(gx, 0, nx - 2);
            gy = Mathf.Clamp(gy, 0, ny - 2);

            float tx = fx - gx;
            float ty = fy - gy;
            tx = Mathf.Clamp01(tx);
            ty = Mathf.Clamp01(ty);

            float b00 = _magneticFieldZ[gx, gy];
            float b10 = _magneticFieldZ[gx + 1, gy];
            float b01 = _magneticFieldZ[gx, gy + 1];
            float b11 = _magneticFieldZ[gx + 1, gy + 1];

            float bx0 = Mathf.Lerp(b00, b10, tx);
            float bx1 = Mathf.Lerp(b01, b11, tx);
            return Mathf.Lerp(bx0, bx1, ty);
        }

        public Vector3 GetLorentzForce(Vector2 position, Vector2 velocity, float charge)
        {
            if (!_config.EnableMagneticField || _coils.Count == 0) return Vector3.zero;

            float Bz = GetMagneticFieldZAt(position);
            Vector3 v3 = new Vector3(velocity.x, velocity.y, 0f);
            Vector3 B = new Vector3(0f, 0f, Bz);
            Vector3 vCrossB = Vector3.Cross(v3, B);
            return charge * vCrossB;
        }

        public void LoadState(List<MagneticCoilData> coils)
        {
            if (coils != null)
            {
                _coils = new List<MagneticCoilData>(coils);
                SolveField();
            }
        }
    }
}
