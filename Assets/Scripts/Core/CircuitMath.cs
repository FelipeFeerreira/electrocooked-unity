using System;
using System.Linq;

namespace ElectroCooked.Core
{
    /// <summary>
    /// Toda a matemática do jogo mora aqui, isolada e sem depender da UnityEngine.
    /// Isso permite testar as fórmulas fora do Unity (ex: com um script Python/console
    /// espelhando a mesma lógica) antes de confiar nelas dentro do jogo.
    ///
    /// Convenção de unidades: Volts (V), Ohms (Ω), Ampères (A), Watts (W).
    /// Circuito considerado: série simples (1 bateria + N resistores + fios).
    /// </summary>
    public static class CircuitMath
    {
        public const float DefaultTolerancePercent = 0.08f; // 8% de margem de erro aceito

        /// <summary>Resistência equivalente de resistores em série: soma simples.</summary>
        public static float SeriesResistance(float[] resistorsOhms)
        {
            if (resistorsOhms == null || resistorsOhms.Length == 0)
                throw new ArgumentException("Precisa de ao menos um resistor.");
            return resistorsOhms.Sum();
        }

        /// <summary>Lei de Ohm: I = V / R</summary>
        public static float Current(float voltage, float resistance)
        {
            if (resistance <= 0f) throw new ArgumentException("Resistência deve ser > 0.");
            return voltage / resistance;
        }

        /// <summary>Lei de Ohm: V = I * R</summary>
        public static float Voltage(float current, float resistance)
        {
            return current * resistance;
        }

        /// <summary>Lei de Ohm: R = V / I</summary>
        public static float Resistance(float voltage, float current)
        {
            if (current <= 0f) throw new ArgumentException("Corrente deve ser > 0.");
            return voltage / current;
        }

        /// <summary>Potência: P = V * I</summary>
        public static float Power(float voltage, float current)
        {
            return voltage * current;
        }

        /// <summary>Potência alternativa: P = I² * R (útil para conferência cruzada)</summary>
        public static float PowerFromCurrentAndResistance(float current, float resistance)
        {
            return current * current * resistance;
        }

        /// <summary>Potência alternativa: P = V² / R (útil para conferência cruzada)</summary>
        public static float PowerFromVoltageAndResistance(float voltage, float resistance)
        {
            if (resistance <= 0f) throw new ArgumentException("Resistência deve ser > 0.");
            return (voltage * voltage) / resistance;
        }

        /// <summary>
        /// Verifica se a resposta do aluno está dentro da tolerância aceitável.
        /// Usa erro percentual (bom para valores de ordens de grandeza diferentes,
        /// como 0.4 A vs 220 Ω).
        /// </summary>
        public static bool IsAnswerCorrect(float studentAnswer, float correctAnswer, float tolerancePercent = DefaultTolerancePercent)
        {
            if (correctAnswer == 0f)
                return Math.Abs(studentAnswer) < 0.001f;

            float error = Math.Abs(studentAnswer - correctAnswer) / Math.Abs(correctAnswer);
            return error <= tolerancePercent;
        }
    }
}
