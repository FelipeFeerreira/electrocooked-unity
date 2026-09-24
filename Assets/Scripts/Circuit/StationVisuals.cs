using UnityEngine;

namespace ElectroCooked.Circuit
{
    /// <summary>
    /// Feedback visual da bancada: pinta o tampo conforme o estado
    /// (esperando / montando / calculando / queimado) e faz a bancada
    /// "pular" um pouco quando uma peça é aceita ou recusada.
    /// Igual ao Overcooked, o jogador precisa ler a cozinha de longe.
    /// </summary>
    [RequireComponent(typeof(CircuitStation))]
    public class StationVisuals : MonoBehaviour
    {
        [Header("Renderer do tampo da bancada")]
        public Renderer surfaceRenderer;

        [Header("Cores por estado")]
        public Color idleColor = new Color(0.35f, 0.38f, 0.45f);
        public Color assemblingColor = new Color(0.20f, 0.55f, 0.85f);
        public Color minigameColor = new Color(0.95f, 0.75f, 0.15f);
        public Color brokenColor = new Color(0.30f, 0.13f, 0.10f);
        public Color rejectColor = new Color(0.85f, 0.20f, 0.20f);

        [Header("Punch de escala")]
        public float punchDuration = 0.18f;
        public float punchAmount = 0.08f;

        CircuitStation _station;
        MaterialPropertyBlock _block;
        Vector3 _baseScale;
        float _punchTimer;
        float _flashTimer;
        Color _flashColor;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        void Awake()
        {
            _station = GetComponent<CircuitStation>();
            _block = new MaterialPropertyBlock();
            if (surfaceRenderer != null) _baseScale = surfaceRenderer.transform.localScale;
        }

        void OnEnable()
        {
            _station.OnOrderAssigned += HandleAssigned;
            _station.OnPartPlaced += HandlePlaced;
            _station.OnPartRejected += HandleRejected;
            _station.OnOrderDelivered += HandleChanged;
            _station.OnExploded += HandleChanged;
            _station.OnFireOut += HandleChanged;
        }

        void OnDisable()
        {
            _station.OnOrderAssigned -= HandleAssigned;
            _station.OnPartPlaced -= HandlePlaced;
            _station.OnPartRejected -= HandleRejected;
            _station.OnOrderDelivered -= HandleChanged;
            _station.OnExploded -= HandleChanged;
            _station.OnFireOut -= HandleChanged;
        }

        void HandleAssigned(CircuitStation s, Core.CircuitOrder o) => Apply();
        void HandleChanged(CircuitStation s) => Apply();

        void HandlePlaced(CircuitStation s)
        {
            _punchTimer = punchDuration;
            Apply();
        }

        void HandleRejected(CircuitStation s)
        {
            _flashTimer = 0.25f;
            _flashColor = rejectColor;
            _punchTimer = punchDuration;
        }

        void Update()
        {
            if (_punchTimer > 0f)
            {
                _punchTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(_punchTimer / punchDuration);
                float s = 1f + Mathf.Sin(t * Mathf.PI) * punchAmount;
                if (surfaceRenderer != null)
                    surfaceRenderer.transform.localScale = new Vector3(_baseScale.x * s, _baseScale.y, _baseScale.z * s);
            }

            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                SetColor(_flashColor);
                if (_flashTimer <= 0f) Apply();
            }
        }

        void Apply()
        {
            if (_flashTimer > 0f) return;
            SetColor(ColorForState(_station.CurrentState));
        }

        Color ColorForState(CircuitStation.State state)
        {
            switch (state)
            {
                case CircuitStation.State.Assembling: return assemblingColor;
                case CircuitStation.State.InMinigame: return minigameColor;
                case CircuitStation.State.Broken: return brokenColor;
                default: return idleColor;
            }
        }

        void SetColor(Color c)
        {
            if (surfaceRenderer == null) return;
            surfaceRenderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, c); // URP
            _block.SetColor(ColorId, c);     // built-in, por segurança
            surfaceRenderer.SetPropertyBlock(_block);
        }
    }
}
