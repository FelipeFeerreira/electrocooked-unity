using System;
using ElectroCooked.Circuit;
using ElectroCooked.Core;
using UnityEngine;

namespace ElectroCooked.Minigame
{
    /// <summary>
    /// Orquestra o desafio de cálculo (VRI/potência) depois que uma bancada é
    /// montada corretamente. A UI (MinigameUI) escuta os eventos daqui e chama
    /// SubmitNumericAnswer / SubmitMultipleChoiceAnswer conforme o estilo sorteado
    /// no CircuitOrder (teclado numérico, múltipla escolha ou régua/slider).
    /// </summary>
    public class MinigameManager : MonoBehaviour
    {
        public static MinigameManager Instance { get; private set; }

        public CircuitOrder CurrentOrder { get; private set; }
        public float TimeRemaining { get; private set; }
        public bool IsActive { get; private set; }

        CircuitStation _currentStation;

        public event Action<CircuitStation, CircuitOrder> OnChallengeStarted;
        public event Action<float, float> OnChallengeTick;  // (tempoRestante, tempoTotal)
        public event Action<bool, float> OnChallengeEnded;  // (acertou, respostaCerta)

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (!IsActive || CurrentOrder == null) return;

            TimeRemaining -= Time.deltaTime;
            OnChallengeTick?.Invoke(Mathf.Max(0f, TimeRemaining), CurrentOrder.TimeLimitSeconds);

            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                FinishChallenge(false); // acabou o tempo = conta errada = explode
            }
        }

        public void BeginChallenge(CircuitStation station, CircuitOrder order)
        {
            if (station == null || order == null) return;

            _currentStation = station;
            CurrentOrder = order;
            TimeRemaining = order.TimeLimitSeconds;
            IsActive = true;
            OnChallengeStarted?.Invoke(station, order);
        }

        /// <summary>Usado pelos estilos NumericInput (teclado numérico) e Slider.</summary>
        public void SubmitNumericAnswer(float value)
        {
            if (!IsActive || CurrentOrder == null) return;
            bool correct = CircuitMath.IsAnswerCorrect(value, CurrentOrder.CorrectAnswer, CurrentOrder.TolerancePercent);
            FinishChallenge(correct);
        }

        /// <summary>Usado pelo estilo MultipleChoice — índice dentro de CurrentOrder.MultipleChoiceOptions.</summary>
        public void SubmitMultipleChoiceAnswer(int optionIndex)
        {
            if (!IsActive || CurrentOrder?.MultipleChoiceOptions == null) return;
            if (optionIndex < 0 || optionIndex >= CurrentOrder.MultipleChoiceOptions.Length) return;

            float value = CurrentOrder.MultipleChoiceOptions[optionIndex];
            bool correct = CircuitMath.IsAnswerCorrect(value, CurrentOrder.CorrectAnswer, CurrentOrder.TolerancePercent);
            FinishChallenge(correct);
        }

        void FinishChallenge(bool correct)
        {
            IsActive = false;

            CircuitOrder order = CurrentOrder;
            CircuitStation station = _currentStation;
            _currentStation = null;
            CurrentOrder = null;

            float speedBonus01 = order != null && order.TimeLimitSeconds > 0f
                ? Mathf.Clamp01(TimeRemaining / order.TimeLimitSeconds)
                : 0f;

            OnChallengeEnded?.Invoke(correct, order?.CorrectAnswer ?? 0f);

            if (station == null) return;
            if (correct) station.CompleteOrder(speedBonus01);
            else station.FailOrder();
        }
    }
}
