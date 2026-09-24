namespace ElectroCooked.Core
{
    /// <summary>
    /// Nomes das cenas em um lugar só. Tudo que troca de cena passa por aqui,
    /// então renomear uma cena é mexer neste arquivo e reconstruir.
    /// </summary>
    public static class SceneNames
    {
        public const string School = "Escola";
        public const string CircuitClass = "Aula_Circuitos";
        public const string CalculusClass = "Aula_Calculo";
    }

    /// <summary>Identificadores das matérias, usados para guardar as notas.</summary>
    public static class SubjectIds
    {
        public const string Circuits = "circuitos";
        public const string Calculus = "calculo";
        public const string Physics = "fisica";
        public const string Chemistry = "quimica";
    }
}
