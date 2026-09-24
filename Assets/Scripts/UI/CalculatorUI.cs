using System;
using System.Globalization;
using ElectroCooked.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ElectroCooked.UI
{
    /// <summary>
    /// Calculadora de bolso na tela (tecla C). Serve pra conta quebrada —
    /// tipo 12 / 85 = 0,1412 — que é onde o aluno perde tempo e erra por
    /// aritmética, não por não saber a fórmula. O desafio do jogo é escolher
    /// a fórmula certa e ser rápido; dividir na mão não é o ponto.
    ///
    /// Funciona com os botões na tela E com o teclado numérico.
    /// O botão "USAR NA RESPOSTA" joga o resultado direto no minigame.
    /// </summary>
    public class CalculatorUI : MonoBehaviour
    {
        [Header("Painel")]
        public GameObject rootPanel;
        public Text displayText;      // número sendo digitado / resultado
        public Text expressionText;   // a conta em andamento, ex: "12 /"

        [Header("Teclas")]
        public Button[] digitButtons = new Button[10]; // índice == dígito
        public Button dotButton;
        public Button clearButton;
        public Button backspaceButton;
        public Button addButton;
        public Button subtractButton;
        public Button multiplyButton;
        public Button divideButton;
        public Button equalsButton;

        [Header("Ações")]
        public Button useResultButton;
        public Button closeButton;
        public MinigameUI minigameUI;

        [Header("Comportamento")]
        public bool startOpen = false;

        const string NumberFormat = "0.######";

        string _entry = "";
        double _accumulator;
        char _pendingOp = '\0';
        bool _justEvaluated;
        bool _error;

        public bool IsOpen => rootPanel != null && rootPanel.activeSelf;

        /// <summary>Valor atualmente mostrado no visor.</summary>
        public double CurrentValue =>
            _entry.Length > 0 && double.TryParse(_entry, NumberStyles.Float, CultureInfo.InvariantCulture, out double v)
                ? v
                : _accumulator;

        void Start()
        {
            WireButtons();
            if (rootPanel != null) rootPanel.SetActive(startOpen);
            ClearAll();
        }

        void Update()
        {
            if (InputReader.CalculatorPressed) Toggle();
            if (!IsOpen) return;

            string key = InputReader.CalculatorKeyThisFrame;
            if (key.Length > 0) Press(key);
        }

        public void Toggle()
        {
            if (rootPanel == null) return;
            bool opening = !rootPanel.activeSelf;
            rootPanel.SetActive(opening);
            if (opening) Refresh();
        }

        public void Close()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
        }

        void WireButtons()
        {
            for (int i = 0; i < digitButtons.Length; i++)
            {
                if (digitButtons[i] == null) continue;
                string digit = i.ToString(CultureInfo.InvariantCulture);
                digitButtons[i].onClick.RemoveAllListeners();
                digitButtons[i].onClick.AddListener(() => Press(digit));
            }

            Bind(dotButton, ".");
            Bind(clearButton, "C");
            Bind(backspaceButton, "<");
            Bind(addButton, "+");
            Bind(subtractButton, "-");
            Bind(multiplyButton, "*");
            Bind(divideButton, "/");
            Bind(equalsButton, "=");

            if (useResultButton != null)
            {
                useResultButton.onClick.RemoveAllListeners();
                useResultButton.onClick.AddListener(UseResult);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }
        }

        void Bind(Button button, string key)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => Press(key));
        }

        /// <summary>Ponto único de entrada — os botões e o teclado passam por aqui.</summary>
        public void Press(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            if (key == "C") { ClearAll(); return; }

            if (_error && key != "C") { ClearAll(); }

            switch (key)
            {
                case "<":
                    if (_justEvaluated) { ClearEntry(); break; }
                    if (_entry.Length > 0) _entry = _entry.Substring(0, _entry.Length - 1);
                    break;

                case ".":
                    if (_justEvaluated) { _entry = "0."; _justEvaluated = false; break; }
                    if (_entry.Contains(".")) break;
                    _entry = _entry.Length == 0 ? "0." : _entry + ".";
                    break;

                case "+":
                case "-":
                case "*":
                case "/":
                    PushOperator(key[0]);
                    break;

                case "=":
                    DoEquals();
                    break;

                default:
                    if (key.Length == 1 && key[0] >= '0' && key[0] <= '9')
                    {
                        if (_justEvaluated) { _entry = ""; _justEvaluated = false; }
                        if (_entry.Length >= 12) break;
                        if (_entry == "0") _entry = "";
                        _entry += key;
                    }
                    break;
            }

            Refresh();
        }

        void PushOperator(char op)
        {
            double current = ReadEntryOrAccumulator();

            if (_pendingOp != '\0' && _entry.Length > 0)
                _accumulator = Apply(_accumulator, current, _pendingOp);
            else
                _accumulator = current;

            _pendingOp = op;
            _entry = "";
            _justEvaluated = false;
        }

        void DoEquals()
        {
            double current = ReadEntryOrAccumulator();

            if (_pendingOp != '\0')
            {
                _accumulator = Apply(_accumulator, current, _pendingOp);
                _pendingOp = '\0';
            }
            else
            {
                _accumulator = current;
            }

            _entry = _error ? "" : _accumulator.ToString(NumberFormat, CultureInfo.InvariantCulture);
            _justEvaluated = true;
        }

        double ReadEntryOrAccumulator()
        {
            if (_entry.Length > 0 &&
                double.TryParse(_entry, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                return v;
            return _accumulator;
        }

        double Apply(double a, double b, char op)
        {
            switch (op)
            {
                case '+': return a + b;
                case '-': return a - b;
                case '*': return a * b;
                case '/':
                    if (Math.Abs(b) < 1e-12) { _error = true; return 0d; }
                    return a / b;
                default: return b;
            }
        }

        void ClearAll()
        {
            _entry = "";
            _accumulator = 0d;
            _pendingOp = '\0';
            _justEvaluated = false;
            _error = false;
            Refresh();
        }

        void ClearEntry()
        {
            _entry = "";
            _justEvaluated = false;
        }

        void UseResult()
        {
            if (_error || minigameUI == null) return;

            // se o aluno esqueceu de apertar "=", resolve a conta pendente antes
            if (_pendingOp != '\0') { DoEquals(); Refresh(); }

            minigameUI.ApplyExternalValue((float)CurrentValue);
        }

        void Refresh()
        {
            if (displayText != null)
            {
                if (_error) displayText.text = "ERRO";
                else if (_entry.Length > 0) displayText.text = _entry;
                else displayText.text = _accumulator.ToString(NumberFormat, CultureInfo.InvariantCulture);
            }

            if (expressionText != null)
            {
                expressionText.text = _pendingOp == '\0'
                    ? ""
                    : $"{_accumulator.ToString(NumberFormat, CultureInfo.InvariantCulture)} {SymbolOf(_pendingOp)}";
            }
        }

        static string SymbolOf(char op)
        {
            switch (op)
            {
                case '*': return "x";
                case '/': return "/";
                default: return op.ToString();
            }
        }
    }
}
