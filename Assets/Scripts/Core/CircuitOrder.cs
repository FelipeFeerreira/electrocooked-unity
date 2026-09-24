using System;
using System.Collections.Generic;
using System.Linq;

namespace ElectroCooked.Core
{
    /// <summary>
    /// Um "pedido" da aula — equivalente ao ticket de receita do Overcooked.
    /// Diz quais peças físicas pegar (o quê montar na bancada) e, depois de montado,
    /// quais números usar na conta e o que precisa ser calculado.
    ///
    /// Design: os valores de V e R "existem" nas peças/no ticket (como um circuito
    /// de livro didático). Quando a grandeza pedida é a Resistência ou a Tensão,
    /// a história é "essa peça está sem etiqueta / é uma fonte sem visor" — por
    /// isso ela some do ticket e vira a incógnita, mesmo a peça física genérica
    /// ainda sendo colocada na bancada normalmente.
    ///
    /// REGRA DE OURO: o ticket é a fonte da verdade. Tudo que a conta do aluno
    /// precisa está escrito nele, e a resposta certa é derivada exatamente dos
    /// números escritos (não dos valores internos "de verdade"). Sem isso o
    /// arredondamento do amperímetro faz o aluno acertar a conta e o jogo dizer
    /// que ele errou.
    /// </summary>
    public class CircuitOrder
    {
        public string Id;
        public int RequiredResistorCount;      // quantas peças "Resistor" o jogador precisa buscar
        public bool RequiresBattery = true;    // sempre 1 bateria neste protótipo (série simples)
        public bool RequiresWire = true;       // sempre precisa fechar o circuito com fio

        public float BatteryVoltage;           // valor da bateria (pode estar oculto no ticket)
        public float[] ResistorValues;         // valores de cada resistor (pode ter 1 oculto)
        public int UnknownResistorIndex = -1;  // -1 = nenhum resistor é a incógnita

        public QuestionType Question;
        public bool VoltageIsUnknown;          // true só quando Question == ComputeVoltage
        public float? MeasuredCurrent;         // "leitura do amperímetro" mostrada no ticket, quando aplicável

        public float CorrectAnswer;
        public string Unit;                    // "A", "V", "Ohm" ou "W"
        public float TolerancePercent = CircuitMath.DefaultTolerancePercent;

        public MinigameStyle Style;
        public float[] MultipleChoiceOptions;  // preenchido só quando Style == MultipleChoice
        public float SliderMin, SliderMax;     // preenchido só quando Style == Slider

        public float TimeLimitSeconds;
        public float BaseGradeValue = 1f;      // quanto esse pedido vale na nota final

        public float SeriesResistanceTotal => ResistorValues.Sum();

        /// <summary>Formato usado tanto no ticket quanto na conferência da resposta.</summary>
        public const string CurrentFormat = "0.####";

        /// <summary>Linhas de texto prontas para a UI do ticket/minigame mostrar.</summary>
        public List<string> BuildTicketLines()
        {
            var lines = new List<string>();

            lines.Add(VoltageIsUnknown
                ? "V = ?   (fonte sem visor)"
                : $"V = {BatteryVoltage:0.##} V");

            for (int i = 0; i < ResistorValues.Length; i++)
            {
                lines.Add(i == UnknownResistorIndex
                    ? $"R{i + 1} = ?   (etiqueta rasgada)"
                    : $"R{i + 1} = {ResistorValues[i]:0.##} Ohm");
            }

            if (MeasuredCurrent.HasValue)
                lines.Add($"I = {MeasuredCurrent.Value.ToString(CurrentFormat)} A   (amperimetro)");

            lines.Add($"Calcule: {QuestionLabel()}  ({Unit})");
            return lines;
        }

        public string QuestionLabel()
        {
            switch (Question)
            {
                case QuestionType.ComputeCurrent: return "a Corrente (I)";
                case QuestionType.ComputeVoltage: return "a Tensao (V)";
                case QuestionType.ComputeResistance: return $"o Resistor R{UnknownResistorIndex + 1}";
                case QuestionType.ComputePower: return "a Potencia (P)";
                default: return "?";
            }
        }
    }

    /// <summary>
    /// Fábrica de pedidos. Puramente determinística a partir de um System.Random,
    /// o que permite gerar/testar milhares de pedidos fora da Unity para validar
    /// que a matemática nunca quebra.
    /// </summary>
    public static class CircuitOrderGenerator
    {
        static readonly float[] VoltagePool = { 6f, 9f, 12f, 24f };
        static readonly float[] ResistorPool = { 5f, 10f, 15f, 20f, 25f, 30f, 40f, 50f, 60f, 80f, 100f };

        // pesos de sorteio do tipo de pergunta POR nível de dificuldade.
        // Nível 1 quase só corrente (fácil); nível 3 puxa potência e tensão,
        // onde o aluno precisa encadear duas contas. Potência = cálculo V*I
        // com resposta maior, exige mais atenção.
        static readonly (QuestionType type, float weight)[][] QuestionWeightsByDifficulty =
        {
            new[] { (QuestionType.ComputeCurrent, 0.45f), (QuestionType.ComputeResistance, 0.25f),
                    (QuestionType.ComputeVoltage, 0.15f), (QuestionType.ComputePower, 0.15f) },
            new[] { (QuestionType.ComputeCurrent, 0.30f), (QuestionType.ComputeResistance, 0.25f),
                    (QuestionType.ComputeVoltage, 0.20f), (QuestionType.ComputePower, 0.25f) },
            new[] { (QuestionType.ComputeCurrent, 0.20f), (QuestionType.ComputeResistance, 0.20f),
                    (QuestionType.ComputeVoltage, 0.25f), (QuestionType.ComputePower, 0.35f) },
        };

        // pesos do estilo de resposta por nível: no começo sobra múltipla escolha
        // (ler a alternativa já resolve); conforme a aula avança sobe a digitação,
        // que é a mais rápida pra quem sabe a conta mas também a mais punitiva.
        static readonly (MinigameStyle style, float weight)[][] StyleWeightsByDifficulty =
        {
            new[] { (MinigameStyle.NumericInput, 0.30f), (MinigameStyle.MultipleChoice, 0.50f),
                    (MinigameStyle.Slider, 0.20f) },
            new[] { (MinigameStyle.NumericInput, 0.40f), (MinigameStyle.MultipleChoice, 0.35f),
                    (MinigameStyle.Slider, 0.25f) },
            new[] { (MinigameStyle.NumericInput, 0.50f), (MinigameStyle.MultipleChoice, 0.25f),
                    (MinigameStyle.Slider, 0.25f) },
        };

        // quanto do tempo-base cada nível dá: nível 1 folgado, nível 3 apertado
        static readonly float[] TimeScaleByDifficulty = { 1.15f, 1f, 0.85f };

        public static CircuitOrder Generate(System.Random rng, int difficultyLevel = 1)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            int difficulty = difficultyLevel < 1 ? 1 : (difficultyLevel > 3 ? 3 : difficultyLevel);
            float timeScale = TimeScaleByDifficulty[difficulty - 1];

            int resistorCount = difficulty <= 1 ? 2 : (difficulty == 2 ? (rng.Next(0, 2) == 0 ? 2 : 3) : 3);
            float voltage = VoltagePool[rng.Next(VoltagePool.Length)];
            float[] resistors = new float[resistorCount];
            for (int i = 0; i < resistorCount; i++)
                resistors[i] = ResistorPool[rng.Next(ResistorPool.Length)];

            float rTotal = CircuitMath.SeriesResistance(resistors);
            float iTotal = CircuitMath.Current(voltage, rTotal);

            // A corrente que o ticket MOSTRA é arredondada. Toda resposta derivada
            // dela usa esse mesmo valor arredondado, senão o aluno faz a conta certa
            // com o número da tela e o jogo reprova.
            float shownCurrent = RoundToDisplay(iTotal);

            var order = new CircuitOrder
            {
                Id = Guid.NewGuid().ToString("N").Substring(0, 6),
                RequiredResistorCount = resistorCount,
                BatteryVoltage = voltage,
                ResistorValues = resistors,
                Question = PickQuestionType(rng, difficulty),
                Style = PickStyle(rng, difficulty),
                TimeLimitSeconds = 45f * timeScale,
                BaseGradeValue = 1f + (resistorCount - 2) * 0.25f
            };

            switch (order.Question)
            {
                case QuestionType.ComputeCurrent:
                    // V e todos os R estão no ticket: a resposta é exata.
                    order.CorrectAnswer = iTotal;
                    order.Unit = "A";
                    order.TimeLimitSeconds = 40f * timeScale;
                    break;

                case QuestionType.ComputePower:
                    // V e todos os R estão no ticket: P = V * (V / Rtotal), exato.
                    order.CorrectAnswer = CircuitMath.Power(voltage, iTotal);
                    order.Unit = "W";
                    order.TimeLimitSeconds = 50f * timeScale;
                    break;

                case QuestionType.ComputeResistance:
                {
                    order.UnknownResistorIndex = rng.Next(resistorCount);
                    order.MeasuredCurrent = shownCurrent;

                    // Rtotal vem do que está na tela (V / I_mostrado); o resistor
                    // desconhecido é o que sobra depois de tirar os outros.
                    float othersSum = 0f;
                    for (int i = 0; i < resistorCount; i++)
                        if (i != order.UnknownResistorIndex) othersSum += resistors[i];

                    order.CorrectAnswer = (voltage / shownCurrent) - othersSum;
                    order.Unit = "Ohm";
                    order.TimeLimitSeconds = 55f * timeScale;
                    break;
                }

                case QuestionType.ComputeVoltage:
                {
                    order.VoltageIsUnknown = true;
                    order.MeasuredCurrent = shownCurrent;
                    // V = I_mostrado * Rtotal (Rtotal está todo visível no ticket)
                    order.CorrectAnswer = shownCurrent * rTotal;
                    order.Unit = "V";
                    order.TimeLimitSeconds = 45f * timeScale;
                    break;
                }
            }

            if (order.Style == MinigameStyle.MultipleChoice)
                order.MultipleChoiceOptions = BuildDistractors(rng, order.CorrectAnswer);
            else if (order.Style == MinigameStyle.Slider)
                BuildSliderRange(order);

            return order;
        }

        /// <summary>
        /// Arredonda para 4 algarismos significativos — a mesma precisão que o
        /// formato "0.####" do ticket consegue mostrar para as correntes deste jogo.
        /// </summary>
        public static float RoundToDisplay(float value)
        {
            if (value == 0f) return 0f;
            double v = value;
            int decimals = 4;
            double abs = Math.Abs(v);
            if (abs >= 1000) decimals = 0;
            else if (abs >= 100) decimals = 1;
            else if (abs >= 10) decimals = 2;
            else if (abs >= 1) decimals = 3;
            return (float)Math.Round(v, decimals, MidpointRounding.AwayFromZero);
        }

        static QuestionType PickQuestionType(System.Random rng, int difficulty)
        {
            var weights = QuestionWeightsByDifficulty[difficulty - 1];
            float roll = (float)rng.NextDouble();
            float cumulative = 0f;
            foreach (var (type, weight) in weights)
            {
                cumulative += weight;
                if (roll <= cumulative) return type;
            }
            return weights[0].type;
        }

        static MinigameStyle PickStyle(System.Random rng, int difficulty)
        {
            var weights = StyleWeightsByDifficulty[difficulty - 1];
            float roll = (float)rng.NextDouble();
            float cumulative = 0f;
            foreach (var (style, weight) in weights)
            {
                cumulative += weight;
                if (roll <= cumulative) return style;
            }
            return weights[0].style;
        }

        static float[] BuildDistractors(System.Random rng, float correct)
        {
            float correctRounded = RoundNice(correct);
            var options = new List<float> { correctRounded };

            // erros clássicos de aluno: inverter a divisão, esquecer um resistor,
            // dobrar/dividir por dois. Só entram se não colidirem com a resposta
            // certa dentro da tolerância (senão haveria duas alternativas certas).
            float[] candidateFactors = { 0.5f, 2f, 1.3f, 0.7f, 1.6f, 0.45f };
            foreach (float factor in candidateFactors)
            {
                if (options.Count >= 4) break;
                TryAdd(options, RoundNice(correct * factor), correct);
            }

            int guard = 0;
            while (options.Count < 4 && guard++ < 64)
            {
                float jitter = correct * (0.25f + (float)rng.NextDouble() * 0.7f) * (rng.Next(0, 2) == 0 ? 1 : -1);
                TryAdd(options, RoundNice(correct + jitter), correct);
            }
            while (options.Count < 4) // rede de segurança: nunca devolver menos de 4
                options.Add(RoundNice(correct * (options.Count + 2)));

            Shuffle(rng, options);
            return options.ToArray();
        }

        static void TryAdd(List<float> options, float candidate, float correct)
        {
            if (candidate <= 0f) return;
            if (CircuitMath.IsAnswerCorrect(candidate, correct)) return; // seria uma segunda resposta certa
            foreach (float existing in options)
                if (Math.Abs(existing - candidate) < 0.0001f) return;    // duplicada
            options.Add(candidate);
        }

        static void BuildSliderRange(CircuitOrder order)
        {
            float span = Math.Max(order.CorrectAnswer * 1.2f, 1f);
            order.SliderMin = Math.Max(0f, order.CorrectAnswer - span);
            order.SliderMax = order.CorrectAnswer + span;
        }

        static float RoundNice(float value)
        {
            return (float)Math.Round(value, value < 1 ? 3 : (value < 10 ? 2 : 1), MidpointRounding.AwayFromZero);
        }

        static void Shuffle<T>(System.Random rng, List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
