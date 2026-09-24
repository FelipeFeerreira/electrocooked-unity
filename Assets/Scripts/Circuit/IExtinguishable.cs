using UnityEngine;

namespace ElectroCooked.Circuit
{
    /// <summary>Qualquer coisa que possa estar em chamas e ser apagada pelo extintor.</summary>
    public interface IExtinguishable
    {
        bool IsOnFire { get; }
        Transform transform { get; } // MonoBehaviours já expõem isso automaticamente
        void ReduceFire(float amount);
    }
}
