namespace ElectroCooked.Core
{
    /// <summary>
    /// Tipos de peça que existem nas "bancadas de suprimento" da sala de aula,
    /// igual aos ingredientes espalhados pela cozinha no Overcooked.
    /// </summary>
    public enum CircuitComponentType
    {
        Battery,   // Bateria / fonte de tensão (V)
        Resistor,  // Resistor (Ohms)
        Wire       // Fio de ligação (não entra nas contas, só fecha o circuito)
    }
}
