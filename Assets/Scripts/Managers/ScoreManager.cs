using System;
using UnityEngine;

namespace ElectroCooked.Managers
{
    /// <summary>
    /// Converte pedidos entregues em "nota da aula" (escala 0-10), igual ao placar
    /// de pontos do Overcooked, só que aqui é a nota do aluno na disciplina.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Balanceamento")]
        [Tooltip("Quantos pontos brutos equivalem à nota máxima (10).")]
        public float pointsForFullGrade = 8f;
        [Tooltip("Pontos perdidos cada vez que o aluno erra.")]
        public float penaltyPerMistake = 0.25f;
        [Tooltip("Maior multiplicador de combo que o streak pode alcançar.")]
        public int maxComboMultiplier = 4;

        public float RawPoints { get; private set; }
        public int DeliveredCount { get; private set; }
        public int MistakeCount { get; private set; }
        /// <summary>Acertos em sequência sem errar.</summary>
        public int Streak { get; private set; }
        /// <summary>Multiplicador de pontos atual (1-4): sobe a cada 3 acertos.</summary>
        public int ComboMultiplier => Mathf.Min(1 + Streak / 3, maxComboMultiplier);

        public float CurrentGrade => Mathf.Clamp(RawPoints / Mathf.Max(0.01f, pointsForFullGrade) * 10f, 0f, 10f);

        public event Action<float> OnGradeChanged; // nota atual (0-10)
        public event Action<int> OnMistake;        // total de erros até agora
        public event Action<int> OnComboChanged;   // multiplicador de combo atual

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void AddGrade(float rawPointsEarned)
        {
            Streak++;
            rawPointsEarned *= ComboMultiplier;
            RawPoints += Mathf.Max(0f, rawPointsEarned);
            DeliveredCount++;
            OnComboChanged?.Invoke(ComboMultiplier);
            OnGradeChanged?.Invoke(CurrentGrade);
        }

        public void RegisterMistake()
        {
            MistakeCount++;
            Streak = 0;
            RawPoints = Mathf.Max(0f, RawPoints - penaltyPerMistake);
            OnComboChanged?.Invoke(ComboMultiplier);
            OnMistake?.Invoke(MistakeCount);
            OnGradeChanged?.Invoke(CurrentGrade);
        }

        public void ResetScore()
        {
            RawPoints = 0f;
            DeliveredCount = 0;
            MistakeCount = 0;
            Streak = 0;
            OnComboChanged?.Invoke(ComboMultiplier);
            OnGradeChanged?.Invoke(CurrentGrade);
        }
    }
}
