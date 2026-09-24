using ElectroCooked.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace ElectroCooked.Calculus
{
    /// <summary>
    /// Painel central do minigame de cálculo: mostra a pergunta do professor, a
    /// barra de tempo da pergunta atual, as vidas restantes e o feedback do
    /// último acerto/erro. O tempo da aula, a nota e o combo continuam no HUDUI.
    /// </summary>
    public class CalculusHUD : MonoBehaviour
    {
        [Header("Painel da pergunta")]
        public GameObject rootPanel;
        public Text questionText;
        public Slider timerBar;
        public Image timerBarFill;
        public Text livesText;
        public Text feedbackText;

        [Header("Cores")]
        public Color feedbackCorrect = new Color(0.35f, 0.95f, 0.55f);
        public Color feedbackWrong = new Color(0.95f, 0.35f, 0.35f);
        public Color timerGoodColor = new Color(0.25f, 0.80f, 0.45f);
        public Color timerLowColor = new Color(0.95f, 0.35f, 0.30f);

        public void ShowQuestion(CalculusQuestion question)
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            if (questionText != null) questionText.text = question.Prompt;
            if (feedbackText != null) feedbackText.text = "";
            TickTimer(question.TimeLimitSeconds, question.TimeLimitSeconds);
        }

        public void TickTimer(float remaining, float total)
        {
            if (timerBar == null) return;
            timerBar.maxValue = Mathf.Max(0.0001f, total);
            timerBar.value = remaining;
            if (timerBarFill != null)
                timerBarFill.color = remaining <= total * 0.3f ? timerLowColor : timerGoodColor;
        }

        public void SetLives(int lives)
        {
            if (livesText == null) return;
            livesText.text = $"VIDAS: {Mathf.Max(0, lives)}";
            livesText.color = lives == 1 ? feedbackWrong : Color.white;
        }

        public void ShowFeedback(string message, Color color)
        {
            if (feedbackText == null) return;
            feedbackText.text = message;
            feedbackText.color = color;
        }

        void Update()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.State == GameManager.ClassState.Ended && rootPanel != null)
                rootPanel.SetActive(false);
        }
    }
}