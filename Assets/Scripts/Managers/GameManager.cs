using System;
using ElectroCooked.Core;
using UnityEngine;

namespace ElectroCooked.Managers
{
    /// <summary>
    /// Controla o "período de aula": duração, dificuldade crescente ao longo do
    /// tempo e o resultado final — igual ao cronômetro de nível do Overcooked.
    /// No fim da aula grava a nota no boletim do aluno, que o corredor da escola
    /// lê depois.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum ClassState { NotStarted, Running, Ended }

        [Header("Identidade da aula")]
        public string subjectId = SubjectIds.Circuits;
        public string subjectName = "CIRCUITOS I";

        [Header("Duração da aula")]
        public float classDurationSeconds = 300f; // 5 minutos por padrão

        [Header("Dificuldade crescente")]
        [Tooltip("A cada X segundos, a dificuldade sobe 1 nível (até o nível 3).")]
        public float secondsPerDifficultyStep = 90f;

        public ClassState State { get; private set; } = ClassState.NotStarted;
        public float TimeRemaining { get; private set; }

        /// <summary>Nível de dificuldade atual da aula, derivado do tempo decorrido.</summary>
        public int CurrentDifficultyLevel
            => State == ClassState.Running
                ? Mathf.Clamp(1 + Mathf.FloorToInt(_elapsed / secondsPerDifficultyStep), 1, 3)
                : 1;

        public event Action OnClassStarted;
        public event Action<float, float> OnClassTimeChanged; // (restante, total)
        public event Action<float> OnClassEnded;              // nota final (0-10)

        float _elapsed;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            StartClass();
        }

        public void StartClass()
        {
            State = ClassState.Running;
            TimeRemaining = classDurationSeconds;
            _elapsed = 0f;
            OrderManager.Instance?.SetDifficulty(1);
            OnClassStarted?.Invoke();
        }

        void Update()
        {
            if (State != ClassState.Running) return;

            _elapsed += Time.deltaTime;
            TimeRemaining = Mathf.Max(0f, classDurationSeconds - _elapsed);
            OnClassTimeChanged?.Invoke(TimeRemaining, classDurationSeconds);

            int expectedLevel = 1 + Mathf.FloorToInt(_elapsed / secondsPerDifficultyStep);
            OrderManager.Instance?.SetDifficulty(Mathf.Clamp(expectedLevel, 1, 3));

            if (TimeRemaining <= 0f)
                EndClass();
        }

        public void EndClass()
        {
            if (State == ClassState.Ended) return;
            State = ClassState.Ended;

            float finalGrade = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentGrade : 0f;
            StudentRecord.SaveGrade(subjectId, finalGrade);

            OnClassEnded?.Invoke(finalGrade);
        }
    }
}
