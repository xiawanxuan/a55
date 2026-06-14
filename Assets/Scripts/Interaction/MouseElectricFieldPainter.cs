using UnityEngine;
using PlasmaSimulation.Data;
using PlasmaSimulation.Simulation;

namespace PlasmaSimulation.Interaction
{
    public class MouseElectricFieldPainter : MonoBehaviour
    {
        private Camera _mainCamera;
        private SimulationConfig _config;
        private ElectromagneticFieldSolver _fieldSolver;
        private bool _isPainting;
        private float _currentChargeSign = 1f;
        private Vector2 _lastPaintPosition;

        public bool IsPainting => _isPainting;
        public float CurrentChargeSign => _currentChargeSign;

        public void Initialize(SimulationConfig config, ElectromagneticFieldSolver fieldSolver)
        {
            _config = config;
            _fieldSolver = fieldSolver;
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                _mainCamera = FindObjectOfType<Camera>();
            }
        }

        private void Update()
        {
            if (_config == null || _mainCamera == null) return;

            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _currentChargeSign = 1f;
                Debug.Log("Switched to positive electrode");
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                _currentChargeSign = -1f;
                Debug.Log("Switched to negative electrode");
            }

            if (Input.GetMouseButtonDown(0))
            {
                _isPainting = true;
                Vector2 worldPos = ScreenToWorld(Input.mousePosition);
                _lastPaintPosition = worldPos;
                PlaceElectrode(worldPos);
            }

            if (Input.GetMouseButton(0) && _isPainting)
            {
                Vector2 worldPos = ScreenToWorld(Input.mousePosition);
                float dist = Vector2.Distance(worldPos, _lastPaintPosition);
                if (dist > _config.ElectrodeBrushSize * 0.5f)
                {
                    PlaceElectrode(worldPos);
                    _lastPaintPosition = worldPos;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                _isPainting = false;
            }

            if (Input.GetMouseButtonDown(1))
            {
                Vector2 worldPos = ScreenToWorld(Input.mousePosition);
                _fieldSolver.RemoveElectrodeAt(worldPos, 0.8f);
                _fieldSolver.SolveField();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                ClearElectrodes();
            }
        }

        private void PlaceElectrode(Vector2 worldPosition)
        {
            Vector2 halfSize = _config.SimulationSize * 0.5f;
            worldPosition.x = Mathf.Clamp(worldPosition.x, -halfSize.x + 0.2f, halfSize.x - 0.2f);
            worldPosition.y = Mathf.Clamp(worldPosition.y, -halfSize.y + 0.2f, halfSize.y - 0.2f);

            float charge = _currentChargeSign * _config.ElectrodeDefaultCharge;
            _fieldSolver.AddElectrode(worldPosition, charge, _config.ElectrodeDefaultRadius);
            _fieldSolver.SolveField();
        }

        public void ClearElectrodes()
        {
            _fieldSolver.ClearElectrodes();
            _fieldSolver.SolveField();
            Debug.Log("All electrodes cleared");
        }

        public Vector2 ScreenToWorld(Vector3 screenPosition)
        {
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -_mainCamera.transform.position.z));
            return new Vector2(worldPos.x, worldPos.y);
        }

        public void SetChargeSign(float sign)
        {
            _currentChargeSign = Mathf.Sign(sign);
        }

        public void ToggleChargeSign()
        {
            _currentChargeSign = -_currentChargeSign;
        }
    }
}
