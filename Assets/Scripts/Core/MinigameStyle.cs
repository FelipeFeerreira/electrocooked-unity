namespace ElectroCooked.Core
{
    /// <summary>
    /// Estilo de interação do minigame de cálculo. Trocar o estilo a cada pedido
    /// é o que dá a variedade que o usuário pediu ("mudando o minigame").
    /// </summary>
    public enum MinigameStyle
    {
        NumericInput,   // digita o número calculado no teclado
        MultipleChoice, // escolhe entre 4 valores (1 certo + 3 pegadinhas)
        Slider          // arrasta um marcador até o valor certo numa régua
    }
}
