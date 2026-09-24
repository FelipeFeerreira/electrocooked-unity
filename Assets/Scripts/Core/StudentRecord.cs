using UnityEngine;

namespace ElectroCooked.Core
{
    /// <summary>
    /// O boletim do aluno, guardado no PlayerPrefs. Sobrevive à troca de cena
    /// (escola -> aula -> escola) e ao fechar o jogo, que é o que faz a escola
    /// parecer uma escola de verdade: você volta e vê sua nota lá no mural.
    ///
    /// PlayerPrefs é simples demais para um jogo grande, mas é exatamente o
    /// certo para um protótipo — zero setup, zero arquivo pra versionar.
    /// </summary>
    public static class StudentRecord
    {
        const string BestKey = "ec_best_";
        const string LastKey = "ec_last_";
        const string CountKey = "ec_count_";

        /// <summary>Registra o resultado de uma aula. Guarda a melhor nota e a última.</summary>
        public static void SaveGrade(string subjectId, float grade)
        {
            if (string.IsNullOrEmpty(subjectId)) return;

            grade = Mathf.Clamp(grade, 0f, 10f);
            PlayerPrefs.SetFloat(LastKey + subjectId, grade);

            if (grade > BestGrade(subjectId))
                PlayerPrefs.SetFloat(BestKey + subjectId, grade);

            PlayerPrefs.SetInt(CountKey + subjectId, TimesTaken(subjectId) + 1);
            PlayerPrefs.Save();
        }

        public static float BestGrade(string subjectId) => PlayerPrefs.GetFloat(BestKey + subjectId, 0f);
        public static float LastGrade(string subjectId) => PlayerPrefs.GetFloat(LastKey + subjectId, 0f);
        public static int TimesTaken(string subjectId) => PlayerPrefs.GetInt(CountKey + subjectId, 0);
        public static bool HasTaken(string subjectId) => TimesTaken(subjectId) > 0;

        /// <summary>Conceito da faculdade a partir da nota.</summary>
        public static string Concept(float grade)
        {
            if (grade >= 9f) return "A";
            if (grade >= 7f) return "B";
            if (grade >= 5f) return "C";
            return "REPROVADO";
        }

        public static void ResetAll()
        {
            foreach (string id in new[] { SubjectIds.Circuits, SubjectIds.Calculus, SubjectIds.Physics, SubjectIds.Chemistry })
            {
                PlayerPrefs.DeleteKey(BestKey + id);
                PlayerPrefs.DeleteKey(LastKey + id);
                PlayerPrefs.DeleteKey(CountKey + id);
            }
            PlayerPrefs.Save();
        }
    }
}
