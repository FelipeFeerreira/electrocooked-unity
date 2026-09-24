using System;
using System.Collections.Generic;

namespace ElectroCooked.Calculus
{
    /// <summary>
    /// Uma pergunta de Cálculo Básico estilo Bully: o professor mostra o enunciado,
    /// o aluno corre até o círculo do chão com a resposta certa.
    /// </summary>
    public class CalculusQuestion
    {
        public string Prompt;
        public string CorrectAnswer;
        public string[] Options;
        public int Difficulty = 1;
        public float TimeLimitSeconds = 20f;
    }

    /// <summary>
    /// Fábrica de perguntas de cálculo. Puramente determinística a partir de um
    /// System.Random (igual ao CircuitOrderGenerator), com resposta sempre entre 4
    /// opções distintas e notação ASCII pura para não depender de fonte/unicode.
    /// </summary>
    public static class CalculusQuestionGenerator
    {
        enum Kind
        {
            PowerDerivative,
            PowerAntiderivative,
            ConstDerivative,
            TrigDerivative,
            TrigAntiderivative,
            ExpDerivative,
            ExpAntiderivative,
            ChainSin,
            ChainCos,
            ChainExp,
            ChainExpIntegral,
            ProductXSin,
            ProductXExp,
            PowerChainSquare,
        }

        static readonly Kind[] Difficulty1Kinds =
        {
            Kind.PowerDerivative, Kind.PowerAntiderivative, Kind.ConstDerivative,
            Kind.TrigDerivative, Kind.TrigAntiderivative,
        };

        static readonly Kind[] Difficulty2Kinds =
        {
            Kind.PowerDerivative, Kind.PowerAntiderivative, Kind.TrigDerivative,
            Kind.TrigAntiderivative, Kind.ExpDerivative, Kind.ExpAntiderivative,
            Kind.ChainSin, Kind.ChainCos,
        };

        static readonly Kind[] Difficulty3Kinds =
        {
            Kind.PowerDerivative, Kind.PowerAntiderivative, Kind.ChainSin, Kind.ChainCos,
            Kind.ChainExp, Kind.ChainExpIntegral, Kind.ProductXSin, Kind.ProductXExp,
            Kind.PowerChainSquare,
        };

        public static CalculusQuestion Generate(System.Random rng, int difficultyLevel)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            int difficulty = difficultyLevel < 1 ? 1 : (difficultyLevel > 3 ? 3 : difficultyLevel);
            Kind[] pool = difficulty == 1 ? Difficulty1Kinds
                : difficulty == 2 ? Difficulty2Kinds
                : Difficulty3Kinds;

            Kind kind = pool[rng.Next(pool.Length)];
            return Build(rng, kind, difficulty);
        }

        static CalculusQuestion Build(System.Random rng, Kind kind, int difficulty)
        {
            switch (kind)
            {
                case Kind.PowerDerivative: return PowerDerivative(rng, difficulty);
                case Kind.PowerAntiderivative: return PowerAntiderivative(rng, difficulty);
                case Kind.ConstDerivative: return ConstDerivative(rng);
                case Kind.TrigDerivative: return TrigDerivative(rng);
                case Kind.TrigAntiderivative: return TrigAntiderivative(rng);
                case Kind.ExpDerivative: return ExpDerivative(rng);
                case Kind.ExpAntiderivative: return ExpAntiderivative(rng);
                case Kind.ChainSin: return ChainSine(rng);
                case Kind.ChainCos: return ChainCos(rng);
                case Kind.ChainExp: return ChainExp(rng);
                case Kind.ChainExpIntegral: return ChainExpIntegral(rng);
                case Kind.ProductXSin: return ProductXSinus(rng);
                case Kind.ProductXExp: return ProductXExponential(rng);
                case Kind.PowerChainSquare: return PowerChainSquare(rng);
                default: return PowerDerivative(rng, difficulty);
            }
        }

        static CalculusQuestion PowerDerivative(System.Random rng, int difficulty)
        {
            int a = Roll(rng, difficulty >= 3 ? new[] { 2, 3, 4, 5 } : new[] { 2, 3, 4, 5, 6 });
            int n = difficulty == 1 ? Roll(rng, new[] { 1, 2 })
                : difficulty == 2 ? Roll(rng, new[] { 2, 3, 4 })
                : Roll(rng, new[] { 3, 4, 5 });

            string correct = Mono(a * n, n - 1);
            var candidates = new List<string>
            {
                Mono(a, n),
                Mono(a, n + 1),
                Mono(a * n, n),
                n >= 2 ? Mono(a * n, n - 2) : Mono(a + n, n - 1),
            };
            return Finish(rng, $"d/dx [ {Mono(a, n)} ]", correct, candidates, difficulty);
        }

        static CalculusQuestion PowerAntiderivative(System.Random rng, int difficulty)
        {
            int n = difficulty == 1 ? Roll(rng, new[] { 0, 1 })
                : difficulty == 2 ? Roll(rng, new[] { 2, 3 })
                : Roll(rng, new[] { 3, 4 });
            int b = Roll(rng, new[] { 1, 2, 3 });
            int a = b * (n + 1);

            string correct = Mono(b, n + 1) + " + C";
            var candidates = new List<string>();
            if (n >= 1)
            {
                candidates.Add(Mono(a, n + 1) + " + C");
                candidates.Add(Mono(a, n) + " + C");
                candidates.Add(Mono(b, n + 1));
                candidates.Add(Mono(b, n) + " + C");
            }
            else
            {
                candidates.Add(a.ToString());
                candidates.Add(Mono(a, 1));
                candidates.Add(a + " + C");
                candidates.Add("x + C");
            }
            return Finish(rng, $"int {Mono(a, n)} dx", correct, candidates, difficulty);
        }

        static CalculusQuestion ConstDerivative(System.Random rng)
        {
            int c = Roll(rng, new[] { 5, 7, 9 });
            string correct = "0";
            var candidates = new List<string> { c.ToString(), "1", "x", c + " x" };
            return Finish(rng, $"d/dx [ {c} ]", correct, candidates, 1);
        }

        static CalculusQuestion TrigDerivative(System.Random rng)
        {
            bool sine = rng.Next(2) == 0;
            if (sine)
            {
                var candidates = new List<string> { "-cos(x)", "sin(x)", "-sin(x)" };
                return Finish(rng, "d/dx [ sin(x) ]", "cos(x)", candidates, 1);
            }
            var cosCandidates = new List<string> { "sin(x)", "cos(x)", "-cos(x)" };
            return Finish(rng, "d/dx [ cos(x) ]", "-sin(x)", cosCandidates, 1);
        }

        static CalculusQuestion TrigAntiderivative(System.Random rng)
        {
            bool sine = rng.Next(2) == 0;
            if (sine)
            {
                var candidates = new List<string> { "cos(x) + C", "sin(x) + C", "-sin(x) + C", "cos(x)" };
                return Finish(rng, "int sin(x) dx", "-cos(x) + C", candidates, 1);
            }
            var cosCandidates = new List<string> { "cos(x) + C", "-sin(x) + C", "sin(x)", "-cos(x) + C" };
            return Finish(rng, "int cos(x) dx", "sin(x) + C", cosCandidates, 1);
        }

        static CalculusQuestion ExpDerivative(System.Random rng)
        {
            var candidates = new List<string> { "x e^x", "e^(x - 1)", "0" };
            return Finish(rng, "d/dx [ e^x ]", "e^x", candidates, 2);
        }

        static CalculusQuestion ExpAntiderivative(System.Random rng)
        {
            var candidates = new List<string> { "e^x", "x e^x + C", "e^(x + 1) + C" };
            return Finish(rng, "int e^x dx", "e^x + C", candidates, 2);
        }

        static CalculusQuestion ChainSine(System.Random rng)
        {
            int k = Roll(rng, new[] { 2, 3 });
            string correct = $"{k} cos({k}x)";
            var candidates = new List<string>
            {
                $"cos({k}x)",
                $"-{k} cos({k}x)",
                $"{k} sin({k}x)",
            };
            return Finish(rng, $"d/dx [ sin({k}x) ]", correct, candidates, 2);
        }

        static CalculusQuestion ChainCos(System.Random rng)
        {
            int k = Roll(rng, new[] { 2, 3 });
            string correct = $"-{k} sin({k}x)";
            var candidates = new List<string>
            {
                $"{k} sin({k}x)",
                $"sin({k}x)",
                $"-{k} cos({k}x)",
            };
            return Finish(rng, $"d/dx [ cos({k}x) ]", correct, candidates, 2);
        }

        static CalculusQuestion ChainExp(System.Random rng)
        {
            int k = Roll(rng, new[] { 2, 3 });
            string correct = $"{k} e^({k}x)";
            var candidates = new List<string>
            {
                $"e^({k}x)",
                $"-{k} e^({k}x)",
                $"{k} e^x",
            };
            return Finish(rng, $"d/dx [ e^({k}x) ]", correct, candidates, 3);
        }

        static CalculusQuestion ChainExpIntegral(System.Random rng)
        {
            int k = Roll(rng, new[] { 2, 3 });
            string correct = $"e^({k}x) / {k} + C";
            var candidates = new List<string>
            {
                $"{k} e^({k}x) + C",
                $"e^({k}x) + C",
                $"e^x + C",
                $"e^({k}x)",
            };
            return Finish(rng, $"int e^({k}x) dx", correct, candidates, 3);
        }

        static CalculusQuestion ProductXSinus(System.Random rng)
        {
            var candidates = new List<string> { "cos(x)", "sin(x)", "x cos(x)" };
            return Finish(rng, "d/dx [ x sin(x) ]", "sin(x) + x cos(x)", candidates, 3);
        }

        static CalculusQuestion ProductXExponential(System.Random rng)
        {
            var candidates = new List<string> { "e^x", "x e^x", "e^x + x" };
            return Finish(rng, "d/dx [ x e^x ]", "e^x + x e^x", candidates, 3);
        }

        static CalculusQuestion PowerChainSquare(System.Random rng)
        {
            var candidates = new List<string> { "2(3x + 1)", "6x + 2", "(3x + 1)^2", "6" };
            return Finish(rng, "d/dx [ (3x + 1)^2 ]", "6(3x + 1)", candidates, 3);
        }

        static CalculusQuestion Finish(System.Random rng, string prompt, string correct,
            List<string> candidates, int difficulty)
        {
            var options = new List<string> { correct };
            Shuffle(rng, candidates);
            foreach (string c in candidates)
            {
                if (options.Count >= 4) break;
                if (string.Equals(c, correct, StringComparison.Ordinal) || options.Contains(c)) continue;
                options.Add(c);
            }

            string[] fallback = { "0", "1", "x", "2x" };
            int i = 0;
            while (options.Count < 4)
            {
                string f = fallback[i++ % fallback.Length];
                if (!options.Contains(f)) options.Add(f);
            }

            Shuffle(rng, options);
            return new CalculusQuestion
            {
                Prompt = prompt,
                CorrectAnswer = correct,
                Options = options.ToArray(),
                Difficulty = difficulty,
            };
        }

        static int Roll(System.Random rng, int[] values) => values[rng.Next(values.Length)];

        static string Mono(int a, int p)
        {
            if (p == 0) return a.ToString();
            bool negative = a < 0;
            int av = Math.Abs(a);
            string coef = av == 1 ? "" : av.ToString();
            string xp = p == 1 ? "x" : "x^" + p;
            return (negative ? "-" : "") + coef + xp;
        }

        static void Shuffle(System.Random rng, List<string> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}