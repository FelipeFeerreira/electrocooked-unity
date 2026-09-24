using System;
using System.Collections.Generic;
using ElectroCooked.Core;
using ElectroCooked.Items;
using ElectroCooked.Managers;
using ElectroCooked.Minigame;
using UnityEngine;

namespace ElectroCooked.Circuit
{
    /// <summary>
    /// A "bancada de montagem" — equivalente à tábua de corte/fogão do Overcooked.
    /// Recebe as peças físicas, confere se o conjunto do pedido está completo,
    /// dispara o minigame de cálculo e reage a acerto (entrega) ou erro (explosão).
    /// </summary>
    public class CircuitStation : MonoBehaviour
    {
        public enum State { Idle, Assembling, InMinigame, Broken }

        [Header("Pontos de encaixe visual")]
        public Transform batterySlot;
        public Transform wireSlot;
        public Transform[] resistorSlots;

        [Header("Perigo de incêndio")]
        public FireHazard fireHazard;

        [Header("Efeito de explosão (opcional)")]
        public ParticleSystem explosionParticles;

        public State CurrentState { get; private set; } = State.Idle;
        public CircuitOrder CurrentOrder { get; private set; }

        bool _batteryPlaced;
        bool _wirePlaced;
        readonly List<Carryable> _placedResistors = new List<Carryable>();
        readonly List<Carryable> _allPlacedParts = new List<Carryable>();

        public event Action<CircuitStation, CircuitOrder> OnOrderAssigned;
        public event Action<CircuitStation> OnPartPlaced;
        public event Action<CircuitStation> OnPartRejected;
        public event Action<CircuitStation> OnOrderDelivered;
        public event Action<CircuitStation> OnExploded;
        public event Action<CircuitStation> OnFireOut;

        void Awake()
        {
            if (fireHazard != null)
                fireHazard.OnFireExtinguished += HandleFireExtinguished;
        }

        void OnDestroy()
        {
            if (fireHazard != null)
                fireHazard.OnFireExtinguished -= HandleFireExtinguished;
        }

        /// <summary>Chamado pelo OrderManager quando esta bancada fica livre.</summary>
        public void AssignOrder(CircuitOrder order)
        {
            CurrentOrder = order;
            ResetPlacement();
            CurrentState = State.Assembling;
            OnOrderAssigned?.Invoke(this, order);
        }

        /// <summary>Tenta encaixar a peça que o jogador está segurando. Retorna se foi aceita.</summary>
        public bool TryPlacePart(Carryable item)
        {
            if (CurrentState != State.Assembling || CurrentOrder == null) { OnPartRejected?.Invoke(this); return false; }
            if (item == null || item.kind != Carryable.Kind.CircuitPart) { OnPartRejected?.Invoke(this); return false; }

            switch (item.partType)
            {
                case CircuitComponentType.Battery:
                    if (!CurrentOrder.RequiresBattery || _batteryPlaced) { OnPartRejected?.Invoke(this); return false; }
                    PlaceAt(item, batterySlot);
                    _batteryPlaced = true;
                    break;

                case CircuitComponentType.Resistor:
                    if (_placedResistors.Count >= CurrentOrder.RequiredResistorCount) { OnPartRejected?.Invoke(this); return false; }
                    Transform slot = resistorSlots != null && _placedResistors.Count < resistorSlots.Length
                        ? resistorSlots[_placedResistors.Count]
                        : null;
                    PlaceAt(item, slot);
                    _placedResistors.Add(item);
                    break;

                case CircuitComponentType.Wire:
                    if (!CurrentOrder.RequiresWire || _wirePlaced) { OnPartRejected?.Invoke(this); return false; }
                    PlaceAt(item, wireSlot);
                    _wirePlaced = true;
                    break;

                default:
                    OnPartRejected?.Invoke(this);
                    return false;
            }

            _allPlacedParts.Add(item);
            OnPartPlaced?.Invoke(this);

            if (IsAssemblyComplete())
            {
                CurrentState = State.InMinigame;
                if (MinigameManager.Instance != null)
                    MinigameManager.Instance.BeginChallenge(this, CurrentOrder);
                else
                    Debug.LogError("[CircuitStation] Não há MinigameManager na cena — o desafio de cálculo não vai abrir.");
            }
            return true;
        }

        void PlaceAt(Carryable item, Transform slot)
        {
            item.OnDropped(slot != null ? slot : transform, Vector3.zero);
            item.Lock();
        }

        bool IsAssemblyComplete()
        {
            bool batteryOk = !CurrentOrder.RequiresBattery || _batteryPlaced;
            bool wireOk = !CurrentOrder.RequiresWire || _wirePlaced;
            bool resistorsOk = _placedResistors.Count >= CurrentOrder.RequiredResistorCount;
            return batteryOk && wireOk && resistorsOk;
        }

        /// <summary>Descrição curta do que falta, para a UI do ticket ("Faltam: 2 resistores").</summary>
        public string GetMissingPartsDescription()
        {
            if (CurrentOrder == null) return "";
            var missing = new List<string>();
            if (CurrentOrder.RequiresBattery && !_batteryPlaced) missing.Add("1 bateria");
            int missingResistors = CurrentOrder.RequiredResistorCount - _placedResistors.Count;
            if (missingResistors > 0) missing.Add($"{missingResistors} resistor(es)");
            if (CurrentOrder.RequiresWire && !_wirePlaced) missing.Add("1 fio");
            return missing.Count == 0 ? "Montado! Faça a conta." : "Faltam: " + string.Join(", ", missing);
        }

        /// <summary>Chamado pelo MinigameManager quando o aluno acerta a conta.</summary>
        public void CompleteOrder(float speedBonus01)
        {
            if (CurrentOrder != null)
            {
                float grade = CurrentOrder.BaseGradeValue * (1f + Mathf.Clamp01(speedBonus01) * 0.5f);
                ScoreManager.Instance?.AddGrade(grade);
            }

            ConsumeAllParts();
            CurrentState = State.Idle;
            CurrentOrder = null;
            OnOrderDelivered?.Invoke(this);
            OrderManager.Instance?.NotifyStationFreed(this);
        }

        /// <summary>Chamado pelo MinigameManager quando o aluno erra a conta: o circuito explode.</summary>
        public void FailOrder()
        {
            ConsumeAllParts();
            CurrentState = State.Broken;
            ScoreManager.Instance?.RegisterMistake();
            if (explosionParticles != null) explosionParticles.Play();
            fireHazard?.StartFire();
            OnExploded?.Invoke(this);
        }

        void HandleFireExtinguished()
        {
            if (CurrentState != State.Broken) return;
            // o mesmo pedido continua valendo: o aluno tem que remontar e acertar a conta.
            ResetPlacement();
            CurrentState = State.Assembling;
            OnFireOut?.Invoke(this);
        }

        void ConsumeAllParts()
        {
            foreach (var part in _allPlacedParts)
                if (part != null) part.Consume();
            _allPlacedParts.Clear();
        }

        void ResetPlacement()
        {
            _batteryPlaced = false;
            _wirePlaced = false;
            _placedResistors.Clear();
            ConsumeAllParts();
        }
    }
}
