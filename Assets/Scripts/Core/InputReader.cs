using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ElectroCooked.Core
{
    /// <summary>
    /// Camada fina de leitura de input. O projeto está configurado com o
    /// Input System novo (Project Settings > Player > Active Input Handling),
    /// onde a classe UnityEngine.Input antiga lança exceção. Todo o jogo lê
    /// input por aqui, então trocar o esquema de controle é mexer num arquivo só.
    ///
    /// Controles: WASD / setas / analógico esquerdo para andar,
    /// E (ou botão sul do controle) para pegar/usar,
    /// Espaço / botão esquerdo do mouse / gatilho direito para o extintor.
    /// </summary>
    public static class InputReader
    {
        /// <summary>Direção de caminhada, já normalizada (x = direita, y = frente).</summary>
        public static Vector2 Move
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Vector2 v = Vector2.zero;
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
                }

                var pad = Gamepad.current;
                if (pad != null && v.sqrMagnitude < 0.01f)
                    v = pad.leftStick.ReadValue();

                return v.sqrMagnitude > 1f ? v.normalized : v;
#else
                Vector2 v = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                return v.sqrMagnitude > 1f ? v.normalized : v;
#endif
            }
        }

        /// <summary>Pegar peça / encaixar na bancada. Dispara uma vez por toque.</summary>
        public static bool InteractPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && kb.eKey.wasPressedThisFrame) return true;

                var pad = Gamepad.current;
                if (pad != null && pad.buttonSouth.wasPressedThisFrame) return true;

                return false;
#else
                return Input.GetKeyDown(KeyCode.E);
#endif
            }
        }

        /// <summary>Segurar para usar o extintor.</summary>
        public static bool UseHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && kb.spaceKey.isPressed) return true;

                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.isPressed) return true;

                var pad = Gamepad.current;
                if (pad != null && pad.rightTrigger.ReadValue() > 0.3f) return true;

                return false;
#else
                return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Mouse0);
#endif
            }
        }

        /// <summary>ENTER (ou botão leste) confirma — no fim da aula, volta para a escola.</summary>
        public static bool ConfirmPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                    return true;

                var pad = Gamepad.current;
                if (pad != null && pad.buttonEast.wasPressedThisFrame) return true;

                return false;
#else
                return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
            }
        }

        /// <summary>C (ou botão oeste do controle) abre/fecha a calculadora.</summary>
        public static bool CalculatorPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && kb.cKey.wasPressedThisFrame) return true;

                var pad = Gamepad.current;
                if (pad != null && pad.buttonWest.wasPressedThisFrame) return true;

                return false;
#else
                return Input.GetKeyDown(KeyCode.C);
#endif
            }
        }

        /// <summary>F (ou botão norte do controle) abre/fecha o formulário de V, R, I e P.</summary>
        public static bool FormulasPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && kb.fKey.wasPressedThisFrame) return true;

                var pad = Gamepad.current;
                if (pad != null && pad.buttonNorth.wasPressedThisFrame) return true;

                return false;
#else
                return Input.GetKeyDown(KeyCode.F);
#endif
            }
        }

        /// <summary>
        /// Tecla de calculadora apertada neste frame, já normalizada:
        /// "0".."9", ".", "+", "-", "*", "/", "=" , "&lt;" (apagar) ou "C" (limpar).
        /// Devolve "" quando nada foi apertado. Deixa a calculadora usável no
        /// teclado numérico, que é muito mais rápido que clicar botão por botão.
        /// </summary>
        public static string CalculatorKeyThisFrame
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb == null) return "";

                if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame) return "0";
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) return "1";
                if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) return "2";
                if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) return "3";
                if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) return "4";
                if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) return "5";
                if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) return "6";
                if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame) return "7";
                if (kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame) return "8";
                if (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame) return "9";

                // vírgula também vale como separador decimal (teclado brasileiro)
                if (kb.periodKey.wasPressedThisFrame || kb.numpadPeriodKey.wasPressedThisFrame
                    || kb.commaKey.wasPressedThisFrame) return ".";

                if (kb.numpadPlusKey.wasPressedThisFrame) return "+";
                if (kb.numpadMinusKey.wasPressedThisFrame || kb.minusKey.wasPressedThisFrame) return "-";
                if (kb.numpadMultiplyKey.wasPressedThisFrame) return "*";
                if (kb.numpadDivideKey.wasPressedThisFrame || kb.slashKey.wasPressedThisFrame) return "/";

                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame
                    || kb.equalsKey.wasPressedThisFrame) return "=";

                if (kb.backspaceKey.wasPressedThisFrame) return "<";
                if (kb.deleteKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame) return "C";

                return "";
#else
                foreach (char c in Input.inputString)
                {
                    if (c >= '0' && c <= '9') return c.ToString();
                    if (c == '.' || c == ',') return ".";
                    if (c == '+' || c == '-' || c == '*' || c == '/') return c.ToString();
                    if (c == '=' || c == '\n' || c == '\r') return "=";
                    if (c == '\b') return "<";
                }
                return "";
#endif
            }
        }

        /// <summary>R (ou Start) para recomeçar a aula depois do resumo final.</summary>
        public static bool RestartPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && kb.rKey.wasPressedThisFrame) return true;

                var pad = Gamepad.current;
                if (pad != null && pad.startButton.wasPressedThisFrame) return true;

                return false;
#else
                return Input.GetKeyDown(KeyCode.R);
#endif
            }
        }
    }
}
