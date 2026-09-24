using ElectroCooked.Circuit;
using ElectroCooked.Core;
using ElectroCooked.Items;
using UnityEngine;

namespace ElectroCooked.Player
{
    /// <summary>
    /// Gerencia o que o jogador está segurando e todas as interações:
    /// pegar peça de uma caixa de suprimentos, colocar na bancada de circuito,
    /// pegar/usar o extintor. Um item na mão por vez, igual ao Overcooked.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCarrier : MonoBehaviour
    {
        [Header("Configuração")]
        public Transform carryPoint;
        public float interactRange = 1.8f;
        public LayerMask interactableMask = ~0;

        [Header("Estado (runtime)")]
        public Carryable heldItem;

        PlayerController _controller;
        FireExtinguisherItem _heldExtinguisher;
        readonly Collider[] _hits = new Collider[24];

        /// <summary>Centro da "mão" — um pouco à frente do jogador.</summary>
        Vector3 InteractCenter => transform.position + transform.forward * 0.6f + Vector3.up * 0.2f;

        void Awake()
        {
            _controller = GetComponent<PlayerController>();
        }

        void Update()
        {
            if (InputReader.InteractPressed)
                TryInteract();

            if (heldItem != null && heldItem.kind == Carryable.Kind.FireExtinguisher)
            {
                if (_heldExtinguisher == null)
                    _heldExtinguisher = heldItem.GetComponent<FireExtinguisherItem>();

                bool spraying = false;
                if (InputReader.UseHeld && _heldExtinguisher != null)
                    spraying = _heldExtinguisher.TryExtinguish(transform.position, _controller.Forward, Time.deltaTime);

                _heldExtinguisher?.SetSprayVisual(spraying);
            }
        }

        void TryInteract()
        {
            if (heldItem == null) TryPickUpNearby();
            else TryUseHeldItem();
        }

        void TryPickUpNearby()
        {
            int count = Physics.OverlapSphereNonAlloc(InteractCenter, interactRange, _hits, interactableMask, QueryTriggerInteraction.Collide);

            float bestCrateDist = float.MaxValue;
            float bestItemDist = float.MaxValue;
            SupplyCrate bestCrate = null;
            Carryable bestGroundItem = null;

            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i];
                if (hit == null) continue;
                float dist = Vector3.Distance(InteractCenter, hit.transform.position);

                var crate = hit.GetComponentInParent<SupplyCrate>();
                if (crate != null)
                {
                    if (dist < bestCrateDist) { bestCrateDist = dist; bestCrate = crate; }
                    continue;
                }

                var carryable = hit.GetComponentInParent<Carryable>();
                if (carryable != null && !carryable.isHeld && !carryable.isLocked)
                {
                    if (dist < bestItemDist) { bestItemDist = dist; bestGroundItem = carryable; }
                }
            }

            // Um item solto no chão sempre tem prioridade sobre a caixa: evita
            // encher a sala de peças duplicadas quando o jogador derruba algo.
            if (bestGroundItem != null && bestItemDist <= bestCrateDist)
            {
                PickUp(bestGroundItem);
            }
            else if (bestCrate != null)
            {
                Carryable spawned = bestCrate.Spawn();
                if (spawned != null) PickUp(spawned);
            }
        }

        void TryUseHeldItem()
        {
            // 1) tentar colocar numa estação de circuito na frente do jogador
            int count = Physics.OverlapSphereNonAlloc(InteractCenter, interactRange, _hits, interactableMask, QueryTriggerInteraction.Collide);

            CircuitStation bestStation = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i];
                if (hit == null) continue;
                var station = hit.GetComponentInParent<CircuitStation>();
                if (station == null) continue;

                float dist = Vector3.Distance(InteractCenter, station.transform.position);
                if (dist < bestDist) { bestDist = dist; bestStation = station; }
            }

            if (bestStation != null && heldItem.kind == Carryable.Kind.CircuitPart)
            {
                if (bestStation.TryPlacePart(heldItem))
                {
                    heldItem = null;
                    _heldExtinguisher = null;
                    return;
                }
                // peça recusada (não faz parte do pedido): não solta, só avisa
                return;
            }

            // 2) sem estação por perto: solta o item no chão, à frente do jogador
            DropAtFeet();
        }

        void PickUp(Carryable item)
        {
            heldItem = item;
            item.OnPickedUp(carryPoint);
            _heldExtinguisher = item.kind == Carryable.Kind.FireExtinguisher
                ? item.GetComponent<FireExtinguisherItem>()
                : null;
        }

        void DropAtFeet()
        {
            if (heldItem == null) return;
            Vector3 dropPos = transform.position + transform.forward * 0.9f + Vector3.up * 0.2f;
            _heldExtinguisher?.SetSprayVisual(false);
            heldItem.OnDropped(null);
            heldItem.transform.position = dropPos;
            heldItem = null;
            _heldExtinguisher = null;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + transform.forward * 0.6f + Vector3.up * 0.2f, interactRange);
        }
    }
}
