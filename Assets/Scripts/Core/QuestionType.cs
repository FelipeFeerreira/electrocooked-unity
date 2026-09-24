namespace ElectroCooked.Core
{
    /// <summary>
    /// Qual grandeza o aluno precisa calcular para "entregar" o circuito.
    /// Isso é sorteado a cada pedido, trocando o minigame como pedido o usuário
    /// ("mudando o minigame para... calcular potência algumas vezes").
    /// </summary>
    public enum QuestionType
    {
        ComputeCurrent,    // dado V e R -> achar I  (I = V / R)
        ComputeVoltage,    // dado I e R -> achar V  (V = I * R)
        ComputeResistance, // dado V e I -> achar R  (R = V / I)
        ComputePower       // dado V e I -> achar P  (P = V * I)
    }
}
