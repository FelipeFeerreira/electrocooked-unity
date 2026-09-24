using System.Collections;
using System.Globalization;
using System.Text;
using ElectroCooked.Circuit;
using ElectroCooked.Core;
using ElectroCooked.Minigame;
using UnityEngine;
using UnityEngine.UI;

namespace ElectroCooked.UI
{
    /// <summary>
    /// Painel único (Canvas Screen Space - Overlay) que aparece sempre que um
    /// desafio de cálculo começa. Troca de layout conforme o MinigameStyle
    /// sorteado no pedido: teclado numérico, múltipla escolha ou régua/slider.
    ///
    /// O estilo "numérico" usa um teclado de botões na tela (e não um InputField),
    /// porque o projeto roda com o Input System novo e porque assim o jogo
    /// continua jogável no controle, como um Overcooked de verdade.
    /// </summary>
    public class MinigameUI : MonoBehaviour
    {
        [Header("Painel raiz")]
        public GameObject rootPanel;
        public Text headerText;
        public Text ticketReminderText;
        public Slider timerBar;
        public Image timerBarFill;
        public Text feedbackText;

        [Header("Estilo: teclado numérico")]
        public GameObject numericPanel;
        public Text numericDisplayText;
        public Button[] digitButtons = new Button[10]; // índice == dígito
        public Button decimalButton;
        public Button clearButton;
        public Button numericConfirmButton;

        [Header("Estilo: múltipla escolha")]
        public GameObject multipleChoicePanel;
        public Button[] choiceButtons = new Button[4];

        [Header("Estilo: régua / slider")]
        public GameObject sliderPanel;
        public Slider answerSlider;
        public Text answerSliderValueText;
        public Button sliderConfirmButton;

        [Header("Entrada pelo teclado do PC")]
        [Tooltip("Quando a calculadora estiver aberta, o teclado fica com ela.")]
        public CalculatorUI calculator;

        [Header("Cores da barra de tempo")]
        public Color timeSafeColor = new Color(0.25f, 0.80f, 0.45f);
        public Color timeDangerColor = new Color(0.90f, 0.25f, 0.20f);

        CircuitOrder _activeOrder;
        Coroutine _hideRoutine;
        readonly StringBuilder _typed = new StringBuilder();

        bool IsNumericActive =>
            rootPanel != null && rootPanel.activeSelf
            && numericPanel != null && numericPanel.activeSelf
            && _activeOrder != null && _activeOrder.Style == MinigameStyle.NumericInput;

        void Update()
        {
            if (!IsNumericActive) return;
            if (calculator != null && calculator.IsOpen) return;

            string key = InputReader.CalculatorKeyThisFrame;
            if (key.Length == 0) return;

            if (key == "=" || key == "C" || key == "<")
            {
                if (key == "=") SubmitTyped();
                else if (key == "C") ClearTyped();
                else if (_typed.Length > 0) { _typed.Length--; RefreshDisplay(); }
                return;
            }

            TypeChar(key);
        }

        void Start()
        {
            if (MinigameManager.Instance == null)
            {
                Debug.LogError("[MinigameUI] Nenhum MinigameManager na cena.");
                return;
            }

            MinigameManager.Instance.OnChallengeStarted += HandleStarted;
            MinigameManager.Instance.OnChallengeTick += HandleTick;
            MinigameManager.Instance.OnChallengeEnded += HandleEnded;

            WireKeypad();

            if (sliderConfirmButton != null)
                sliderConfirmButton.onClick.AddListener(SubmitSliderValue);
            if (answerSlider != null)
                answerSlider.onValueChanged.AddListener(_ => UpdateSliderLabel());

            if (rootPanel != null) rootPanel.SetActive(false);
        }

        void OnDestroy()
        {
            if (MinigameManager.Instance == null) return;
            MinigameManager.Instance.OnChallengeStarted -= HandleStarted;
            MinigameManager.Instance.OnChallengeTick -= HandleTick;
            MinigameManager.Instance.OnChallengeEnded -= HandleEnded;
        }

        void WireKeypad()
        {
            for (int i = 0; i < digitButtons.Length; i++)
            {
                if (digitButtons[i] == null) continue;
                int digit = i;
                digitButtons[i].onClick.RemoveAllListeners();
                digitButtons[i].onClick.AddListener(() => TypeChar(digit.ToString(CultureInfo.InvariantCulture)));
            }

            if (decimalButton != null)
            {
                decimalButton.onClick.RemoveAllListeners();
                decimalButton.onClick.AddListener(() => TypeChar("."));
            }
            if (clearButton != null)
            {
                clearButton.onClick.RemoveAllListeners();
                clearButton.onClick.AddListener(ClearTyped);
            }
            if (numericConfirmButton != null)
            {
                numericConfirmButton.onClick.RemoveAllListeners();
                numericConfirmButton.onClick.AddListener(SubmitTyped);
            }
        }

        void TypeChar(string c)
        {
            if (_typed.Length >= 8) return;
            if (c == "." && _typed.ToString().Contains(".")) return;
            if (c == "." && _typed.Length == 0) _typed.Append('0');
            _typed.Append(c);
            RefreshDisplay();
        }

        void ClearTyped()
        {
            _typed.Clear();
            RefreshDisplay();
        }

        void RefreshDisplay()
        {
            if (numericDisplayText == null) return;
            string unit = _activeOrder != null ? _activeOrder.Unit : "";
            numericDisplayText.text = _typed.Length == 0 ? $"_ _ _  {unit}" : $"{_typed}  {unit}";
        }

        void SubmitTyped()
        {
            if (_typed.Length == 0)
            {
                if (feedbackText != null) feedbackText.text = "Digite o valor calculado.";
                return;
            }

            if (float.TryParse(_typed.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                MinigameManager.Instance.SubmitNumericAnswer(value);
            else if (feedbackText != null)
                feedbackText.text = "Número inválido.";
        }

        void HandleStarted(CircuitStation station, CircuitOrder order)
        {
            _activeOrder = order;
            if (_hideRoutine != null) { StopCoroutine(_hideRoutine); _hideRoutine = null; }

            if (rootPanel != null) rootPanel.SetActive(true);
            if (feedbackText != null) feedbackText.text = "";
            if (headerText != null) headerText.text = $"CALCULE {order.QuestionLabel().ToUpperInvariant()}";
            if (ticketReminderText != null)
                ticketReminderText.text = BuildReminder(order);

            bool isNumeric = order.Style == MinigameStyle.NumericInput;
            bool isChoice = order.Style == MinigameStyle.MultipleChoice;
            bool isSlider = order.Style == MinigameStyle.Slider;

            if (numericPanel != null) numericPanel.SetActive(isNumeric);
            if (multipleChoicePanel != null) multipleChoicePanel.SetActive(isChoice);
            if (sliderPanel != null) sliderPanel.SetActive(isSlider);

            if (isNumeric) ClearTyped();
            if (isChoice) SetupChoiceButtons(order);

            if (isSlider && answerSlider != null)
            {
                answerSlider.minValue = order.SliderMin;
                answerSlider.maxValue = order.SliderMax;
                answerSlider.value = (order.SliderMin + order.SliderMax) * 0.5f;
                UpdateSliderLabel();
            }
        }

        /// <summary>
        /// Monta o lembrete do circuito em UMA linha por grandeza, separando os
        /// dados (branco) da pergunta (amarelo). Ler rápido importa: o cronômetro
        /// está correndo enquanto o aluno lê.
        /// </summary>
        static string BuildReminder(CircuitOrder order)
        {
            var lines = order.BuildTicketLines();
            var sb = new StringBuilder();

            // UMA grandeza por linha. Tentei juntar tudo numa linha só para
            // economizar espaço e ficou ilegível: com os "=" empilhados o olho
            // acha o número que procura sem reler a frase inteira.
            for (int i = 0; i < lines.Count - 1; i++)
                sb.Append(lines[i]).Append('\n');

            sb.Append("\n<b><color=#FFD24A>");
            sb.Append(lines[lines.Count - 1]);
            sb.Append("</color></b>");
            return sb.ToString();
        }

        /// <summary>
        /// Recebe um valor vindo de fora (a calculadora) e coloca na resposta:
        /// preenche o visor do teclado numérico ou posiciona a régua.
        /// </summary>
        public void ApplyExternalValue(float value)
        {
            if (_activeOrder == null) return;

            if (_activeOrder.Style == MinigameStyle.NumericInput)
            {
                _typed.Clear();
                _typed.Append(value.ToString("0.####", CultureInfo.InvariantCulture));
                RefreshDisplay();
                if (feedbackText != null) feedbackText.text = "Valor da calculadora carregado. Confira e entregue.";
            }
            else if (_activeOrder.Style == MinigameStyle.Slider && answerSlider != null)
            {
                answerSlider.value = Mathf.Clamp(value, answerSlider.minValue, answerSlider.maxValue);
                UpdateSliderLabel();
            }
            else if (feedbackText != null)
            {
                feedbackText.text = "Nesta rodada e multipla escolha: clique na alternativa.";
            }
        }

        void SetupChoiceButtons(CircuitOrder order)
        {
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] == null) continue;
                bool hasOption = order.MultipleChoiceOptions != null && i < order.MultipleChoiceOptions.Length;
                choiceButtons[i].gameObject.SetActive(hasOption);
                if (!hasOption) continue;

                float optionValue = order.MultipleChoiceOptions[i];
                var label = choiceButtons[i].GetComponentInChildren<Text>();
                if (label != null) label.text = $"{optionValue:0.###} {order.Unit}";

                int capturedIndex = i;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => MinigameManager.Instance.SubmitMultipleChoiceAnswer(capturedIndex));
            }
        }

        void SubmitSliderValue()
        {
            if (answerSlider == null) return;
            MinigameManager.Instance.SubmitNumericAnswer(answerSlider.value);
        }

        void UpdateSliderLabel()
        {
            if (answerSliderValueText != null && answerSlider != null && _activeOrder != null)
                answerSliderValueText.text = $"{answerSlider.value:0.###} {_activeOrder.Unit}";
        }

        void HandleTick(float remaining, float total)
        {
            if (timerBar != null)
                timerBar.value = total > 0f ? remaining / total : 0f;

            if (timerBarFill != null)
            {
                float t = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;
                timerBarFill.color = Color.Lerp(timeDangerColor, timeSafeColor, Mathf.Clamp01(t * 2.5f));
            }
        }

        void HandleEnded(bool correct, float correctAnswer)
        {
            if (feedbackText != null)
            {
                string unit = _activeOrder != null ? _activeOrder.Unit : "";
                feedbackText.text = correct
                    ? "CIRCUITO ENTREGUE! Boa conta."
                    : $"BUM! Conta errada — o certo era {correctAnswer:0.###} {unit}. Pegue o extintor!";
                feedbackText.color = correct ? timeSafeColor : timeDangerColor;
            }

            _activeOrder = null;
            _typed.Clear();

            if (numericPanel != null) numericPanel.SetActive(false);
            if (multipleChoicePanel != null) multipleChoicePanel.SetActive(false);
            if (sliderPanel != null) sliderPanel.SetActive(false);

            _hideRoutine = StartCoroutine(HideAfterDelay(2.2f));
        }

        IEnumerator HideAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (rootPanel != null) rootPanel.SetActive(false);
            _hideRoutine = null;
        }
    }
}
