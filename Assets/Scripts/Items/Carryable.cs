using ElectroCooked.Core;
using UnityEngine;

namespace ElectroCooked.Items
{
    /// <summary>
    /// Qualquer objeto que o jogador pode pegar/carregar/soltar, igual aos
    /// ingredientes do Overcooked. Pode ser uma peça de circuito (bateria,
    /// resistor, fio) ou o extintor de incêndio.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Carryable : MonoBehaviour
    {
        public enum Kind { CircuitPart, FireExtinguisher }

        [Header("O que é este item")]
        public Kind kind = Kind.CircuitPart;
        public CircuitComponentType partType; // só relevante quando kind == CircuitPart

        [Header("Estado (preenchido em runtime)")]
        public bool isHeld;
        [Tooltip("Peça já encaixada na bancada: não pode mais ser pega de volta.")]
        public bool isLocked;

        Rigidbody _rb;
        Collider _col;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();
        }

        void EnsureRefs()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            if (_col == null) _col = GetComponent<Collider>();
        }

        /// <summary>Chamado pelo PlayerCarrier ao pegar o item na mão.</summary>
        public void OnPickedUp(Transform carryPoint)
        {
            EnsureRefs();
            isHeld = true;
            if (_rb != null) _rb.isKinematic = true;
            if (_col != null) _col.enabled = false;

            transform.SetParent(carryPoint);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>Chamado ao soltar/colocar o item numa estação ou no chão.</summary>
        public void OnDropped(Transform newParent = null, Vector3? localPos = null)
        {
            EnsureRefs();
            isHeld = false;
            transform.SetParent(newParent);

            if (localPos.HasValue)
            {
                transform.localPosition = localPos.Value;
                transform.localRotation = Quaternion.identity;
            }

            if (newParent == null)
            {
                if (_rb != null) _rb.isKinematic = false;
                if (_col != null) _col.enabled = true;
            }
        }

        /// <summary>
        /// Trava a peça na bancada: ela continua visível no encaixe, mas o jogador
        /// não consegue mais pegá-la de volta (senão dava pra desmontar o circuito
        /// sem querer ao apertar E perto da bancada).
        /// </summary>
        public void Lock()
        {
            EnsureRefs();
            isLocked = true;
            if (_rb != null) _rb.isKinematic = true;
            if (_col != null) _col.enabled = false;
        }

        /// <summary>Destroi o item (usado quando um pedido é entregue ou o circuito explode).</summary>
        public void Consume()
        {
            Destroy(gameObject);
        }
    }
}
