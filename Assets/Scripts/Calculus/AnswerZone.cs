using UnityEngine;
using UnityEngine.UI;

namespace ElectroCooked.Calculus
{
    /// <summary>
    /// Um "círculo de resposta" no chão, estilo Bully: o professor pergunta e o
    /// aluno corre até a zona certa. A zona só segura o estado visual (rótulo,
    /// cor de idle/hover/flash); quem decide se a resposta está certa ou errada
    /// é o CalculusGameManager, por distância horizontal.
    /// </summary>
    public class AnswerZone : MonoBehaviour
    {
        [Header("Visual")]
        public Text label;
        public Renderer surfaceRenderer;

        public string Text { get; private set; }
        public bool IsCorrect { get; private set; }

        public Color idleColor = new Color(0.16f, 0.18f, 0.24f);
        public Color highlightColor = new Color(0.30f, 0.55f, 0.90f);
        public Color correctFlashColor = new Color(0.20f, 0.80f, 0.45f);
        public Color wrongFlashColor = new Color(0.90f, 0.28f, 0.22f);

        MaterialPropertyBlock _block;
        Color _currentColor = new Color(0.16f, 0.18f, 0.24f);
        Color _targetColor;
        Color _animStart;
        float _animT = 1f;
        float _flashUntil;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        void Awake()
        {
            _targetColor = idleColor;
            ApplyColor(idleColor);
        }

        public void Bind(string text, bool isCorrect, bool visible)
        {
            Text = text;
            IsCorrect = isCorrect;
            if (label != null)
            {
                label.text = text;
                label.gameObject.SetActive(visible);
            }
            ResetVisual();
        }

        public bool IsInside(Vector3 point, float radius)
        {
            float dx = point.x - transform.position.x;
            float dz = point.z - transform.position.z;
            return dx * dx + dz * dz <= radius * radius;
        }

        public void ResetVisual()
        {
            _targetColor = idleColor;
            _flashUntil = 0f;
        }

        public void Highlight()
        {
            _targetColor = highlightColor;
            _flashUntil = 0f;
        }

        public void Flash(bool correct)
        {
            _targetColor = correct ? correctFlashColor : wrongFlashColor;
            _flashUntil = Time.time + 0.7f;
        }

        void Update()
        {
            if (_flashUntil > 0f && Time.time >= _flashUntil)
            {
                _flashUntil = 0f;
                _targetColor = idleColor;
            }

            if (_animT < 1f)
            {
                _animT = Mathf.Min(1f, _animT + Time.deltaTime * 9f);
                _currentColor = Color.Lerp(_animStart, _targetColor, _animT);
                ApplyColor(_currentColor);
            }
        }

        void ApplyColor(Color color)
        {
            if (surfaceRenderer == null) return;
            if (_block == null) _block = new MaterialPropertyBlock();
            surfaceRenderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _block.SetColor(ColorId, color);
            surfaceRenderer.SetPropertyBlock(_block);
        }
    }
}