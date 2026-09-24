using ElectroCooked.Core;
using ElectroCooked.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElectroCooked.UI
{
    /// <summary>
    /// HUD fixo da aula: tempo restante, nota atual e nº de explosões.
    /// No fim do período mostra o boletim, com opção de repetir a aula ou
    /// voltar para o corredor da escola.
    /// </summary>
    public class HUDUI : MonoBehaviour
    {
        [Header("HUD")]
        public Text timeText;
        public Text gradeText;
        public Text mistakesText;
        public Text comboText;
        public Text controlsHintText;

        [Header("Resumo final (boletim)")]
        public GameObject summaryPanel;
        public Text summaryText;

        [Header("Textos configuráveis")]
        [Tooltip("Dica de controles no topo. Vazio = usa padrão da aula de circuitos.")]
        public string controlsHintOverride = "";
        [Tooltip("Rótulo de entregas no boletim final.")]
        public string deliveredLabel = "Circuitos entregues:";
        [Tooltip("Rótulo de erros no boletim final e no HUD.")]
        public string mistakeLabel = "Explosoes";

        [Header("Cores")]
        public Color normalTimeColor = Color.white;
        public Color lowTimeColor = new Color(0.95f, 0.35f, 0.30f);
        public Color comboActiveColor = new Color(1f, 0.80f, 0.25f);

        bool _ended;

        void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnClassTimeChanged += HandleTimeChanged;
                GameManager.Instance.OnClassEnded += HandleClassEnded;
            }
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnGradeChanged += HandleGradeChanged;
                ScoreManager.Instance.OnMistake += HandleMistake;
                ScoreManager.Instance.OnComboChanged += HandleComboChanged;
                HandleGradeChanged(ScoreManager.Instance.CurrentGrade);
                HandleMistake(ScoreManager.Instance.MistakeCount);
                HandleComboChanged(ScoreManager.Instance.ComboMultiplier);
            }

            if (summaryPanel != null) summaryPanel.SetActive(false);
            if (controlsHintText != null)
                controlsHintText.text = string.IsNullOrEmpty(controlsHintOverride)
                    ? "WASD andar    E pegar / encaixar    ESPACO extintor    C calculadora    F formulas"
                    : controlsHintOverride;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnClassTimeChanged -= HandleTimeChanged;
                GameManager.Instance.OnClassEnded -= HandleClassEnded;
            }
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnGradeChanged -= HandleGradeChanged;
                ScoreManager.Instance.OnMistake -= HandleMistake;
                ScoreManager.Instance.OnComboChanged -= HandleComboChanged;
            }
        }

        void Update()
        {
            if (!_ended) return;

            if (InputReader.RestartPressed)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            else if (InputReader.ConfirmPressed)
                SceneManager.LoadScene(SceneNames.School);
        }

        void HandleTimeChanged(float remaining, float total)
        {
            if (timeText == null) return;
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            timeText.text = $"{minutes:00}:{seconds:00}";
            timeText.color = remaining <= 30f ? lowTimeColor : normalTimeColor;
        }

        void HandleGradeChanged(float grade)
        {
            if (gradeText != null) gradeText.text = $"NOTA {grade:0.0}";
        }

        void HandleMistake(int count)
        {
            if (mistakesText != null) mistakesText.text = $"{mistakeLabel} {count}";
        }

        void HandleComboChanged(int multiplier)
        {
            if (comboText == null) return;
            comboText.gameObject.SetActive(multiplier > 1);
            if (multiplier > 1)
                comboText.text = $"COMBO x{multiplier}";
            comboText.color = comboActiveColor;
        }

        void HandleClassEnded(float finalGrade)
        {
            _ended = true;
            if (summaryPanel == null) return;

            summaryPanel.SetActive(true);
            if (summaryText == null) return;

            int mistakes = ScoreManager.Instance != null ? ScoreManager.Instance.MistakeCount : 0;
            int delivered = ScoreManager.Instance != null ? ScoreManager.Instance.DeliveredCount : 0;
            string subject = GameManager.Instance != null ? GameManager.Instance.subjectName : "AULA";
            float best = GameManager.Instance != null
                ? StudentRecord.BestGrade(GameManager.Instance.subjectId)
                : finalGrade;

            summaryText.text =
                $"<b>FIM DA AULA — {subject}</b>\n\n" +
                $"{deliveredLabel}  {delivered}\n" +
                $"{mistakeLabel}:  {mistakes}\n\n" +
                $"<b>NOTA FINAL:  {finalGrade:0.0} / 10   ({StudentRecord.Concept(finalGrade)})</b>\n" +
                $"Melhor nota ate agora:  {best:0.0}\n\n" +
                "<color=#FFD24A>[ R ]  repetir a aula</color>\n" +
                "<color=#8FD8FF>[ ENTER ]  voltar para a escola</color>";
        }
    }
}
