using ElectroCooked.Core;
using ElectroCooked.Managers;
using UnityEngine;

namespace ElectroCooked.Calculus
{
    /// <summary>
    /// Controla o minigame de Cálculo: sorteia pergunta, deixa 4 zonas de resposta
    /// no chão, detecta a zona em que o aluno pisou (por distância, sem trigger de
    /// física) e converte o resultado em pontos/perda de vida. Quando as vidas
    /// acabam, encerra a aula pela GameManager.
    /// </summary>
    public class CalculusGameManager : MonoBehaviour
    {
        public static CalculusGameManager Instance { get; private set; }

        [Header("Referências")]
        [Tooltip("Transform do Aluno, usado para detectar em qual zona ele entrou.")]
        public Transform player;
        public AnswerZone[] zones = new AnswerZone[4];
        public CalculusHUD hud;

        [Header("Balanceamento")]
        public int startingLives = 3;
        public float zoneRadius = 1.05f;
        public float delayBetweenQuestions = 1.2f;
        public float questionGraceSeconds = 0.5f;
        public float[] timePerDifficulty = { 22f, 18f, 14f };
        public float gradePerQuestion = 0.9f;

        System.Random _rng = new System.Random();

        CalculusQuestion _current;
        bool _hasQuestion;
        bool _awaitingFeedback;
        float _feedbackTimer;
        float _questionTimeRemaining;
        float _questionGraceRemaining;
        int _lives;
        int _difficulty;

        public int Lives => _lives;
        public CalculusQuestion CurrentQuestion => _current;
        public float QuestionTimeRatio => _hasQuestion && _current != null
            ? Mathf.Clamp01(_questionTimeRemaining / _current.TimeLimitSeconds)
            : 0f;

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
            _lives = startingLives;
            if (hud != null) hud.SetLives(_lives);
            NextQuestion();
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.State == GameManager.ClassState.Ended)
                return;

            if (_awaitingFeedback)
            {
                _feedbackTimer -= Time.deltaTime;
                if (_feedbackTimer <= 0f)
                {
                    _awaitingFeedback = false;
                    NextQuestion();
                }
                return;
            }

            if (!_hasQuestion || _current == null) return;

            if (_questionGraceRemaining > 0f)
            {
                _questionGraceRemaining = Mathf.Max(0f, _questionGraceRemaining - Time.deltaTime);
                return;
            }

            _questionTimeRemaining -= Time.deltaTime;
            if (hud != null)
                hud.TickTimer(Mathf.Max(0f, _questionTimeRemaining), _current.TimeLimitSeconds);

            if (_questionTimeRemaining <= 0f)
            {
                Resolve(null, true);
                return;
            }

            if (player == null || zones == null) return;
            foreach (AnswerZone zone in zones)
            {
                if (zone == null) continue;
                if (zone.IsInside(player.position, zoneRadius))
                {
                    Resolve(zone, false);
                    return;
                }
            }
        }

        void Resolve(AnswerZone zone, bool timedOut)
        {
            bool correct = !timedOut && zone != null && zone.IsCorrect;
            _hasQuestion = false;
            _awaitingFeedback = true;
            _feedbackTimer = delayBetweenQuestions;

            if (zones != null)
                foreach (AnswerZone z in zones)
                    if (z != null) z.ResetVisual();

            if (correct)
            {
                float speedBonus = 0.4f + 0.6f * QuestionTimeRatio;
                ScoreManager.Instance?.AddGrade(gradePerQuestion * speedBonus);
                zone.Flash(true);
                if (hud != null) hud.ShowFeedback("ACERTOU!", hud.feedbackCorrect);
            }
            else
            {
                ScoreManager.Instance?.RegisterMistake();
                if (zone != null) zone.Flash(false);
                _lives--;
                if (hud != null)
                {
                    hud.SetLives(_lives);
                    hud.ShowFeedback(timedOut ? "TEMPO ESGOTADO!" : "ERRADO!", hud.feedbackWrong);
                }

                if (_lives <= 0)
                {
                    if (hud != null) hud.ShowFeedback("AULA ENCERRADA", hud.feedbackWrong);
                    if (GameManager.Instance != null) GameManager.Instance.EndClass();
                    return;
                }
            }
        }

        void NextQuestion()
        {
            if (GameManager.Instance != null && GameManager.Instance.State == GameManager.ClassState.Ended) return;
            if (_lives <= 0) return;

            _difficulty = GameManager.Instance != null ? GameManager.Instance.CurrentDifficultyLevel : 1;
            _current = CalculusQuestionGenerator.Generate(_rng, _difficulty);
            _current.TimeLimitSeconds = timePerDifficulty[Mathf.Clamp(_difficulty - 1, 0, timePerDifficulty.Length - 1)];
            _questionTimeRemaining = _current.TimeLimitSeconds;
            _questionGraceRemaining = questionGraceSeconds;
            _hasQuestion = true;

            if (zones != null)
            {
                for (int i = 0; i < zones.Length; i++)
                {
                    if (zones[i] == null) continue;
                    bool hasOption = i < _current.Options.Length;
                    string text = hasOption ? _current.Options[i] : "";
                    bool isCorrect = hasOption && _current.Options[i] == _current.CorrectAnswer;
                    zones[i].Bind(text, isCorrect, hasOption);
                }
            }

            if (hud != null) hud.ShowQuestion(_current);
        }
    }
}