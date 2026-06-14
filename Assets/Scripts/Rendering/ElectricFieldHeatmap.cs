using UnityEngine;
using PlasmaSimulation.Data;
using PlasmaSimulation.Simulation;

namespace PlasmaSimulation.Rendering
{
    public class ElectricFieldHeatmap : MonoBehaviour
    {
        private SimulationConfig _config;
        private ElectromagneticFieldSolver _fieldSolver;
        private Camera _targetCamera;
        private Material _heatmapMaterial;
        private Texture2D _fieldTexture;
        private GameObject _heatmapQuad;
        private MeshRenderer _quadRenderer;
        private Color[] _heatmapPixels;
        private float _lastFieldUpdate = -1f;

        public void Initialize(SimulationConfig config, ElectromagneticFieldSolver fieldSolver, Camera targetCamera)
        {
            _config = config;
            _fieldSolver = fieldSolver;
            _targetCamera = targetCamera;

            CreateHeatmapMaterial();
            CreateFieldTexture();
            CreateHeatmapQuad();
        }

        private void CreateHeatmapMaterial()
        {
            Shader shader = Shader.Find("Plasma/ElectricFieldHeatmap");
            if (shader == null)
            {
                Debug.LogWarning("Plasma/ElectricFieldHeatmap shader not found, using fallback.");
                shader = Shader.Find("Unlit/Transparent");
            }
            _heatmapMaterial = new Material(shader);
            _heatmapMaterial.SetFloat("_Opacity", _config.HeatmapOpacity);
        }

        private void CreateFieldTexture()
        {
            int width = _config.FieldGridResolutionX;
            int height = _config.FieldGridResolutionY;
            _fieldTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _fieldTexture.filterMode = FilterMode.Bilinear;
            _fieldTexture.wrapMode = TextureWrapMode.Clamp;
            _heatmapPixels = new Color[width * height];
        }

        private void CreateHeatmapQuad()
        {
            _heatmapQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _heatmapQuad.name = "FieldHeatmap";
            _heatmapQuad.transform.SetParent(transform);
            _heatmapQuad.transform.localPosition = new Vector3(0f, 0f, 5f);
            _heatmapQuad.transform.localScale = new Vector3(_config.SimulationSize.x, _config.SimulationSize.y, 1f);

            _quadRenderer = _heatmapQuad.GetComponent<MeshRenderer>();
            _quadRenderer.material = _heatmapMaterial;
            _quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _quadRenderer.receiveShadows = false;
            _quadRenderer.sortingOrder = -1000;
        }

        private void Update()
        {
            if (_fieldSolver == null || _config == null) return;
            UpdateHeatmapTexture();
            HandleResize();
        }

        private void UpdateHeatmapTexture()
        {
            int width = _config.FieldGridResolutionX;
            int height = _config.FieldGridResolutionY;
            float scale = _config.HeatmapScale;

            var eField = _fieldSolver.ElectricField;
            if (eField == null) return;

            float maxMag = 0f;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    float mag = eField[i, j].magnitude;
                    if (mag > maxMag) maxMag = mag;
                }
            }

            if (maxMag < 1e-10f) maxMag = 1e-10f;
            float invMax = 1f / maxMag;

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    float mag = eField[i, j].magnitude * invMax;
                    _heatmapPixels[j * width + i] = HeatmapColor(mag);
                }
            }

            _fieldTexture.SetPixels(_heatmapPixels);
            _fieldTexture.Apply(false);

            if (_heatmapMaterial != null)
            {
                _heatmapMaterial.SetTexture("_MainTex", _fieldTexture);
                _heatmapMaterial.SetFloat("_Opacity", _config.HeatmapOpacity);
                _heatmapMaterial.SetFloat("_FieldScale", scale);
            }
        }

        private Color HeatmapColor(float t)
        {
            t = Mathf.Clamp01(t);
            Color c;
            if (t < 0.25f)
            {
                c = Color.Lerp(new Color(0f, 0f, 0.3f, 0f), new Color(0f, 0f, 1f, 1f), t * 4f);
            }
            else if (t < 0.5f)
            {
                c = Color.Lerp(new Color(0f, 0f, 1f, 1f), new Color(0f, 1f, 1f, 1f), (t - 0.25f) * 4f);
            }
            else if (t < 0.75f)
            {
                c = Color.Lerp(new Color(0f, 1f, 1f, 1f), new Color(1f, 1f, 0f, 1f), (t - 0.5f) * 4f);
            }
            else
            {
                c = Color.Lerp(new Color(1f, 1f, 0f, 1f), new Color(1f, 0f, 0f, 1f), (t - 0.75f) * 4f);
            }
            return c;
        }

        private void HandleResize()
        {
            if (_targetCamera == null || _heatmapQuad == null) return;

            if (_targetCamera.orthographic)
            {
                float aspect = (float)Screen.width / Screen.height;
                float verticalSize = _config.SimulationSize.y * 0.5f;
                _targetCamera.orthographicSize = verticalSize;

                float targetAspect = _config.SimulationSize.x / _config.SimulationSize.y;
                if (aspect < targetAspect)
                {
                    _targetCamera.orthographicSize = _config.SimulationSize.x * 0.5f / aspect;
                }
            }
        }

        public void Cleanup()
        {
            if (_fieldTexture != null) { Destroy(_fieldTexture); _fieldTexture = null; }
            if (_heatmapMaterial != null) { Destroy(_heatmapMaterial); _heatmapMaterial = null; }
            if (_heatmapQuad != null) { Destroy(_heatmapQuad); _heatmapQuad = null; }
            _heatmapPixels = null;
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
